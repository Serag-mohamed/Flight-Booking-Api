using FlightBooking.Modules.Flights.Domain.Entities;

namespace FlightBooking.Modules.Flights.Domain.IRepositories;

public interface IAirportRepository
{
    Task<(IReadOnlyList<Airport> Items, int TotalCount)> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken ct);

    Task<Airport?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Airport?> GetByIataCodeAsync(string iataCode, CancellationToken ct);
    Task<bool> IataCodeExistsAsync(string iataCode, Guid? excludeId, CancellationToken ct);
    Task<bool> HasFlightsAsync(Guid id, CancellationToken ct);

    Task AddAsync(Airport airport, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    Task DeleteAsync(Airport airport, CancellationToken ct);
}
