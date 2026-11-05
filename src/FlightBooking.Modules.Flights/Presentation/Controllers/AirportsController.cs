using FlightBooking.Shared.Common;
using FlightBooking.Modules.Flights.Application.DTOs;
using FlightBooking.Modules.Flights.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightBooking.Modules.Flights.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AirportsController : ControllerBase
{
    private readonly IAirportService _airportService;

    public AirportsController(IAirportService airportService)
    {
        _airportService = airportService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AirportResponseDto>>> GetAll(
        [FromQuery] AirportQueryParams query, CancellationToken ct)
    {
        return Ok(await _airportService.GetAllAsync(query, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AirportResponseDto>> GetById(Guid id, CancellationToken ct)
    {
        var airport = await _airportService.GetByIdAsync(id, ct);
        return airport is null ? NotFound() : Ok(airport);
    }

    [HttpGet("by-code/{iataCode}")]
    public async Task<ActionResult<AirportResponseDto>> GetByIataCode(string iataCode, CancellationToken ct)
    {
        var airport = await _airportService.GetByIataCodeAsync(iataCode, ct);
        return airport is null ? NotFound() : Ok(airport);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<AirportResponseDto>> Create(
        [FromBody] AddAirportRequestDto request, CancellationToken ct)
    {
        var created = await _airportService.AddAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AirportResponseDto>> Update(
        Guid id, [FromBody] UpdateAirportRequestDto request, CancellationToken ct)
    {
        var updated = await _airportService.UpdateAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        return await _airportService.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
