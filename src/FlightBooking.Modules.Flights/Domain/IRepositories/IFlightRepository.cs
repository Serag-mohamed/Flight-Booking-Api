using FlightBooking.Modules.Flights.Domain.Entities;
using FlightBooking.Modules.Flights.Domain.Enums;

namespace FlightBooking.Modules.Flights.Domain.IRepositories;

public interface IFlightRepository
{
    Task<(IReadOnlyList<Flight> Items, int TotalCount)> SearchAsync(
        Guid originId, Guid destinationId, DateTimeOffset fromUtc, DateTimeOffset toUtc,
        CabinClass? cabinClass, int passengers, int page, int pageSize, CancellationToken ct);

    Task<(IReadOnlyList<Flight> Items, int TotalCount)> GetPagedAsync(
        string? flightNumber, FlightStatus? status, DateTimeOffset? fromUtc, DateTimeOffset? toUtc,
        int page, int pageSize, CancellationToken ct);

    Task<Flight?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Flight?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct);

    Task<Dictionary<Guid, (int Available, decimal? Lowest, string? Currency)>> GetSeatSummariesAsync(
        IEnumerable<Guid> flightIds, CabinClass? cabinClass, CancellationToken ct);

    Task<bool> FlightNumberExistsOnDateAsync(
        string flightNumber, DateTimeOffset fromUtc, DateTimeOffset toUtc, Guid? excludeId, CancellationToken ct);

    Task<bool> AircraftHasOverlapAsync(
        Guid aircraftId, DateTimeOffset departureUtc, DateTimeOffset arrivalUtc, Guid? excludeFlightId, CancellationToken ct);

    Task<bool> HasBookedOrHeldSeatsAsync(Guid flightId, CancellationToken ct);

    Task<IReadOnlyList<FlightSeat>> GetSeatsAsync(Guid flightId, CabinClass? cabinClass, bool availableOnly, CancellationToken ct);
    Task<List<FlightSeat>> GetTrackedSeatsAsync(Guid flightId, CancellationToken ct);
    Task<FlightSeat?> GetSeatAsync(Guid flightId, Guid seatId, CancellationToken ct);
    Task<bool> HasSeatsAsync(Guid flightId, CancellationToken ct);
    Task AddSeatsAsync(IEnumerable<FlightSeat> seats, CancellationToken ct);

    Task<Airport?> GetAirportAsync(Guid id, CancellationToken ct);
    Task<Airport?> GetAirportByIataAsync(string iata, CancellationToken ct);
    Task<Aircraft?> GetAircraftWithSeatsAsync(Guid id, CancellationToken ct);

    Task AddAsync(Flight flight, CancellationToken ct);
    Task RemoveAsync(Flight flight, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
