using System.ComponentModel.DataAnnotations;
using FlightBooking.Modules.Payments.Domain.Enums;

namespace FlightBooking.Modules.Payments.Application.DTOs;

public record PayRequestDto
{
    [Required]
    public Guid BookingId { get; init; }

    [Required, StringLength(50)]
    public string CardToken { get; init; } = "tok_success";
}

public record PaymentResponseDto
{
    public Guid Id { get; init; }
    public Guid BookingId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public PaymentStatus Status { get; init; }
    public string ProviderReference { get; init; } = string.Empty;
    public string? FailureReason { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}
