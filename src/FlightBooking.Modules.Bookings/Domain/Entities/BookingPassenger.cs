using FlightBooking.Modules.Bookings.Domain.Enums;

namespace FlightBooking.Modules.Bookings.Domain.Entities;

public class BookingPassenger
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public PassengerType PassengerType { get; set; }

    public Booking Booking { get; set; } = null!;
}
