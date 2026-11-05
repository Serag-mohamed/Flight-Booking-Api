using FlightBooking.Modules.Bookings.Domain.Enums;

namespace FlightBooking.Modules.Bookings.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BookingReference { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;

    public Guid FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public string OriginIata { get; set; } = string.Empty;
    public string DestinationIata { get; set; } = string.Empty;
    public DateTimeOffset DepartureUtc { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<BookingPassenger> Passengers { get; set; } = new List<BookingPassenger>();
    public ICollection<BookingSeat> Seats { get; set; } = new List<BookingSeat>();
}
