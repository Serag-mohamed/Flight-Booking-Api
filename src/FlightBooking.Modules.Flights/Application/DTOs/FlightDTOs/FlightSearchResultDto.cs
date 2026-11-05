namespace FlightBooking.Modules.Flights.Application.DTOs;

public record FlightSearchResultDto
{
    public FlightResponseDto Flight { get; init; } = null!;
    public int AvailableSeats { get; init; }
    public decimal? LowestPrice { get; init; }
    public string? Currency { get; init; }
}
