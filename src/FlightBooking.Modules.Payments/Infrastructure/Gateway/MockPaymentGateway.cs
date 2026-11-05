using FlightBooking.Modules.Payments.Application.Interfaces;

namespace FlightBooking.Modules.Payments.Infrastructure.Gateway;

public class MockPaymentGateway : IPaymentGateway
{
    public Task<GatewayChargeResult> ChargeAsync(GatewayChargeRequest request, CancellationToken ct)
    {
        var reference = $"MOCK-{Guid.NewGuid():N}";

        var result = request.CardToken switch
        {
            "tok_success" => new GatewayChargeResult(true, reference, null),
            "tok_declined" => new GatewayChargeResult(false, reference, "The card was declined."),
            "tok_insufficient_funds" => new GatewayChargeResult(false, reference, "Insufficient funds."),
            _ => new GatewayChargeResult(false, reference, "Unknown test card token.")
        };

        return Task.FromResult(result);
    }

    public Task<GatewayRefundResult> RefundAsync(string providerReference, decimal amount, CancellationToken ct)
        => Task.FromResult(new GatewayRefundResult(true, $"MOCK-REF-{Guid.NewGuid():N}", null));
}
