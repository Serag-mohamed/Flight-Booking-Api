using System.Security.Cryptography;
using FlightBooking.Modules.Bookings.Application.DTOs;
using FlightBooking.Modules.Bookings.Application.Interfaces;
using FlightBooking.Modules.Bookings.Domain.Entities;
using FlightBooking.Modules.Bookings.Domain.Enums;
using FlightBooking.Modules.Bookings.Infrastructure.Data;
using FlightBooking.Shared.Common;
using FlightBooking.Shared.Contracts.Flights;
using FlightBooking.Shared.Contracts.Payments;
using FlightBooking.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Modules.Bookings.Infrastructure.Services;

public class BookingService : IBookingService
{
    internal static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(15);

    private const int MaxPassengers = 9;
    private const int MaxPageSize = 100;
    private const string ReferenceAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int ReferenceLength = 6;

    private readonly BookingsDbContext _db;
    private readonly IFlightSeatReservationService _seats;
    private readonly IPaymentRefundService _refunds;

    public BookingService(
        BookingsDbContext db,
        IFlightSeatReservationService seats,
        IPaymentRefundService refunds)
    {
        _db = db;
        _seats = seats;
        _refunds = refunds;
    }

    public async Task<BookingResponseDto> CreateAsync(
        string userId, CreateBookingRequestDto request, CancellationToken ct)
    {
        var passengers = request.Passengers;

        if (passengers.Count is 0 or > MaxPassengers)
            throw new BusinessRuleException($"A booking must have between 1 and {MaxPassengers} passengers.");

        var seatIds = passengers.Select(p => p.FlightSeatId).ToList();
        if (seatIds.Distinct().Count() != seatIds.Count)
            throw new BusinessRuleException("A seat can only be assigned to one passenger.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (passengers.Any(p => p.DateOfBirth > today || p.DateOfBirth.Year < 1900))
            throw new BusinessRuleException("A passenger date of birth is invalid.");

        var hold = await _seats.HoldSeatsAsync(seatIds, ct);
        var seatMap = hold.Seats.ToDictionary(s => s.SeatId);

        var departureDate = DateOnly.FromDateTime(hold.DepartureUtc.UtcDateTime);
        var types = passengers.Select(p => ResolvePassengerType(p.DateOfBirth, departureDate)).ToList();

        if (!types.Contains(PassengerType.Adult))
        {
            await SafeReleaseAsync(seatIds);
            throw new BusinessRuleException("At least one adult passenger is required.");
        }

        var now = DateTimeOffset.UtcNow;
        var booking = new Booking
        {
            UserId = userId,
            FlightId = hold.FlightId,
            FlightNumber = hold.FlightNumber,
            OriginIata = hold.OriginIata,
            DestinationIata = hold.DestinationIata,
            DepartureUtc = hold.DepartureUtc,
            Currency = hold.Currency,
            Status = BookingStatus.Pending,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(HoldDuration)
        };

        for (var i = 0; i < passengers.Count; i++)
        {
            var p = passengers[i];
            var seat = seatMap[p.FlightSeatId];

            var passenger = new BookingPassenger
            {
                BookingId = booking.Id,
                FullName = p.FullName.Trim(),
                DateOfBirth = p.DateOfBirth,
                PassengerType = types[i]
            };

            booking.Passengers.Add(passenger);
            booking.Seats.Add(new BookingSeat
            {
                BookingId = booking.Id,
                FlightSeatId = seat.SeatId,
                PassengerId = passenger.Id,
                UnitPrice = seat.Price,
                SeatNumber = seat.SeatNumber,
                CabinClass = seat.CabinClass
            });
        }

        booking.TotalAmount = booking.Seats.Sum(s => s.UnitPrice);

        try
        {
            booking.BookingReference = await GenerateUniqueReferenceAsync(ct);
            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            await SafeReleaseAsync(seatIds);
            throw;
        }

        return booking.ToDto();
    }

    public Task<PagedResult<BookingResponseDto>> GetMyBookingsAsync(
        string userId, BookingQueryParams query, CancellationToken ct)
    {
        var q = _db.Bookings.Where(b => b.UserId == userId);
        if (query.Status.HasValue)
            q = q.Where(b => b.Status == query.Status.Value);

        return PageAsync(q, query.Page, query.PageSize, ct);
    }

    public Task<PagedResult<BookingResponseDto>> GetAllAsync(BookingAdminQueryParams query, CancellationToken ct)
    {
        var q = _db.Bookings.AsQueryable();

        if (query.Status.HasValue)
            q = q.Where(b => b.Status == query.Status.Value);
        if (!string.IsNullOrWhiteSpace(query.UserId))
            q = q.Where(b => b.UserId == query.UserId);
        if (query.FlightId.HasValue)
            q = q.Where(b => b.FlightId == query.FlightId.Value);

        return PageAsync(q, query.Page, query.PageSize, ct);
    }

    public async Task<BookingResponseDto?> GetByIdAsync(
        string userId, bool isAdmin, Guid id, CancellationToken ct)
    {
        var booking = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.Passengers)
            .Include(b => b.Seats)
            .AsSplitQuery()
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (booking is null || (!isAdmin && booking.UserId != userId)) return null;

        return booking.ToDto();
    }

