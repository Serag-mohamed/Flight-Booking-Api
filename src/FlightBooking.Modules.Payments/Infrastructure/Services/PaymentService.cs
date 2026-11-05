using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlightBooking.Modules.Payments.Application.DTOs;
using FlightBooking.Modules.Payments.Application.Interfaces;
using FlightBooking.Modules.Payments.Domain.Entities;
using FlightBooking.Modules.Payments.Domain.Enums;
using FlightBooking.Modules.Payments.Infrastructure.Data;
using FlightBooking.Shared.Contracts.Bookings;
using FlightBooking.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Modules.Payments.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private const int MaxIdempotencyKeyLength = 64;
    private static readonly TimeSpan PendingLockWindow = TimeSpan.FromMinutes(2);

    private static readonly JsonSerializerOptions Json = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly PaymentsDbContext _db;
    private readonly IPaymentGateway _gateway;
    private readonly IBookingLifecycleService _bookings;

    public PaymentService(PaymentsDbContext db, IPaymentGateway gateway, IBookingLifecycleService bookings)
    {
        _db = db;
        _gateway = gateway;
        _bookings = bookings;
    }

    public async Task<PaymentResponseDto> PayAsync(
        string userId, PayRequestDto request, string idempotencyKey, CancellationToken ct)
    {
        idempotencyKey = idempotencyKey.Trim();
        if (idempotencyKey.Length == 0 || idempotencyKey.Length > MaxIdempotencyKeyLength)
            throw new BusinessRuleException($"Idempotency-Key must be between 1 and {MaxIdempotencyKeyLength} characters.");

        if (request.BookingId == Guid.Empty)
            throw new BusinessRuleException("BookingId is required.");

        var scope = "pay:" + Sha256(userId)[..32];
        var requestHash = Sha256($"{request.BookingId}|{request.CardToken}");

        var existing = await _db.IdempotencyRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Scope == scope && r.Key == idempotencyKey, ct);

        if (existing is not null)
        {
            if (existing.RequestHash != requestHash)
                throw new ConflictException("This Idempotency-Key was already used with a different request.");

            return JsonSerializer.Deserialize<PaymentResponseDto>(existing.ResponseBody, Json)!;
        }

        var booking = await _bookings.GetForPaymentAsync(request.BookingId, ct);
        if (booking is null || booking.UserId != userId)
            throw new NotFoundException("Booking was not found.");

        if (!booking.IsPayable)
            throw new BusinessRuleException("This booking cannot be paid (it is already paid, cancelled or expired).");

        var now = DateTimeOffset.UtcNow;
        var inFlight = await _db.Payments.AnyAsync(p =>
            p.BookingId == booking.BookingId &&
            (p.Status == PaymentStatus.Succeeded ||
             (p.Status == PaymentStatus.Pending && p.CreatedAtUtc > now - PendingLockWindow)), ct);

        if (inFlight)
            throw new ConflictException("A payment for this booking is already in progress or completed.");

        var payment = new Payment
        {
            BookingId = booking.BookingId,
            UserId = userId,
            Amount = booking.TotalAmount,
            Currency = booking.Currency,
            Status = PaymentStatus.Pending,
            IdempotencyKey = idempotencyKey,
            CreatedAtUtc = now
        };

        try
        {
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("A request with this Idempotency-Key is already being processed.");
        }

        var charge = await _gateway.ChargeAsync(
            new GatewayChargeRequest(payment.Amount, payment.Currency, request.CardToken), ct);

        payment.ProviderReference = charge.Reference;

        if (!charge.Succeeded)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = charge.FailureReason;
        }
        else
        {
            payment.Status = PaymentStatus.Succeeded;

            try
            {
                await _bookings.ConfirmPaidAsync(booking.BookingId, ct);
            }
            catch (Exception ex) when (ex is ConflictException or NotFoundException)
            {
                var refund = await _gateway.RefundAsync(charge.Reference, payment.Amount, ct);

                payment.Status = PaymentStatus.Refunded;
                payment.FailureReason = "Booking could not be confirmed: " + ex.Message;

                _db.Refunds.Add(new Refund
                {
                    PaymentId = payment.Id,
                    Amount = payment.Amount,
                    Status = refund.Succeeded ? RefundStatus.Succeeded : RefundStatus.Failed,
                    Reason = "Booking could not be confirmed after payment"
                });
            }
        }

        var dto = ToDto(payment);

        _db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Scope = scope,
            Key = idempotencyKey,
            RequestHash = requestHash,
            StatusCode = payment.Status switch
            {
                PaymentStatus.Succeeded => 200,
                PaymentStatus.Failed => 402,
                _ => 409
            },
            ResponseBody = JsonSerializer.Serialize(dto, Json)
        });

        await _db.SaveChangesAsync(ct);
        return dto;
    }

    public async Task<PaymentResponseDto?> GetByIdAsync(string userId, bool isAdmin, Guid id, CancellationToken ct)
    {
        var payment = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (payment is null || (!isAdmin && payment.UserId != userId)) return null;

        return ToDto(payment);
    }

    public async Task<IReadOnlyList<PaymentResponseDto>> GetByBookingAsync(
        string userId, bool isAdmin, Guid bookingId, CancellationToken ct)
    {
        var query = _db.Payments.AsNoTracking().Where(p => p.BookingId == bookingId);
        if (!isAdmin) query = query.Where(p => p.UserId == userId);

        var payments = await query.OrderByDescending(p => p.CreatedAtUtc).ToListAsync(ct);
        return payments.Select(ToDto).ToList();
    }

    private static PaymentResponseDto ToDto(Payment p) => new()
    {
        Id = p.Id,
        BookingId = p.BookingId,
        Amount = p.Amount,
        Currency = p.Currency,
        Status = p.Status,
        ProviderReference = p.ProviderReference,
        FailureReason = p.FailureReason,
        CreatedAtUtc = p.CreatedAtUtc
    };

    private static string Sha256(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
