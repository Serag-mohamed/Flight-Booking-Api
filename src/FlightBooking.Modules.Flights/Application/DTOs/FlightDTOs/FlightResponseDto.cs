using FlightBooking.Modules.Flights.Domain.Enums;

namespace FlightBooking.Modules.Flights.Application.DTOs;

public record FlightResponseDto
{
    public Guid Id { get; init; }
    public string FlightNumber { get; init; } = string.Empty;
    public AirportResponseDto OriginAirport { get; init; } = null!;
    public AirportResponseDto DestinationAirport { get; init; } = null!;
    public Guid AircraftId { get; init; }
    public string AircraftModel { get; init; } = string.Empty;
    public DateTimeOffset DepartureUtc { get; init; }
    public DateTimeOffset ArrivalUtc { get; init; }
    public FlightStatus Status { get; init; }
}
