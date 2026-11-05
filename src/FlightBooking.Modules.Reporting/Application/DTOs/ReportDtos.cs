using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Modules.Reporting.Application.DTOs;

public record ReportQueryParams
{
    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = "USD";

    [Range(1, 50)]
    public int Top { get; init; } = 10;
}

public record DailyRevenueDto
{
    public DateOnly Date { get; init; }
    public decimal Revenue { get; init; }
    public decimal Refunds { get; init; }
    public decimal Net { get; init; }
}

public record RevenueReportDto
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal TotalRevenue { get; init; }
    public decimal TotalRefunds { get; init; }
    public decimal NetRevenue { get; init; }
    public IReadOnlyList<DailyRevenueDto> Daily { get; init; } = Array.Empty<DailyRevenueDto>();
}

public record BookingStatusCountDto
{
    public string Status { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal TotalAmount { get; init; }
}

public record BookingsByStatusReportDto
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public int TotalBookings { get; init; }
    public IReadOnlyList<BookingStatusCountDto> Statuses { get; init; } = Array.Empty<BookingStatusCountDto>();
}

public record TopRouteDto
{
    public string Origin { get; init; } = string.Empty;
    public string Destination { get; init; } = string.Empty;
    public int Bookings { get; init; }
    public decimal Revenue { get; init; }
}

public record TopRoutesReportDto
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public string Currency { get; init; } = string.Empty;
    public IReadOnlyList<TopRouteDto> Routes { get; init; } = Array.Empty<TopRouteDto>();
}

public record FlightOccupancyDto
{
    public Guid FlightId { get; init; }
    public string FlightNumber { get; init; } = string.Empty;
    public DateTimeOffset DepartureUtc { get; init; }
    public int TotalSeats { get; init; }
    public int BookedSeats { get; init; }
    public int HeldSeats { get; init; }
    public int UnavailableSeats { get; init; }
    public int AvailableSeats { get; init; }

    public double OccupancyPercent { get; init; }
}

public record FlightOccupancyReportDto
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public IReadOnlyList<FlightOccupancyDto> Flights { get; init; } = Array.Empty<FlightOccupancyDto>();
}
