using FlightBooking.Modules.Flights.Domain.Enums;

namespace FlightBooking.Modules.Flights.Application.DTOs;

public record AircraftSeatResponseDto
{
    public Guid Id { get; init; }
    public string SeatNumber { get; init; } = string.Empty;
    public CabinClass CabinClass { get; init; }
}
