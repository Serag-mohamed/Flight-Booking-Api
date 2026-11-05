using System.Security.Claims;
using FlightBooking.Modules.Payments.Application.DTOs;
using FlightBooking.Modules.Payments.Application.Interfaces;
using FlightBooking.Modules.Payments.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlightBooking.Modules.Payments.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
        => _paymentService = paymentService;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool IsAdmin => User.IsInRole("Admin");

    [HttpPost]
    public async Task<ActionResult<PaymentResponseDto>> Pay(
        [FromBody] PayRequestDto request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Missing header",
                detail: "The Idempotency-Key header is required.");

        var payment = await _paymentService.PayAsync(UserId, request, idempotencyKey, ct);

        return payment.Status switch
        {
            PaymentStatus.Succeeded => Ok(payment),
            PaymentStatus.Failed => StatusCode(StatusCodes.Status402PaymentRequired, payment),
            _ => Conflict(payment)
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentResponseDto>> GetById(Guid id, CancellationToken ct)
    {
        var payment = await _paymentService.GetByIdAsync(UserId, IsAdmin, id, ct);
        return payment is null ? NotFound() : Ok(payment);
    }

    [HttpGet("booking/{bookingId:guid}")]
    public async Task<ActionResult<IReadOnlyList<PaymentResponseDto>>> GetByBooking(Guid bookingId, CancellationToken ct)
        => Ok(await _paymentService.GetByBookingAsync(UserId, IsAdmin, bookingId, ct));
}
