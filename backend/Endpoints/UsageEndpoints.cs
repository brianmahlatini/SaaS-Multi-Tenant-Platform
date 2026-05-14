using SaaS.Api.Contracts;
using SaaS.Api.Domain;
using SaaS.Api.Persistence;
using SaaS.Api.Security;
using SaaS.Api.Services;

namespace SaaS.Api.Endpoints;

public static class UsageEndpoints
{
    public static IEndpointRouteBuilder MapUsageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/usage").WithTags("Usage");

        group.MapGet("/", Summary);
        group.MapPost("/ingest", Ingest);

        return app;
    }

    private static IResult Summary(HttpContext http, PlatformStore store)
    {
        var auth = CurrentUser.From(http, store);
        if (auth is null) return Results.Unauthorized();

        var events = store.UsageEvents.Values
            .Where(e => e.OrganizationId == auth.Organization.Id)
            .OrderByDescending(e => e.OccurredAt)
            .Take(50)
            .ToList();

        var daily = events
            .GroupBy(e => e.OccurredAt.UtcDateTime.Date)
            .OrderBy(g => g.Key)
            .Select(g => new UsagePointDto(g.Key.ToString("yyyy-MM-dd"), g.Sum(e => e.Units), g.Count()))
            .ToList();

        return Results.Ok(new UsageSummaryDto(
            events.Sum(e => e.Units),
            events.Count,
            events.Count(e => e.StatusCode >= 400),
            daily,
            events.Select(UsageEventDto.From).ToList()));
    }

    private static IResult Ingest(HttpContext http, UsageIngestRequest request, PlatformStore store, ApiKeyService keys)
    {
        var auth = keys.Authenticate(http, store);
        if (auth is null) return Results.Unauthorized();

        var usage = new UsageEvent(
            Guid.NewGuid(),
            auth.Key.OrganizationId,
            auth.Key.Id,
            string.IsNullOrWhiteSpace(request.Path) ? "/" : request.Path,
            string.IsNullOrWhiteSpace(request.Method) ? "GET" : request.Method.ToUpperInvariant(),
            request.StatusCode,
            Math.Max(1, request.Units),
            DateTimeOffset.UtcNow);

        store.UsageEvents[usage.Id] = usage;
        store.ApiKeys[auth.Key.Id] = auth.Key with { LastUsedAt = DateTimeOffset.UtcNow };

        return Results.Accepted($"/api/usage/{usage.Id}", UsageEventDto.From(usage));
    }
}
