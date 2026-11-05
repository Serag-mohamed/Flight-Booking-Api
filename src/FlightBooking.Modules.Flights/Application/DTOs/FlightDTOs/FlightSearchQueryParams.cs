using System.ComponentModel.DataAnnotations;
using FlightBooking.Modules.Flights.Domain.Enums;

namespace FlightBooking.Modules.Flights.Application.DTOs;

public record FlightSearchQueryParams
{
    [Required, StringLength(3, MinimumLength = 3)]
    public string Origin { get; init; } = string.Empty;

    [Required, StringLength(3, MinimumLength = 3)]
    public string Destination { get; init; } = string.Empty;

    public DateOnly Date { get; init; }

    [Range(1, 9)]
    public int Passengers { get; init; } = 1;

    public CabinClass? CabinClass { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
