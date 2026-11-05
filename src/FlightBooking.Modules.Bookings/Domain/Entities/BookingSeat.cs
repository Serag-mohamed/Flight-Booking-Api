namespace FlightBooking.Modules.Bookings.Domain.Entities;

public class BookingSeat
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Guid FlightSeatId { get; set; }
    public Guid PassengerId { get; set; }
    public decimal UnitPrice { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string CabinClass { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public Booking Booking { get; set; } = null!;
    public BookingPassenger Passenger { get; set; } = null!;
}
