using SaaS.Api.Contracts;
using SaaS.Api.Domain;

namespace SaaS.Api.Services;

public sealed class BillingService(IConfiguration configuration)
{
    public CheckoutResponse? CreateCheckoutSession(Organization organization, string plan)
    {
        var normalizedPlan = plan.Trim().ToLowerInvariant();
        var priceId = normalizedPlan switch
        {
            "pro" => configuration["Stripe:ProPriceId"],
            "enterprise" => configuration["Stripe:EnterprisePriceId"],
            _ => null
        };

        if (priceId is null) return null;

        var successUrl = configuration["Stripe:SuccessUrl"] ?? "http://localhost:4200/billing?checkout=success";
        var cancelUrl = configuration["Stripe:CancelUrl"] ?? "http://localhost:4200/billing?checkout=cancelled";

        // Replace this with Stripe.net SessionService.Create in production.
        var sessionId = $"cs_test_{Guid.NewGuid():N}";
        var url = $"https://checkout.stripe.com/c/pay/{sessionId}#price={Uri.EscapeDataString(priceId)}&success={Uri.EscapeDataString(successUrl)}&cancel={Uri.EscapeDataString(cancelUrl)}";

        return new CheckoutResponse(sessionId, url, normalizedPlan);
    }
}
