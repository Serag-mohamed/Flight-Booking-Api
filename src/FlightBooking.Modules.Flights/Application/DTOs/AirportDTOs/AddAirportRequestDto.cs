using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Modules.Flights.Application.DTOs;

public record AddAirportRequestDto
{
    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "IATA code must be exactly 3 letters.")]
    [RegularExpression("^[a-zA-Z]{3}$", ErrorMessage = "IATA code must contain letters only.")]
    public string IataCode { get; init; } = string.Empty;

    [Required, StringLength(150)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string Country { get; init; } = string.Empty;

    [Required, StringLength(50)]
    public string TimeZoneId { get; init; } = string.Empty;
}
