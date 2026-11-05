using FlightBooking.Modules.Bookings.Domain.Enums;

namespace FlightBooking.Modules.Bookings.Application.DTOs;

public record BookingResponseDto
{
    public Guid Id { get; init; }
    public string BookingReference { get; init; } = string.Empty;
    public Guid FlightId { get; init; }
    public string FlightNumber { get; init; } = string.Empty;
    public string OriginIata { get; init; } = string.Empty;
    public string DestinationIata { get; init; } = string.Empty;
    public DateTimeOffset DepartureUtc { get; init; }
    public BookingStatus Status { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public IReadOnlyList<BookingPassengerResponseDto> Passengers { get; init; } = Array.Empty<BookingPassengerResponseDto>();
}

public record BookingPassengerResponseDto
{
    public Guid PassengerId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public DateOnly DateOfBirth { get; init; }
    public PassengerType PassengerType { get; init; }
    public Guid FlightSeatId { get; init; }
    public string SeatNumber { get; init; } = string.Empty;
    public string CabinClass { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
}
