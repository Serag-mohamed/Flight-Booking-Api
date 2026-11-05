namespace FlightBooking.Modules.Reporting.Domain.ReadModels;

public class BookingRow
{
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string OriginIata { get; set; } = string.Empty;
    public string DestinationIata { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public class PaymentRow
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public class RefundRow
{
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public class FlightRow
{
    public Guid Id { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public DateTimeOffset DepartureUtc { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class FlightSeatRow
{
    public Guid FlightId { get; set; }
    public string Status { get; set; } = string.Empty;
}
