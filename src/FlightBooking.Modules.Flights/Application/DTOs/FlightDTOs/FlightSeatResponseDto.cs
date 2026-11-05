using FlightBooking.Modules.Flights.Domain.Enums;

namespace FlightBooking.Modules.Flights.Application.DTOs;

public record FlightSeatResponseDto
{
    public Guid Id { get; init; }
    public string SeatNumber { get; init; } = string.Empty;
    public CabinClass CabinClass { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public SeatStatus Status { get; init; }
}
