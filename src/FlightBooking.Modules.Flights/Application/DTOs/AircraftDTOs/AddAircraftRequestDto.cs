using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Modules.Flights.Application.DTOs;

public record AddAircraftRequestDto
{
    [Required, StringLength(50)]
    public string RegistrationNumber { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string Model { get; init; } = string.Empty;
}
