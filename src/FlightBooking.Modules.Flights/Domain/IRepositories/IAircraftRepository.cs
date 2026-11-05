using FlightBooking.Modules.Flights.Domain.Entities;

namespace FlightBooking.Modules.Flights.Domain.IRepositories;

public interface IAircraftRepository
{
    Task<(IReadOnlyList<Aircraft> Items, int TotalCount)> GetPagedAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken ct);

    Task<Aircraft?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> RegistrationExistsAsync(string registrationNumber, Guid? excludeId, CancellationToken ct);
    Task<int> CountSeatsAsync(Guid aircraftId, CancellationToken ct);
    Task<Dictionary<Guid, int>> GetSeatsCountsAsync(IEnumerable<Guid> aircraftIds, CancellationToken ct);

    Task AddAsync(Aircraft aircraft, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);

    Task<IReadOnlyList<AircraftSeat>> GetSeatsAsync(Guid aircraftId, CancellationToken ct);
    Task<AircraftSeat?> GetSeatAsync(Guid aircraftId, Guid seatId, CancellationToken ct);
    Task<HashSet<string>> GetSeatNumbersAsync(Guid aircraftId, CancellationToken ct);
    Task AddSeatsAsync(IEnumerable<AircraftSeat> seats, CancellationToken ct);
    Task RemoveSeatAsync(AircraftSeat seat, CancellationToken ct);
}
