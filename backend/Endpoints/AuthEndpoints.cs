using SaaS.Api.Contracts;
using SaaS.Api.Domain;
using SaaS.Api.Persistence;
using SaaS.Api.Security;

namespace SaaS.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", Register).RequireRateLimiting("dashboard");
        group.MapPost("/login", Login).RequireRateLimiting("dashboard");
        group.MapGet("/me", Me).RequireRateLimiting("dashboard");

        return app;
    }

    private static IResult Register(RegisterRequest request, PlatformStore store, TokenService tokens)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || request.Password.Length < 8 || string.IsNullOrWhiteSpace(request.OrganizationName))
        {
            return Results.BadRequest(new { message = "Email, organization name, and an 8+ character password are required." });
        }

        if (store.UsersByEmail.ContainsKey(email))
        {
            return Results.Conflict(new { message = "A user with this email already exists." });
        }

        var user = new User(Guid.NewGuid(), email, request.FullName.Trim(), PasswordHasher.Hash(request.Password), DateTimeOffset.UtcNow);
        var organization = new Organization(Guid.NewGuid(), request.OrganizationName.Trim(), "free", null, null, DateTimeOffset.UtcNow);
        var membership = new Membership(Guid.NewGuid(), user.Id, organization.Id, Role.Owner, DateTimeOffset.UtcNow);
        var subscription = new Subscription(Guid.NewGuid(), organization.Id, "free", "active", null, null, DateTimeOffset.UtcNow);

        store.Users[user.Id] = user;
        store.UsersByEmail[email] = user.Id;
        store.Organizations[organization.Id] = organization;
        store.Memberships[membership.Id] = membership;
        store.Subscriptions[organization.Id] = subscription;

        return Results.Ok(SessionResponse.From(user, organization, tokens.CreateToken(user, organization, membership.Role), membership.Role));
    }

    private static IResult Login(LoginRequest request, PlatformStore store, TokenService tokens)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (!store.UsersByEmail.TryGetValue(email, out var userId) ||
            !store.Users.TryGetValue(userId, out var user) ||
            !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        var membership = store.Memberships.Values.First(m => m.UserId == user.Id);
        var organization = store.Organizations[membership.OrganizationId];
        return Results.Ok(SessionResponse.From(user, organization, tokens.CreateToken(user, organization, membership.Role), membership.Role));
    }

    private static IResult Me(HttpContext http, PlatformStore store)
    {
        var auth = CurrentUser.From(http, store);
        return auth is null
            ? Results.Unauthorized()
            : Results.Ok(SessionResponse.From(auth.User, auth.Organization, auth.Token, auth.Role));
    }
}
