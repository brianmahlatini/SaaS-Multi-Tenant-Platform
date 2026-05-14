using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using SaaS.Api.Contracts;
using SaaS.Api.Domain;
using SaaS.Api.Infrastructure.Caching;
using SaaS.Api.Infrastructure.Messaging;
using SaaS.Api.Persistence;
using SaaS.Api.Persistence.Mongo;
using SaaS.Api.Persistence.Postgres;
using SaaS.Api.Realtime;
using SaaS.Api.Security;
using SaaS.Api.Services;

namespace SaaS.Api.Endpoints;

public static class UsageEndpoints
{
    public static IEndpointRouteBuilder MapUsageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/usage").WithTags("Usage");

        group.MapGet("/", Summary).RequireRateLimiting("dashboard");
        group.MapPost("/ingest", Ingest).RequireRateLimiting("ingest");

        return app;
    }

    private static async Task<IResult> Summary(HttpContext http, PlatformStore store, IDistributedCache cache)
    {
        var auth = CurrentUser.From(http, store);
        if (auth is null) return Results.Unauthorized();

        var cacheKey = CacheKeys.UsageSummary(auth.Organization.Id);
        var cached = await cache.GetStringAsync(cacheKey, http.RequestAborted);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            return Results.Content(cached, "application/json");
        }

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

        var summary = new UsageSummaryDto(
            events.Sum(e => e.Units),
            events.Count,
            events.Count(e => e.StatusCode >= 400),
            daily,
            events.Select(UsageEventDto.From).ToList());

        await cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(summary),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) },
            http.RequestAborted);

        return Results.Ok(summary);
    }

    private static async Task<IResult> Ingest(
        HttpContext http,
        UsageIngestRequest request,
        PlatformStore store,
        ApiKeyService keys,
        IDistributedCache cache,
        IEventBus events,
        MongoUsageService mongo,
        PostgresProjectionService postgres,
        IHubContext<RealtimeHub> hub)
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
        await cache.RemoveAsync(CacheKeys.UsageSummary(auth.Key.OrganizationId), http.RequestAborted);
        var platformEvent = new PlatformEvent("usage.ingested", auth.Key.OrganizationId, UsageEventDto.From(usage), DateTimeOffset.UtcNow);
        await postgres.SaveSnapshotAsync(store, http.RequestAborted);
        await mongo.StoreUsageAsync(usage, http.RequestAborted);
        await mongo.StoreAuditAsync(platformEvent, http.RequestAborted);
        await events.PublishAsync(platformEvent, http.RequestAborted);
        await hub.Clients.Group(RealtimeHub.OrganizationGroup(auth.Key.OrganizationId)).SendAsync("usageUpdated", UsageEventDto.From(usage), http.RequestAborted);

        return Results.Accepted($"/api/usage/{usage.Id}", UsageEventDto.From(usage));
    }
}
