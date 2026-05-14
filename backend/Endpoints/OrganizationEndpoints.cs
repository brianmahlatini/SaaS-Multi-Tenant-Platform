using SaaS.Api.Contracts;
using SaaS.Api.Domain;
using SaaS.Api.Persistence;
using SaaS.Api.Security;

namespace SaaS.Api.Endpoints;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations").WithTags("Organizations");

        group.MapGet("/list", List).RequireRateLimiting("dashboard");
        group.MapPost("/", Create).RequireRateLimiting("dashboard");
        group.MapGet("/current", Current).RequireRateLimiting("dashboard");

        return app;
    }

    private static IResult List(HttpContext http, PlatformStore store)
    {
        var auth = CurrentUser.From(http, store);
        if (auth is null) return Results.Unauthorized();

        var organizations = store.Memberships.Values
            .Where(m => m.UserId == auth.User.Id)
            .Select(m =>
            {
                var organization = store.Organizations[m.OrganizationId];
                return new OrganizationDto(organization.Id, organization.Name, m.Role.ToString(), organization.Plan);
            })
            .ToList();

        return Results.Ok(organizations);
    }

    private static IResult Create(HttpContext http, CreateOrganizationRequest request, PlatformStore store, TokenService tokens)
    {
        var auth = CurrentUser.From(http, store);
        if (auth is null) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { message = "Organization name is required." });

        var organization = new Organization(Guid.NewGuid(), request.Name.Trim(), "free", null, null, DateTimeOffset.UtcNow);
        var membership = new Membership(Guid.NewGuid(), auth.User.Id, organization.Id, Role.Owner, DateTimeOffset.UtcNow);

        store.Organizations[organization.Id] = organization;
        store.Memberships[membership.Id] = membership;
        store.Subscriptions[organization.Id] = new Subscription(Guid.NewGuid(), organization.Id, "free", "active", null, null, DateTimeOffset.UtcNow);

        return Results.Ok(SessionResponse.From(auth.User, organization, tokens.CreateToken(auth.User, organization, Role.Owner), Role.Owner));
    }

    private static IResult Current(HttpContext http, PlatformStore store)
    {
        var auth = CurrentUser.From(http, store);
        return auth is null
            ? Results.Unauthorized()
            : Results.Ok(new OrganizationDto(auth.Organization.Id, auth.Organization.Name, auth.Role.ToString(), auth.Organization.Plan));
    }
}
