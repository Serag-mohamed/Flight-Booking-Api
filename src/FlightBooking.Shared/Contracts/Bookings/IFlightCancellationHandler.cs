namespace FlightBooking.Shared.Contracts.Bookings;

public record FlightCancellationResult(int Cancelled, int Refunded, int Failed);

public interface IFlightCancellationHandler
{
    Task<FlightCancellationResult> HandleFlightCancelledAsync(Guid flightId, CancellationToken ct);
}
