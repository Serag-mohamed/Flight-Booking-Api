using System.ComponentModel.DataAnnotations;
using FlightBooking.Modules.Bookings.Domain.Enums;

namespace FlightBooking.Modules.Bookings.Application.DTOs;

public record BookingQueryParams
{
    public BookingStatus? Status { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public record BookingAdminQueryParams
{
    public BookingStatus? Status { get; init; }
    public string? UserId { get; init; }
    public Guid? FlightId { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
