using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Modules.Bookings.Application.DTOs;

public record CreateBookingRequestDto
{
    [Required, MinLength(1), MaxLength(9)]
    public List<BookingPassengerRequestDto> Passengers { get; init; } = new();
}

public record BookingPassengerRequestDto
{
    [Required, StringLength(150)]
    public string FullName { get; init; } = string.Empty;

    public DateOnly DateOfBirth { get; init; }

    public Guid FlightSeatId { get; init; }
}
