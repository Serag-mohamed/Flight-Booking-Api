namespace FlightBooking.Modules.Payments.Domain.Enums;

public enum PaymentStatus
{
    Pending,
    Succeeded,
    Failed,
    Refunded
}

public enum RefundStatus
{
    Pending,
    Succeeded,
    Failed
}
