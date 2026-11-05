using FlightBooking.Shared.Common;
using FlightBooking.Modules.Flights.Application.DTOs;
using FlightBooking.Modules.Flights.Application.Interfaces;
using FlightBooking.Modules.Flights.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightBooking.Modules.Flights.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlightsController : ControllerBase
{
    private readonly IFlightService _flightService;

    public FlightsController(IFlightService flightService)
        => _flightService = flightService;

    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<FlightSearchResultDto>>> Search(
        [FromQuery] FlightSearchQueryParams query, CancellationToken ct)
        => Ok(await _flightService.SearchAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FlightResponseDto>> GetById(Guid id, CancellationToken ct)
    {
        var flight = await _flightService.GetByIdAsync(id, ct);
        return flight is null ? NotFound() : Ok(flight);
    }

    [HttpGet("{id:guid}/seats")]
    public async Task<ActionResult<IReadOnlyList<FlightSeatResponseDto>>> GetSeats(
        Guid id, [FromQuery] CabinClass? cabinClass, CancellationToken ct)
    {
        var seats = await _flightService.GetSeatsAsync(id, cabinClass, availableOnly: false, ct);
        return seats is null ? NotFound() : Ok(seats);
    }

    [HttpGet("{id:guid}/seats/available")]
    public async Task<ActionResult<IReadOnlyList<FlightSeatResponseDto>>> GetAvailableSeats(
        Guid id, [FromQuery] CabinClass? cabinClass, CancellationToken ct)
    {
        var seats = await _flightService.GetSeatsAsync(id, cabinClass, availableOnly: true, ct);
        return seats is null ? NotFound() : Ok(seats);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<FlightResponseDto>>> GetAll(
        [FromQuery] FlightAdminQueryParams query, CancellationToken ct)
        => Ok(await _flightService.GetAllAsync(query, ct));

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<FlightResponseDto>> Create(
        [FromBody] CreateFlightRequestDto request, CancellationToken ct)
    {
        var created = await _flightService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FlightResponseDto>> Update(
        Guid id, [FromBody] UpdateFlightRequestDto request, CancellationToken ct)
    {
        var updated = await _flightService.UpdateAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<FlightResponseDto>> ChangeStatus(
        Guid id, [FromBody] ChangeFlightStatusRequestDto request, CancellationToken ct)
    {
        var updated = await _flightService.ChangeStatusAsync(id, request.Status, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<FlightResponseDto>> Cancel(Guid id, CancellationToken ct)
    {
        var cancelled = await _flightService.CancelAsync(id, ct);
        return cancelled is null ? NotFound() : Ok(cancelled);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => await _flightService.DeleteAsync(id, ct) ? NoContent() : NotFound();

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/seats/generate")]
    public async Task<IActionResult> GenerateSeats(
        Guid id, [FromBody] GenerateFlightSeatsRequestDto request, CancellationToken ct)
    {
        var count = await _flightService.GenerateSeatsAsync(id, request, ct);
        return Ok(new { generatedSeats = count });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}/seats/pricing")]
    public async Task<IActionResult> UpdateSeatPricing(
        Guid id, [FromBody] UpdateFlightSeatPricingRequestDto request, CancellationToken ct)
    {
        var count = await _flightService.UpdateSeatPricingAsync(id, request, ct);
        return Ok(new { updatedSeats = count });
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/seats/{seatId:guid}/block")]
    public async Task<ActionResult<FlightSeatResponseDto>> BlockSeat(
        Guid id, Guid seatId, CancellationToken ct)
    {
        var seat = await _flightService.BlockSeatAsync(id, seatId, ct);
        return seat is null ? NotFound() : Ok(seat);
    }
}
