namespace FlightBooking.Shared.Contracts.Bookings;

public record BookingPaymentInfo(
    Guid BookingId,
    string UserId,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset ExpiresAtUtc,
    bool IsPayable);

public interface IBookingLifecycleService
{
    Task<BookingPaymentInfo?> GetForPaymentAsync(Guid bookingId, CancellationToken ct);

    Task ConfirmPaidAsync(Guid bookingId, CancellationToken ct);
}
