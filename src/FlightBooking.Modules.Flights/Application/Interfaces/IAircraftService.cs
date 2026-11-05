using FlightBooking.Shared.Common;
using FlightBooking.Modules.Flights.Application.DTOs;

namespace FlightBooking.Modules.Flights.Application.Interfaces;

public interface IAircraftService
{
    Task<PagedResult<AircraftResponseDto>> GetAllAsync(AircraftQueryParams query, CancellationToken ct);
    Task<AircraftResponseDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<AircraftResponseDto> AddAsync(AddAircraftRequestDto request, CancellationToken ct);
    Task<AircraftResponseDto?> UpdateAsync(Guid id, UpdateAircraftRequestDto request, CancellationToken ct);
    Task<bool> SetActiveAsync(Guid id, bool isActive, CancellationToken ct);

    Task<IReadOnlyList<AircraftSeatResponseDto>?> GetSeatsAsync(Guid aircraftId, CancellationToken ct);
    Task<AircraftSeatResponseDto> AddSeatAsync(Guid aircraftId, AddAircraftSeatRequestDto request, CancellationToken ct);
    Task<IReadOnlyList<AircraftSeatResponseDto>> BulkAddSeatsAsync(Guid aircraftId, BulkAddAircraftSeatsRequestDto request, CancellationToken ct);
    Task<AircraftSeatResponseDto?> UpdateSeatAsync(Guid aircraftId, Guid seatId, UpdateAircraftSeatRequestDto request, CancellationToken ct);
    Task<bool> DeleteSeatAsync(Guid aircraftId, Guid seatId, CancellationToken ct);
}
