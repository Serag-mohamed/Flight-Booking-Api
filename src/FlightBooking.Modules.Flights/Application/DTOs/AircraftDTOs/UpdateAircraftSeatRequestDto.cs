using System.ComponentModel.DataAnnotations;
using FlightBooking.Modules.Flights.Domain.Enums;

namespace FlightBooking.Modules.Flights.Application.DTOs;

public record UpdateAircraftSeatRequestDto
{
    [EnumDataType(typeof(CabinClass))]
    public CabinClass CabinClass { get; init; }
}
