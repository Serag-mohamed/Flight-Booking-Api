using FlightBooking.Modules.Bookings.Application.DTOs;
using FlightBooking.Shared.Common;

namespace FlightBooking.Modules.Bookings.Application.Interfaces;

public interface IBookingService
{
    Task<BookingResponseDto> CreateAsync(string userId, CreateBookingRequestDto request, CancellationToken ct);
    Task<PagedResult<BookingResponseDto>> GetMyBookingsAsync(string userId, BookingQueryParams query, CancellationToken ct);
    Task<PagedResult<BookingResponseDto>> GetAllAsync(BookingAdminQueryParams query, CancellationToken ct);
    Task<BookingResponseDto?> GetByIdAsync(string userId, bool isAdmin, Guid id, CancellationToken ct);
    Task<BookingResponseDto?> CancelAsync(string userId, bool isAdmin, Guid id, CancellationToken ct);
}
