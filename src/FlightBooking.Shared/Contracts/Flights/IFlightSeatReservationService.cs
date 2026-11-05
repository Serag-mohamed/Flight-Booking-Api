namespace FlightBooking.Shared.Contracts.Flights;

public record HeldSeatInfo(Guid SeatId, string SeatNumber, string CabinClass, decimal Price);

public record SeatHoldResult(
    Guid FlightId,
    string FlightNumber,
    string OriginIata,
    string DestinationIata,
    DateTimeOffset DepartureUtc,
    string Currency,
    IReadOnlyList<HeldSeatInfo> Seats);

public interface IFlightSeatReservationService
{
    Task<SeatHoldResult> HoldSeatsAsync(IReadOnlyCollection<Guid> flightSeatIds, CancellationToken ct);

    Task ConfirmSeatsAsync(IReadOnlyCollection<Guid> flightSeatIds, CancellationToken ct);

    Task ReleaseSeatsAsync(IReadOnlyCollection<Guid> flightSeatIds, CancellationToken ct);
}
