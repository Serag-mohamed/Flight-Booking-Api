using FlightBooking.Modules.Bookings.Application.Interfaces;
using FlightBooking.Modules.Bookings.Domain.Enums;
using FlightBooking.Modules.Bookings.Infrastructure.Data;
using FlightBooking.Shared.Contracts.Bookings;
using FlightBooking.Shared.Contracts.Flights;
using FlightBooking.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Modules.Bookings.Infrastructure.Services;

public class BookingLifecycleService : IBookingLifecycleService, IBookingExpirationProcessor
{
    private const int ExpirationBatchSize = 50;

    private readonly BookingsDbContext _db;
    private readonly IFlightSeatReservationService _seats;

    public BookingLifecycleService(BookingsDbContext db, IFlightSeatReservationService seats)
    {
        _db = db;
        _seats = seats;
    }

    public async Task<BookingPaymentInfo?> GetForPaymentAsync(Guid bookingId, CancellationToken ct)
    {
        var booking = await _db.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookingId, ct);
        if (booking is null) return null;

        var payable = booking.Status == BookingStatus.Pending && booking.ExpiresAtUtc > DateTimeOffset.UtcNow;

        return new BookingPaymentInfo(
            booking.Id, booking.UserId, booking.TotalAmount, booking.Currency, booking.ExpiresAtUtc, payable);
    }

    public async Task ConfirmPaidAsync(Guid bookingId, CancellationToken ct)
    {
        var booking = await _db.Bookings
            .Include(b => b.Seats)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct)
            ?? throw new NotFoundException("Booking was not found.");

        if (booking.Status == BookingStatus.Confirmed) return;

        if (booking.Status != BookingStatus.Pending)
            throw new ConflictException($"The booking is {booking.Status} and cannot be confirmed.");

        if (booking.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            throw new ConflictException("The booking hold has expired.");

        var seatIds = booking.Seats.Where(s => s.IsActive).Select(s => s.FlightSeatId).ToList();

        await _seats.ConfirmSeatsAsync(seatIds, ct);

        booking.Status = BookingStatus.Confirmed;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The booking was changed by another request while confirming.");
        }
    }

    public async Task<int> ExpireOverdueAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var ids = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.Status == BookingStatus.Pending && b.ExpiresAtUtc <= now)
            .OrderBy(b => b.ExpiresAtUtc)
            .Select(b => b.Id)
            .Take(ExpirationBatchSize)
            .ToListAsync(ct);

        var expired = 0;

        foreach (var id in ids)
        {
            _db.ChangeTracker.Clear();

            var booking = await _db.Bookings.Include(b => b.Seats).FirstOrDefaultAsync(b => b.Id == id, ct);
            if (booking is null || booking.Status != BookingStatus.Pending) continue;

            var seatIds = booking.Seats.Where(s => s.IsActive).Select(s => s.FlightSeatId).ToList();

            booking.Status = BookingStatus.Expired;
            foreach (var seat in booking.Seats) seat.IsActive = false;

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                continue;
            }

            await _seats.ReleaseSeatsAsync(seatIds, ct);
            expired++;
        }

        return expired;
    }
}
