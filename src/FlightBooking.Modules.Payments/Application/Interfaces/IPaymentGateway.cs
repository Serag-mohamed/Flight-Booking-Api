namespace FlightBooking.Modules.Payments.Application.Interfaces;

public record GatewayChargeRequest(decimal Amount, string Currency, string CardToken);
public record GatewayChargeResult(bool Succeeded, string Reference, string? FailureReason);
public record GatewayRefundResult(bool Succeeded, string Reference, string? FailureReason);

public interface IPaymentGateway
{
    Task<GatewayChargeResult> ChargeAsync(GatewayChargeRequest request, CancellationToken ct);
    Task<GatewayRefundResult> RefundAsync(string providerReference, decimal amount, CancellationToken ct);
}
