using FlightBooking.Modules.Payments.Application.DTOs;

namespace FlightBooking.Modules.Payments.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponseDto> PayAsync(string userId, PayRequestDto request, string idempotencyKey, CancellationToken ct);
    Task<PaymentResponseDto?> GetByIdAsync(string userId, bool isAdmin, Guid id, CancellationToken ct);
    Task<IReadOnlyList<PaymentResponseDto>> GetByBookingAsync(string userId, bool isAdmin, Guid bookingId, CancellationToken ct);
}