    public async Task<BookingResponseDto?> CancelAsync(
        string userId, bool isAdmin, Guid id, CancellationToken ct)
    {
        var booking = await _db.Bookings
            .Include(b => b.Passengers)
            .Include(b => b.Seats)
            .AsSplitQuery()
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (booking is null || (!isAdmin && booking.UserId != userId)) return null;

        switch (booking.Status)
        {
            case BookingStatus.Cancelled:
                return booking.ToDto();
            case BookingStatus.Expired:
                throw new ConflictException("This booking has already expired.");
            case BookingStatus.Confirmed:
                if (booking.DepartureUtc <= DateTimeOffset.UtcNow)
                    throw new BusinessRuleException("A booking cannot be cancelled after the flight has departed.");

                await _refunds.RefundBookingAsync(booking.Id, "Booking cancelled", ct);
                break;
        }

        var seatIds = booking.Seats.Where(s => s.IsActive).Select(s => s.FlightSeatId).ToList();

        booking.Status = BookingStatus.Cancelled;
        foreach (var seat in booking.Seats) seat.IsActive = false;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The booking was modified by another request. Please try again.");
        }

        await SafeReleaseAsync(seatIds);
        return booking.ToDto();
    }

    private async Task<PagedResult<BookingResponseDto>> PageAsync(
        IQueryable<Booking> query, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .Include(b => b.Passengers)
            .Include(b => b.Seats)
            .AsSplitQuery()
            .OrderByDescending(b => b.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<BookingResponseDto>(items.Select(b => b.ToDto()).ToList(), page, pageSize, total);
    }

    internal static PassengerType ResolvePassengerType(DateOnly dateOfBirth, DateOnly onDate)
    {
        var age = onDate.Year - dateOfBirth.Year;
        if (dateOfBirth.AddYears(age) > onDate) age--;

        return age switch
        {
            < 2 => PassengerType.Infant,
            < 12 => PassengerType.Child,
            _ => PassengerType.Adult
        };
    }

    private async Task<string> GenerateUniqueReferenceAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var chars = new char[ReferenceLength];
            for (var i = 0; i < chars.Length; i++)
                chars[i] = ReferenceAlphabet[RandomNumberGenerator.GetInt32(ReferenceAlphabet.Length)];

            var reference = "FB-" + new string(chars);
            if (!await _db.Bookings.AnyAsync(b => b.BookingReference == reference, ct))
                return reference;
        }

        throw new InvalidOperationException("Could not generate a unique booking reference.");
    }

    private async Task SafeReleaseAsync(IReadOnlyCollection<Guid> seatIds)
    {
        try
        {
            await _seats.ReleaseSeatsAsync(seatIds, CancellationToken.None);
        }
        catch
        {
        }
    }
}
