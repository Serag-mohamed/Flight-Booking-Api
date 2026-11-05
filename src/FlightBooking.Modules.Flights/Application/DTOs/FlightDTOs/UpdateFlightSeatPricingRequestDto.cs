using System.ComponentModel.DataAnnotations;
using FlightBooking.Modules.Flights.Domain.Enums;

namespace FlightBooking.Modules.Flights.Application.DTOs;

public record UpdateFlightSeatPricingRequestDto
{
    [Required, MinLength(1)]
    public Dictionary<CabinClass, decimal> PriceByCabinClass { get; init; } = new();
}
