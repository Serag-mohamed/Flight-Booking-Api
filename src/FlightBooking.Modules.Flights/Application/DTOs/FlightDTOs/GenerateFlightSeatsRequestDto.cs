using System.ComponentModel.DataAnnotations;
using FlightBooking.Modules.Flights.Domain.Enums;

namespace FlightBooking.Modules.Flights.Application.DTOs;

public record GenerateFlightSeatsRequestDto
{
    [Required, MinLength(1)]
    public Dictionary<CabinClass, decimal> PriceByCabinClass { get; init; } = new();

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = "USD";
}
