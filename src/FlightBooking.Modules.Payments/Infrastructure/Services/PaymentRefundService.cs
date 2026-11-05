using FlightBooking.Modules.Payments.Application.Interfaces;
using FlightBooking.Modules.Payments.Domain.Entities;
using FlightBooking.Modules.Payments.Domain.Enums;
using FlightBooking.Modules.Payments.Infrastructure.Data;
using FlightBooking.Shared.Contracts.Payments;
using FlightBooking.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Modules.Payments.Infrastructure.Services;

public class PaymentRefundService : IPaymentRefundService
{
    private readonly PaymentsDbContext _db;
    private readonly IPaymentGateway _gateway;

    public PaymentRefundService(PaymentsDbContext db, IPaymentGateway gateway)
    {
        _db = db;
        _gateway = gateway;
    }

    public async Task<bool> RefundBookingAsync(Guid bookingId, string reason, CancellationToken ct)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.BookingId == bookingId && p.Status == PaymentStatus.Succeeded, ct);

        if (payment is null) return false;

        var result = await _gateway.RefundAsync(payment.ProviderReference, payment.Amount, ct);

        _db.Refunds.Add(new Refund
        {
            PaymentId = payment.Id,
            Amount = payment.Amount,
            Status = result.Succeeded ? RefundStatus.Succeeded : RefundStatus.Failed,
            Reason = reason.Length > 250 ? reason[..250] : reason
        });

        if (result.Succeeded)
            payment.Status = PaymentStatus.Refunded;

        await _db.SaveChangesAsync(ct);

        if (!result.Succeeded)
            throw new BusinessRuleException("The refund failed, so the booking was not cancelled. Please try again.");

        return true;
    }
}
