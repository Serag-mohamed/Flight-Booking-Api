using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Modules.Flights.Application.DTOs;

public record CreateFlightRequestDto
{
    [Required, StringLength(20)]
    [RegularExpression("^[A-Za-z0-9]{2,3}[0-9]{1,4}$", ErrorMessage = "Flight number must look like MS123.")]
    public string FlightNumber { get; init; } = string.Empty;

    public Guid OriginAirportId { get; init; }
    public Guid DestinationAirportId { get; init; }
    public Guid AircraftId { get; init; }

    public DateTimeOffset DepartureUtc { get; init; }
    public DateTimeOffset ArrivalUtc { get; init; }
}
