namespace FlightBooking.Modules.Flights.Application.DTOs;

public record AircraftResponseDto
{
    public Guid Id { get; init; }
    public string RegistrationNumber { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int SeatsCount { get; init; }
}
