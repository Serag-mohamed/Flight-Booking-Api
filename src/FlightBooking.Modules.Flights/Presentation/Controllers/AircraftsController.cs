using FlightBooking.Shared.Common;
using FlightBooking.Modules.Flights.Application.DTOs;
using FlightBooking.Modules.Flights.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightBooking.Modules.Flights.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AircraftsController : ControllerBase
{
    private readonly IAircraftService _aircraftService;

    public AircraftsController(IAircraftService aircraftService)
    {
        _aircraftService = aircraftService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AircraftResponseDto>>> GetAll(
        [FromQuery] AircraftQueryParams query, CancellationToken ct)
    {
        return Ok(await _aircraftService.GetAllAsync(query, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AircraftResponseDto>> GetById(Guid id, CancellationToken ct)
    {
        var aircraft = await _aircraftService.GetByIdAsync(id, ct);
        return aircraft is null ? NotFound() : Ok(aircraft);
    }

    [HttpPost]
    public async Task<ActionResult<AircraftResponseDto>> Create(
        [FromBody] AddAircraftRequestDto request, CancellationToken ct)
    {
        var created = await _aircraftService.AddAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AircraftResponseDto>> Update(
        Guid id, [FromBody] UpdateAircraftRequestDto request, CancellationToken ct)
    {
        var updated = await _aircraftService.UpdateAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
        => await _aircraftService.SetActiveAsync(id, true, ct) ? NoContent() : NotFound();

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
        => await _aircraftService.SetActiveAsync(id, false, ct) ? NoContent() : NotFound();

    [HttpGet("{aircraftId:guid}/seats")]
    public async Task<ActionResult<IReadOnlyList<AircraftSeatResponseDto>>> GetSeats(
        Guid aircraftId, CancellationToken ct)
    {
        var seats = await _aircraftService.GetSeatsAsync(aircraftId, ct);
        return seats is null ? NotFound() : Ok(seats);
    }

    [HttpPost("{aircraftId:guid}/seats")]
    public async Task<ActionResult<AircraftSeatResponseDto>> AddSeat(
        Guid aircraftId, [FromBody] AddAircraftSeatRequestDto request, CancellationToken ct)
    {
        var seat = await _aircraftService.AddSeatAsync(aircraftId, request, ct);
        return CreatedAtAction(nameof(GetSeats), new { aircraftId }, seat);
    }

    [HttpPost("{aircraftId:guid}/seats/bulk")]
    public async Task<ActionResult<IReadOnlyList<AircraftSeatResponseDto>>> BulkAddSeats(
        Guid aircraftId, [FromBody] BulkAddAircraftSeatsRequestDto request, CancellationToken ct)
    {
        var seats = await _aircraftService.BulkAddSeatsAsync(aircraftId, request, ct);
        return CreatedAtAction(nameof(GetSeats), new { aircraftId }, seats);
    }

    [HttpPut("{aircraftId:guid}/seats/{seatId:guid}")]
    public async Task<ActionResult<AircraftSeatResponseDto>> UpdateSeat(
        Guid aircraftId, Guid seatId, [FromBody] UpdateAircraftSeatRequestDto request, CancellationToken ct)
    {
        var seat = await _aircraftService.UpdateSeatAsync(aircraftId, seatId, request, ct);
        return seat is null ? NotFound() : Ok(seat);
    }

    [HttpDelete("{aircraftId:guid}/seats/{seatId:guid}")]
    public async Task<IActionResult> DeleteSeat(Guid aircraftId, Guid seatId, CancellationToken ct)
        => await _aircraftService.DeleteSeatAsync(aircraftId, seatId, ct) ? NoContent() : NotFound();
}
