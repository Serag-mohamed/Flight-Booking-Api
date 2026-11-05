using System.ComponentModel.DataAnnotations;
using FlightBooking.Modules.Flights.Domain.Enums;

namespace FlightBooking.Modules.Flights.Application.DTOs;

public record BulkAddAircraftSeatsRequestDto
{
    [Range(1, 100)]
    public int FromRow { get; init; }

    [Range(1, 100)]
    public int ToRow { get; init; }

    [Required, MinLength(1), MaxLength(10)]
    public List<string> Letters { get; init; } = new();

    [EnumDataType(typeof(CabinClass))]
    public CabinClass CabinClass { get; init; }
}
