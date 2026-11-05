using FlightBooking.Modules.Payments.Domain.Enums;

namespace FlightBooking.Modules.Payments.Domain.Entities;

public class Refund
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Payment Payment { get; set; } = null!;
}
