using FlightBooking.Modules.Flights.Domain.Entities;
using FlightBooking.Modules.Flights.Domain.IRepositories;
using FlightBooking.Modules.Flights.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Modules.Flights.Infrastructure.Repositories;

public class AircraftRepository : IAircraftRepository
{
    private readonly FlightsDbContext _db;

    public AircraftRepository(FlightsDbContext db) => _db = db;

    public async Task<(IReadOnlyList<Aircraft> Items, int TotalCount)> GetPagedAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Aircrafts.AsNoTracking();

        if (isActive.HasValue)
            query = query.Where(a => a.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.RegistrationNumber.Contains(search) || a.Model.Contains(search));

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(a => a.RegistrationNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<Aircraft?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Aircrafts.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<bool> RegistrationExistsAsync(string registrationNumber, Guid? excludeId, CancellationToken ct)
        => _db.Aircrafts.AnyAsync(a =>
            a.RegistrationNumber == registrationNumber && (excludeId == null || a.Id != excludeId), ct);

    public Task<int> CountSeatsAsync(Guid aircraftId, CancellationToken ct)
        => _db.AircraftSeats.CountAsync(s => s.AircraftId == aircraftId, ct);

    public async Task<Dictionary<Guid, int>> GetSeatsCountsAsync(
        IEnumerable<Guid> aircraftIds, CancellationToken ct)
    {
        var ids = aircraftIds.ToList();

        return await _db.AircraftSeats
            .Where(s => ids.Contains(s.AircraftId))
            .GroupBy(s => s.AircraftId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
    }

    public async Task AddAsync(Aircraft aircraft, CancellationToken ct)
    {
        _db.Aircrafts.Add(aircraft);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<AircraftSeat>> GetSeatsAsync(Guid aircraftId, CancellationToken ct)
        => await _db.AircraftSeats
            .AsNoTracking()
            .Where(s => s.AircraftId == aircraftId)
            .OrderBy(s => s.SeatNumber.Length).ThenBy(s => s.SeatNumber)
            .ToListAsync(ct);

    public Task<AircraftSeat?> GetSeatAsync(Guid aircraftId, Guid seatId, CancellationToken ct)
        => _db.AircraftSeats.FirstOrDefaultAsync(s => s.AircraftId == aircraftId && s.Id == seatId, ct);

    public async Task<HashSet<string>> GetSeatNumbersAsync(Guid aircraftId, CancellationToken ct)
        => (await _db.AircraftSeats
                .Where(s => s.AircraftId == aircraftId)
                .Select(s => s.SeatNumber)
                .ToListAsync(ct))
            .ToHashSet();

    public async Task AddSeatsAsync(IEnumerable<AircraftSeat> seats, CancellationToken ct)
    {
        _db.AircraftSeats.AddRange(seats);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveSeatAsync(AircraftSeat seat, CancellationToken ct)
    {
        _db.AircraftSeats.Remove(seat);
        await _db.SaveChangesAsync(ct);
    }
}
