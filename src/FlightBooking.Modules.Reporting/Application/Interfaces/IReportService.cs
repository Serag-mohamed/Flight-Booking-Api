using FlightBooking.Modules.Reporting.Application.DTOs;

namespace FlightBooking.Modules.Reporting.Application.Interfaces;

public interface IReportService
{
    Task<RevenueReportDto> GetRevenueAsync(ReportQueryParams query, CancellationToken ct);
    Task<BookingsByStatusReportDto> GetBookingsByStatusAsync(ReportQueryParams query, CancellationToken ct);
    Task<TopRoutesReportDto> GetTopRoutesAsync(ReportQueryParams query, CancellationToken ct);
    Task<FlightOccupancyReportDto> GetFlightOccupancyAsync(ReportQueryParams query, CancellationToken ct);
}
