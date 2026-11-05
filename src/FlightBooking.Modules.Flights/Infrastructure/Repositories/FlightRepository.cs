using FlightBooking.Modules.Flights.Domain.Entities;
using FlightBooking.Modules.Flights.Domain.Enums;
using FlightBooking.Modules.Flights.Domain.IRepositories;
using FlightBooking.Modules.Flights.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Modules.Flights.Infrastructure.Repositories;

public class FlightRepository : IFlightRepository
{
    private readonly FlightsDbContext _db;

    public FlightRepository(FlightsDbContext db) => _db = db;

    private IQueryable<Flight> WithDetails() => _db.Flights
        .Include(f => f.OriginAirport)
        .Include(f => f.DestinationAirport)
        .Include(f => f.Aircraft);

    public async Task<(IReadOnlyList<Flight> Items, int TotalCount)> SearchAsync(
        Guid originId, Guid destinationId, DateTimeOffset fromUtc, DateTimeOffset toUtc,
        CabinClass? cabinClass, int passengers, int page, int pageSize, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var query = WithDetails().AsNoTracking().Where(f =>
            f.OriginAirportId == originId &&
            f.DestinationAirportId == destinationId &&
            f.DepartureUtc >= fromUtc && f.DepartureUtc < toUtc &&
            f.DepartureUtc > now &&
            f.Status == FlightStatus.Scheduled &&
            f.FlightSeats.Count(s =>
                s.Status == SeatStatus.Available &&
                (cabinClass == null || s.CabinClass == cabinClass)) >= passengers);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(f => f.DepartureUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<(IReadOnlyList<Flight> Items, int TotalCount)> GetPagedAsync(
        string? flightNumber, FlightStatus? status, DateTimeOffset? fromUtc, DateTimeOffset? toUtc,
        int page, int pageSize, CancellationToken ct)
    {
        var query = WithDetails().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(flightNumber))
            query = query.Where(f => f.FlightNumber.Contains(flightNumber));
        if (status.HasValue)
            query = query.Where(f => f.Status == status.Value);
        if (fromUtc.HasValue)
            query = query.Where(f => f.DepartureUtc >= fromUtc.Value);
        if (toUtc.HasValue)
            query = query.Where(f => f.DepartureUtc < toUtc.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(f => f.DepartureUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<Flight?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Flights.FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task<Flight?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct)
        => WithDetails().AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<Dictionary<Guid, (int Available, decimal? Lowest, string? Currency)>> GetSeatSummariesAsync(
        IEnumerable<Guid> flightIds, CabinClass? cabinClass, CancellationToken ct)
    {
        var ids = flightIds.ToList();

        var rows = await _db.FlightSeats
            .AsNoTracking()
            .Where(s => ids.Contains(s.FlightId)
                        && s.Status == SeatStatus.Available
                        && (cabinClass == null || s.CabinClass == cabinClass))
            .GroupBy(s => s.FlightId)
            .Select(g => new
            {
                FlightId = g.Key,
                Available = g.Count(),
                Lowest = g.Min(s => (decimal?)s.Price),
                Currency = g.Max(s => s.Currency)
            })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.FlightId, r => (r.Available, r.Lowest, (string?)r.Currency));
    }

    public Task<bool> FlightNumberExistsOnDateAsync(
        string flightNumber, DateTimeOffset fromUtc, DateTimeOffset toUtc, Guid? excludeId, CancellationToken ct)
        => _db.Flights.AnyAsync(f =>
            f.FlightNumber == flightNumber &&
            f.DepartureUtc >= fromUtc && f.DepartureUtc < toUtc &&
            f.Status != FlightStatus.Cancelled &&
            (excludeId == null || f.Id != excludeId), ct);

    public Task<bool> AircraftHasOverlapAsync(
        Guid aircraftId, DateTimeOffset departureUtc, DateTimeOffset arrivalUtc, Guid? excludeFlightId, CancellationToken ct)
        => _db.Flights.AnyAsync(f =>
            f.AircraftId == aircraftId &&
            f.Status != FlightStatus.Cancelled &&
            (excludeFlightId == null || f.Id != excludeFlightId) &&
            f.DepartureUtc < arrivalUtc && f.ArrivalUtc > departureUtc, ct);

    public Task<bool> HasBookedOrHeldSeatsAsync(Guid flightId, CancellationToken ct)
        => _db.FlightSeats.AnyAsync(s =>
            s.FlightId == flightId && (s.Status == SeatStatus.Booked || s.Status == SeatStatus.Held), ct);

    public async Task<IReadOnlyList<FlightSeat>> GetSeatsAsync(
        Guid flightId, CabinClass? cabinClass, bool availableOnly, CancellationToken ct)
    {
        var query = _db.FlightSeats.AsNoTracking().Where(s => s.FlightId == flightId);

        if (cabinClass.HasValue)
            query = query.Where(s => s.CabinClass == cabinClass.Value);
        if (availableOnly)
            query = query.Where(s => s.Status == SeatStatus.Available);

        return await query
            .OrderBy(s => s.SeatNumber.Length).ThenBy(s => s.SeatNumber)
            .ToListAsync(ct);
    }

    public Task<List<FlightSeat>> GetTrackedSeatsAsync(Guid flightId, CancellationToken ct)
        => _db.FlightSeats.Where(s => s.FlightId == flightId).ToListAsync(ct);

    public Task<FlightSeat?> GetSeatAsync(Guid flightId, Guid seatId, CancellationToken ct)
        => _db.FlightSeats.FirstOrDefaultAsync(s => s.FlightId == flightId && s.Id == seatId, ct);

    public Task<bool> HasSeatsAsync(Guid flightId, CancellationToken ct)
        => _db.FlightSeats.AnyAsync(s => s.FlightId == flightId, ct);

    public async Task AddSeatsAsync(IEnumerable<FlightSeat> seats, CancellationToken ct)
    {
        _db.FlightSeats.AddRange(seats);
        await _db.SaveChangesAsync(ct);
    }

    public Task<Airport?> GetAirportAsync(Guid id, CancellationToken ct)
        => _db.Airports.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Airport?> GetAirportByIataAsync(string iata, CancellationToken ct)
        => _db.Airports.AsNoTracking().FirstOrDefaultAsync(a => a.IataCode == iata, ct);

    public Task<Aircraft?> GetAircraftWithSeatsAsync(Guid id, CancellationToken ct)
        => _db.Aircrafts.AsNoTracking().Include(a => a.AircraftSeats).FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task AddAsync(Flight flight, CancellationToken ct)
    {
        _db.Flights.Add(flight);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(Flight flight, CancellationToken ct)
    {
        _db.Flights.Remove(flight);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
