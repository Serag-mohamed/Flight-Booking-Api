using System.ComponentModel.DataAnnotations;
using FlightBooking.Modules.Flights.Domain.Enums;

namespace FlightBooking.Modules.Flights.Application.DTOs;

public record AddAircraftSeatRequestDto
{
    [Required, StringLength(5)]
    [RegularExpression("^[0-9]{1,3}[A-Za-z]$", ErrorMessage = "Seat number must look like 12A.")]
    public string SeatNumber { get; init; } = string.Empty;

    [EnumDataType(typeof(CabinClass))]
    public CabinClass CabinClass { get; init; }
}
