using FlightBooking.Modules.Flights.Domain.Entities;
using FlightBooking.Modules.Flights.Domain.IRepositories;
using FlightBooking.Modules.Flights.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Modules.Flights.Infrastructure.Repositories;

public class AirportRepository : IAirportRepository
{
    private readonly FlightsDbContext _db;

    public AirportRepository(FlightsDbContext db) => _db = db;

    public async Task<(IReadOnlyList<Airport> Items, int TotalCount)> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Airports.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a =>
                a.IataCode.Contains(search) ||
                a.Name.Contains(search) ||
                a.City.Contains(search) ||
                a.Country.Contains(search));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(a => a.Country).ThenBy(a => a.City)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<Airport?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Airports.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Airport?> GetByIataCodeAsync(string iataCode, CancellationToken ct)
        => _db.Airports.AsNoTracking().FirstOrDefaultAsync(a => a.IataCode == iataCode, ct);

    public Task<bool> IataCodeExistsAsync(string iataCode, Guid? excludeId, CancellationToken ct)
        => _db.Airports.AnyAsync(a => a.IataCode == iataCode && (excludeId == null || a.Id != excludeId), ct);

    public Task<bool> HasFlightsAsync(Guid id, CancellationToken ct)
        => _db.Flights.AnyAsync(f => f.OriginAirportId == id || f.DestinationAirportId == id, ct);

    public async Task AddAsync(Airport airport, CancellationToken ct)
    {
        _db.Airports.Add(airport);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    public async Task DeleteAsync(Airport airport, CancellationToken ct)
    {
        _db.Airports.Remove(airport);
        await _db.SaveChangesAsync(ct);
    }
}
