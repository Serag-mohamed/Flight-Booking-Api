using System.Security.Claims;
using FlightBooking.Modules.Bookings.Application.DTOs;
using FlightBooking.Modules.Bookings.Application.Interfaces;
using FlightBooking.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightBooking.Modules.Bookings.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
        => _bookingService = bookingService;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool IsAdmin => User.IsInRole("Admin");

    [HttpPost]
    public async Task<ActionResult<BookingResponseDto>> Create(
        [FromBody] CreateBookingRequestDto request, CancellationToken ct)
    {
        var created = await _bookingService.CreateAsync(UserId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("my")]
    public async Task<ActionResult<PagedResult<BookingResponseDto>>> GetMy(
        [FromQuery] BookingQueryParams query, CancellationToken ct)
        => Ok(await _bookingService.GetMyBookingsAsync(UserId, query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingResponseDto>> GetById(Guid id, CancellationToken ct)
    {
        var booking = await _bookingService.GetByIdAsync(UserId, IsAdmin, id, ct);
        return booking is null ? NotFound() : Ok(booking);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<BookingResponseDto>> Cancel(Guid id, CancellationToken ct)
    {
        var booking = await _bookingService.CancelAsync(UserId, IsAdmin, id, ct);
        return booking is null ? NotFound() : Ok(booking);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<BookingResponseDto>>> GetAll(
        [FromQuery] BookingAdminQueryParams query, CancellationToken ct)
        => Ok(await _bookingService.GetAllAsync(query, ct));
}
