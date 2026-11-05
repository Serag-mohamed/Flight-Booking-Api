using FlightBooking.Shared.Common;
using FlightBooking.Modules.Flights.Application.DTOs;

namespace FlightBooking.Modules.Flights.Application.Interfaces;

public interface IAirportService
{
    Task<PagedResult<AirportResponseDto>> GetAllAsync(AirportQueryParams query, CancellationToken ct);
    Task<AirportResponseDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<AirportResponseDto?> GetByIataCodeAsync(string iataCode, CancellationToken ct);
    Task<AirportResponseDto> AddAsync(AddAirportRequestDto request, CancellationToken ct);
    Task<AirportResponseDto?> UpdateAsync(Guid id, UpdateAirportRequestDto request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
