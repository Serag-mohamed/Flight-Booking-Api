namespace FlightBooking.Shared.Contracts.Payments;

public interface IPaymentRefundService
{
    Task<bool> RefundBookingAsync(Guid bookingId, string reason, CancellationToken ct);
}
