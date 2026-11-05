using FlightBooking.Modules.Flights.Domain.Enums;
using FlightBooking.Modules.Flights.Infrastructure.Data;
using FlightBooking.Shared.Contracts.Flights;
using FlightBooking.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Modules.Flights.Infrastructure.Services;

public class FlightSeatReservationService : IFlightSeatReservationService
{
    private const int MaxReleaseAttempts = 3;
    private readonly FlightsDbContext _db;

    public FlightSeatReservationService(FlightsDbContext db) => _db = db;

    public async Task<SeatHoldResult> HoldSeatsAsync(IReadOnlyCollection<Guid> flightSeatIds, CancellationToken ct)
    {
        var ids = flightSeatIds.Distinct().ToList();
        if (ids.Count == 0)
            throw new BusinessRuleException("At least one seat is required.");

        var seats = await _db.FlightSeats
            .Include(s => s.Flight).ThenInclude(f => f.OriginAirport)
            .Include(s => s.Flight).ThenInclude(f => f.DestinationAirport)
            .Where(s => ids.Contains(s.Id))
            .ToListAsync(ct);

        if (seats.Count != ids.Count)
            throw new NotFoundException("One or more seats were not found.");

        var flight = seats[0].Flight;

        if (seats.Any(s => s.FlightId != flight.Id))
            throw new BusinessRuleException("All seats must belong to the same flight.");

        if (flight.Status != FlightStatus.Scheduled || flight.DepartureUtc <= DateTimeOffset.UtcNow)
            throw new BusinessRuleException("This flight is not open for booking.");

        var currencies = seats.Select(s => s.Currency).Distinct().ToList();
        if (currencies.Count != 1)
            throw new BusinessRuleException("All seats must use the same currency.");

        var unavailable = seats.Where(s => s.Status != SeatStatus.Available).Select(s => s.SeatNumber).ToList();
        if (unavailable.Count > 0)
            throw new ConflictException($"These seats are not available: {string.Join(", ", unavailable)}.");

        foreach (var seat in seats)
            seat.Status = SeatStatus.Held;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("One or more seats were just taken by another customer. Please choose other seats.");
        }

        return new SeatHoldResult(
            flight.Id,
            flight.FlightNumber,
            flight.OriginAirport.IataCode,
            flight.DestinationAirport.IataCode,
            flight.DepartureUtc,
            currencies[0],
            seats.Select(s => new HeldSeatInfo(s.Id, s.SeatNumber, s.CabinClass.ToString(), s.Price)).ToList());
    }

    public async Task ConfirmSeatsAsync(IReadOnlyCollection<Guid> flightSeatIds, CancellationToken ct)
    {
        var ids = flightSeatIds.Distinct().ToList();
        var seats = await _db.FlightSeats.Where(s => ids.Contains(s.Id)).ToListAsync(ct);

        if (seats.Count != ids.Count)
            throw new NotFoundException("One or more seats were not found.");

        foreach (var seat in seats)
        {
            if (seat.Status == SeatStatus.Booked) continue;

            if (seat.Status != SeatStatus.Held)
                throw new ConflictException($"Seat {seat.SeatNumber} is no longer held for this booking.");

            seat.Status = SeatStatus.Booked;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Seat state changed while confirming. Please try again.");
        }
    }

    public async Task ReleaseSeatsAsync(IReadOnlyCollection<Guid> flightSeatIds, CancellationToken ct)
    {
        var ids = flightSeatIds.Distinct().ToList();

        for (var attempt = 1; attempt <= MaxReleaseAttempts; attempt++)
        {
            var seats = await _db.FlightSeats.Where(s => ids.Contains(s.Id)).ToListAsync(ct);

            foreach (var seat in seats.Where(s => s.Status is SeatStatus.Held or SeatStatus.Booked))
                seat.Status = SeatStatus.Available;

            try
            {
                await _db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxReleaseAttempts)
            {
                _db.ChangeTracker.Clear();
            }
        }
    }
}
