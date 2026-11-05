using FlightBooking.Modules.Flights.Application.DTOs;
using FlightBooking.Modules.Flights.Domain.Entities;

namespace FlightBooking.Modules.Flights.Infrastructure.Services;

internal static class AirportMappings
{
    public static AirportResponseDto ToDto(this Airport a) => new()
    {
        Id = a.Id,
        IataCode = a.IataCode,
        Name = a.Name,
        City = a.City,
        Country = a.Country,
        TimeZoneId = a.TimeZoneId
    };
}
