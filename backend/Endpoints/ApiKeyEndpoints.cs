using SaaS.Api.Contracts;
using SaaS.Api.Persistence;
using SaaS.Api.Security;
using SaaS.Api.Services;

namespace SaaS.Api.Endpoints;

public static class ApiKeyEndpoints
{
    public static IEndpointRouteBuilder MapApiKeyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/api-keys").WithTags("API Keys");

        group.MapGet("/", List);
        group.MapPost("/", Create);
        group.MapDelete("/{id:guid}", Revoke);

        return app;
    }

    private static IResult List(HttpContext http, PlatformStore store)
    {
        var auth = CurrentUser.From(http, store);
        if (auth is null) return Results.Unauthorized();

        var keys = store.ApiKeys.Values
            .Where(k => k.OrganizationId == auth.Organization.Id)
            .OrderByDescending(k => k.CreatedAt)
            .Select(ApiKeyDto.From)
            .ToList();

        return Results.Ok(keys);
    }

    private static IResult Create(HttpContext http, CreateApiKeyRequest request, PlatformStore store, ApiKeyService keys)
    {
        var auth = CurrentUser.From(http, store);
        if (auth is null) return Results.Unauthorized();
        if (!auth.CanManage()) return Results.Forbid();

        var created = keys.Create(auth.Organization.Id, request.Name);
        store.ApiKeys[created.Record.Id] = created.Record;

        return Results.Ok(new CreatedApiKeyDto(ApiKeyDto.From(created.Record), created.PlainTextKey));
    }

    private static IResult Revoke(HttpContext http, Guid id, PlatformStore store)
    {
        var auth = CurrentUser.From(http, store);
        if (auth is null) return Results.Unauthorized();
        if (!auth.CanManage()) return Results.Forbid();
        if (!store.ApiKeys.TryGetValue(id, out var key) || key.OrganizationId != auth.Organization.Id) return Results.NotFound();

        store.ApiKeys[id] = key with { RevokedAt = DateTimeOffset.UtcNow };
        return Results.NoContent();
    }
}
