using System.Text.Json;
using SaaS.Api.Contracts;
using SaaS.Api.Persistence;
using SaaS.Api.Security;
using SaaS.Api.Services;

namespace SaaS.Api.Endpoints;

public static class BillingEndpoints
{
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/billing").WithTags("Billing");

        group.MapGet("/subscription", Subscription).RequireRateLimiting("dashboard");
        group.MapPost("/checkout", Checkout).RequireRateLimiting("dashboard");
        group.MapPost("/webhook", Webhook).RequireRateLimiting("dashboard");

        return app;
    }

    private static IResult Subscription(HttpContext http, PlatformStore store)
    {
        var auth = CurrentUser.From(http, store);
        if (auth is null) return Results.Unauthorized();

        return Results.Ok(SubscriptionDto.From(store.Subscriptions[auth.Organization.Id], auth.Organization.Plan));
    }

    private static IResult Checkout(HttpContext http, CheckoutRequest request, PlatformStore store, BillingService billing)
    {
        var auth = CurrentUser.From(http, store);
        if (auth is null) return Results.Unauthorized();
        if (!auth.CanManage()) return Results.Forbid();

        var checkout = billing.CreateCheckoutSession(auth.Organization, request.Plan);
        return checkout is null
            ? Results.BadRequest(new { message = "Unknown plan. Use pro or enterprise." })
            : Results.Ok(checkout);
    }

    private static async Task<IResult> Webhook(HttpContext http, PlatformStore store, StripeWebhookService webhooks)
    {
        using var reader = new StreamReader(http.Request.Body);
        var payload = await reader.ReadToEndAsync();
        using var doc = JsonDocument.Parse(payload);

        var result = webhooks.Apply(doc.RootElement, store);
        return result is WebhookResult.Duplicate
            ? Results.Ok(new { received = true, duplicate = true })
            : Results.Ok(new { received = true });
    }
}
