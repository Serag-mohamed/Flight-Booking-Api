using FlightBooking.Shared.Common;
using FlightBooking.Modules.Flights.Application.DTOs;
using FlightBooking.Modules.Flights.Domain.Enums;

namespace FlightBooking.Modules.Flights.Application.Interfaces;

public interface IFlightService
{
    Task<PagedResult<FlightSearchResultDto>> SearchAsync(FlightSearchQueryParams query, CancellationToken ct);
    Task<FlightResponseDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<FlightSeatResponseDto>?> GetSeatsAsync(Guid flightId, CabinClass? cabinClass, bool availableOnly, CancellationToken ct);

    Task<PagedResult<FlightResponseDto>> GetAllAsync(FlightAdminQueryParams query, CancellationToken ct);
    Task<FlightResponseDto> CreateAsync(CreateFlightRequestDto request, CancellationToken ct);
    Task<FlightResponseDto?> UpdateAsync(Guid id, UpdateFlightRequestDto request, CancellationToken ct);
    Task<FlightResponseDto?> ChangeStatusAsync(Guid id, FlightStatus status, CancellationToken ct);
    Task<FlightResponseDto?> CancelAsync(Guid id, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);

    Task<int> GenerateSeatsAsync(Guid flightId, GenerateFlightSeatsRequestDto request, CancellationToken ct);
    Task<int> UpdateSeatPricingAsync(Guid flightId, UpdateFlightSeatPricingRequestDto request, CancellationToken ct);
    Task<FlightSeatResponseDto?> BlockSeatAsync(Guid flightId, Guid seatId, CancellationToken ct);
}
