namespace FlightBooking.Modules.Flights.Application.DTOs;

public record AirportResponseDto
{
    public Guid Id { get; init; }
    public string IataCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string TimeZoneId { get; init; } = string.Empty;
}
