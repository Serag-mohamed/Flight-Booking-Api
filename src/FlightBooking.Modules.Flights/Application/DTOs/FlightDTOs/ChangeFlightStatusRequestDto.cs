using System.ComponentModel.DataAnnotations;
using FlightBooking.Modules.Flights.Domain.Enums;

namespace FlightBooking.Modules.Flights.Application.DTOs;

public record ChangeFlightStatusRequestDto
{
    [EnumDataType(typeof(FlightStatus))]
    public FlightStatus Status { get; init; }
}
