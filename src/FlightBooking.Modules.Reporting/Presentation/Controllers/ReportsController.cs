using FlightBooking.Modules.Reporting.Application.DTOs;
using FlightBooking.Modules.Reporting.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightBooking.Modules.Reporting.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueReportDto>> Revenue(
        [FromQuery] ReportQueryParams query, CancellationToken ct)
        => Ok(await _reports.GetRevenueAsync(query, ct));

    [HttpGet("bookings-by-status")]
    public async Task<ActionResult<BookingsByStatusReportDto>> BookingsByStatus(
        [FromQuery] ReportQueryParams query, CancellationToken ct)
        => Ok(await _reports.GetBookingsByStatusAsync(query, ct));

    [HttpGet("top-routes")]
    public async Task<ActionResult<TopRoutesReportDto>> TopRoutes(
        [FromQuery] ReportQueryParams query, CancellationToken ct)
        => Ok(await _reports.GetTopRoutesAsync(query, ct));

    [HttpGet("flight-occupancy")]
    public async Task<ActionResult<FlightOccupancyReportDto>> FlightOccupancy(
        [FromQuery] ReportQueryParams query, CancellationToken ct)
        => Ok(await _reports.GetFlightOccupancyAsync(query, ct));
}
