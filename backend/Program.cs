using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"])
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddOpenApi();
builder.Services.AddSingleton<PlatformStore>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<ApiKeyService>();
builder.Services.AddSingleton<BillingService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("frontend");
app.UseHttpsRedirection();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "SaaS.Api", time = DateTimeOffset.UtcNow }));

app.MapPost("/api/auth/register", (
    RegisterRequest request,
    PlatformStore store,
    TokenService tokens) =>
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

    return Results.Ok(SessionResponse.From(user, organization, tokens.CreateToken(user, organization, membership.Role)));
});

app.MapPost("/api/auth/login", (
    LoginRequest request,
    PlatformStore store,
    TokenService tokens) =>
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
    return Results.Ok(SessionResponse.From(user, organization, tokens.CreateToken(user, organization, membership.Role)));
});

app.MapGet("/api/auth/me", (HttpContext http, PlatformStore store) =>
{
    var auth = CurrentUser.From(http, store);
    return auth is null
        ? Results.Unauthorized()
        : Results.Ok(SessionResponse.From(auth.User, auth.Organization, auth.Token));
});

app.MapGet("/api/organizations/list", (HttpContext http, PlatformStore store) =>
{
    var auth = CurrentUser.From(http, store);
    if (auth is null) return Results.Unauthorized();

    var organizations = store.Memberships.Values
        .Where(m => m.UserId == auth.User.Id)
        .Select(m => new OrganizationDto(store.Organizations[m.OrganizationId].Id, store.Organizations[m.OrganizationId].Name, m.Role.ToString(), store.Organizations[m.OrganizationId].Plan))
        .ToList();

    return Results.Ok(organizations);
});

app.MapPost("/api/organizations", (HttpContext http, CreateOrganizationRequest request, PlatformStore store, TokenService tokens) =>
{
    var auth = CurrentUser.From(http, store);
    if (auth is null) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { message = "Organization name is required." });

    var organization = new Organization(Guid.NewGuid(), request.Name.Trim(), "free", null, null, DateTimeOffset.UtcNow);
    var membership = new Membership(Guid.NewGuid(), auth.User.Id, organization.Id, Role.Owner, DateTimeOffset.UtcNow);
    store.Organizations[organization.Id] = organization;
    store.Memberships[membership.Id] = membership;
    store.Subscriptions[organization.Id] = new Subscription(Guid.NewGuid(), organization.Id, "free", "active", null, null, DateTimeOffset.UtcNow);

    return Results.Ok(SessionResponse.From(auth.User, organization, tokens.CreateToken(auth.User, organization, Role.Owner)));
});

app.MapGet("/api/organizations/current", (HttpContext http, PlatformStore store) =>
{
    var auth = CurrentUser.From(http, store);
    return auth is null
        ? Results.Unauthorized()
        : Results.Ok(new OrganizationDto(auth.Organization.Id, auth.Organization.Name, auth.Role.ToString(), auth.Organization.Plan));
});

app.MapGet("/api/users", (HttpContext http, PlatformStore store) =>
{
    var auth = CurrentUser.From(http, store);
    if (auth is null) return Results.Unauthorized();

    var members = store.Memberships.Values
        .Where(m => m.OrganizationId == auth.Organization.Id)
        .Select(m =>
        {
            var user = store.Users[m.UserId];
            return new TeamMemberDto(user.Id, user.Email, user.FullName, m.Role.ToString(), m.CreatedAt);
        })
        .OrderBy(m => m.Email)
        .ToList();

    return Results.Ok(members);
});

app.MapPost("/api/users/invite", (HttpContext http, InviteUserRequest request, PlatformStore store) =>
{
    var auth = CurrentUser.From(http, store);
    if (auth is null) return Results.Unauthorized();
    if (!auth.CanManage()) return Results.Forbid();

    var email = request.Email.Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(email)) return Results.BadRequest(new { message = "Email is required." });

    var invitation = new Invitation(Guid.NewGuid(), auth.Organization.Id, email, request.Role, "pending", DateTimeOffset.UtcNow);
    store.Invitations[invitation.Id] = invitation;
    return Results.Ok(new InvitationDto(invitation.Id, invitation.Email, invitation.Role.ToString(), invitation.Status, invitation.CreatedAt));
});

app.MapGet("/api/billing/subscription", (HttpContext http, PlatformStore store) =>
{
    var auth = CurrentUser.From(http, store);
    if (auth is null) return Results.Unauthorized();
    return Results.Ok(SubscriptionDto.From(store.Subscriptions[auth.Organization.Id], auth.Organization.Plan));
});

app.MapPost("/api/billing/checkout", (HttpContext http, CheckoutRequest request, PlatformStore store, BillingService billing) =>
{
    var auth = CurrentUser.From(http, store);
    if (auth is null) return Results.Unauthorized();
    if (!auth.CanManage()) return Results.Forbid();

    var checkout = billing.CreateCheckoutSession(auth.Organization, request.Plan);
    return checkout is null
        ? Results.BadRequest(new { message = "Unknown plan. Use pro or enterprise." })
        : Results.Ok(checkout);
});

app.MapPost("/api/billing/webhook", async (HttpContext http, PlatformStore store) =>
{
    using var reader = new StreamReader(http.Request.Body);
    var payload = await reader.ReadToEndAsync();

    using var doc = JsonDocument.Parse(payload);
    var root = doc.RootElement;
    var eventId = root.TryGetProperty("id", out var idProperty) ? idProperty.GetString() : Guid.NewGuid().ToString("N");
    var eventType = root.TryGetProperty("type", out var typeProperty) ? typeProperty.GetString() : "unknown";

    if (!store.ProcessedStripeEvents.TryAdd(eventId!, DateTimeOffset.UtcNow))
    {
        return Results.Ok(new { received = true, duplicate = true });
    }

    if (eventType is "checkout.session.completed" or "customer.subscription.updated" or "customer.subscription.created")
    {
        var organizationId = TryReadOrganizationId(root);
        if (organizationId.HasValue && store.Organizations.TryGetValue(organizationId.Value, out var organization))
        {
            var plan = TryReadPlan(root) ?? organization.Plan;
            store.Organizations[organization.Id] = organization with { Plan = plan, StripeCustomerId = TryReadCustomerId(root) ?? organization.StripeCustomerId };
            store.Subscriptions[organization.Id] = store.Subscriptions[organization.Id] with
            {
                Plan = plan,
                Status = "active",
                StripeSubscriptionId = TryReadSubscriptionId(root) ?? store.Subscriptions[organization.Id].StripeSubscriptionId,
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }
    }

    if (eventType is "customer.subscription.deleted")
    {
        var organizationId = TryReadOrganizationId(root);
        if (organizationId.HasValue && store.Organizations.TryGetValue(organizationId.Value, out var organization))
        {
            store.Organizations[organization.Id] = organization with { Plan = "free" };
            store.Subscriptions[organization.Id] = store.Subscriptions[organization.Id] with { Plan = "free", Status = "canceled", UpdatedAt = DateTimeOffset.UtcNow };
        }
    }

    return Results.Ok(new { received = true });
});

app.MapGet("/api/api-keys", (HttpContext http, PlatformStore store) =>
{
    var auth = CurrentUser.From(http, store);
    if (auth is null) return Results.Unauthorized();

    var keys = store.ApiKeys.Values
        .Where(k => k.OrganizationId == auth.Organization.Id)
        .OrderByDescending(k => k.CreatedAt)
        .Select(ApiKeyDto.From)
        .ToList();

    return Results.Ok(keys);
});

app.MapPost("/api/api-keys", (HttpContext http, CreateApiKeyRequest request, PlatformStore store, ApiKeyService keys) =>
{
    var auth = CurrentUser.From(http, store);
    if (auth is null) return Results.Unauthorized();
    if (!auth.CanManage()) return Results.Forbid();

    var created = keys.Create(auth.Organization.Id, request.Name);
    store.ApiKeys[created.Record.Id] = created.Record;
    return Results.Ok(new CreatedApiKeyDto(ApiKeyDto.From(created.Record), created.PlainTextKey));
});

app.MapDelete("/api/api-keys/{id:guid}", (HttpContext http, Guid id, PlatformStore store) =>
{
    var auth = CurrentUser.From(http, store);
    if (auth is null) return Results.Unauthorized();
    if (!auth.CanManage()) return Results.Forbid();
    if (!store.ApiKeys.TryGetValue(id, out var key) || key.OrganizationId != auth.Organization.Id) return Results.NotFound();

    store.ApiKeys[id] = key with { RevokedAt = DateTimeOffset.UtcNow };
    return Results.NoContent();
});

app.MapGet("/api/usage", (HttpContext http, PlatformStore store) =>
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
});

app.MapPost("/api/usage/ingest", (HttpContext http, UsageIngestRequest request, PlatformStore store, ApiKeyService keys) =>
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
});

SeedData.AddDemoTenant(app.Services.GetRequiredService<PlatformStore>());

app.Run();

static Guid? TryReadOrganizationId(JsonElement root)
{
    if (TryReadMetadata(root, "organization_id", out var value) && Guid.TryParse(value, out var id)) return id;
    return null;
}

static string? TryReadPlan(JsonElement root) => TryReadMetadata(root, "plan", out var value) ? value : null;

static string? TryReadCustomerId(JsonElement root)
{
    var dataObject = TryGetDataObject(root);
    return dataObject.HasValue && dataObject.Value.TryGetProperty("customer", out var customer) ? customer.GetString() : null;
}

static string? TryReadSubscriptionId(JsonElement root)
{
    var dataObject = TryGetDataObject(root);
    return dataObject.HasValue && dataObject.Value.TryGetProperty("subscription", out var subscription) ? subscription.GetString() : null;
}

static bool TryReadMetadata(JsonElement root, string key, out string? value)
{
    var dataObject = TryGetDataObject(root);
    value = null;
    if (!dataObject.HasValue ||
        !dataObject.Value.TryGetProperty("metadata", out var metadata) ||
        !metadata.TryGetProperty(key, out var property))
    {
        return false;
    }

    value = property.GetString();
    return !string.IsNullOrWhiteSpace(value);
}

static JsonElement? TryGetDataObject(JsonElement root)
{
    return root.TryGetProperty("data", out var data) &&
           data.TryGetProperty("object", out var dataObject)
        ? dataObject
        : null;
}

public sealed class PlatformStore
{
    public ConcurrentDictionary<Guid, User> Users { get; } = new();
    public ConcurrentDictionary<string, Guid> UsersByEmail { get; } = new();
    public ConcurrentDictionary<Guid, Organization> Organizations { get; } = new();
    public ConcurrentDictionary<Guid, Membership> Memberships { get; } = new();
    public ConcurrentDictionary<Guid, Invitation> Invitations { get; } = new();
    public ConcurrentDictionary<Guid, Subscription> Subscriptions { get; } = new();
    public ConcurrentDictionary<Guid, ApiKeyRecord> ApiKeys { get; } = new();
    public ConcurrentDictionary<Guid, UsageEvent> UsageEvents { get; } = new();
    public ConcurrentDictionary<string, DateTimeOffset> ProcessedStripeEvents { get; } = new();
}

public sealed class TokenService(IConfiguration configuration)
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(configuration["Jwt:SigningKey"] ?? "dev-only-signing-key-change-before-production-32chars");
    private readonly string _issuer = configuration["Jwt:Issuer"] ?? "SaaS.Api";
    private readonly string _audience = configuration["Jwt:Audience"] ?? "SaaS.Frontend";

    public string CreateToken(User user, Organization organization, Role role)
    {
        var handler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("organization_id", organization.Id.ToString()),
                new Claim(ClaimTypes.Role, role.ToString())
            ]),
            Expires = DateTime.UtcNow.AddHours(8),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256)
        };

        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    public ClaimsPrincipal? Validate(string token)
    {
        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(_key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);
        }
        catch
        {
            return null;
        }
    }
}

public sealed class ApiKeyService
{
    public CreatedApiKey Create(Guid organizationId, string name)
    {
        var secret = $"sk_test_{Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant()}";
        var prefix = secret[..16];
        return new CreatedApiKey(
            new ApiKeyRecord(Guid.NewGuid(), organizationId, string.IsNullOrWhiteSpace(name) ? "Default key" : name.Trim(), prefix, Sha256(secret), DateTimeOffset.UtcNow, null, null),
            secret);
    }

    public ApiKeyAuth? Authenticate(HttpContext http, PlatformStore store)
    {
        var header = http.Request.Headers["X-API-Key"].FirstOrDefault()
            ?? http.Request.Headers.Authorization.FirstOrDefault()?.Replace("Api-Key ", "", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(header)) return null;
        var hash = Sha256(header.Trim());
        var record = store.ApiKeys.Values.FirstOrDefault(k => k.Hash == hash && k.RevokedAt is null);
        return record is null ? null : new ApiKeyAuth(record);
    }

    private static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

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

        // Wire Stripe.net here in production. This demo returns a deterministic Checkout-shaped URL.
        var sessionId = $"cs_test_{Guid.NewGuid():N}";
        var url = $"https://checkout.stripe.com/c/pay/{sessionId}#price={Uri.EscapeDataString(priceId)}&success={Uri.EscapeDataString(successUrl)}&cancel={Uri.EscapeDataString(cancelUrl)}";
        return new CheckoutResponse(sessionId, url, normalizedPlan);
    }
}

public sealed record CurrentUser(User User, Organization Organization, Role Role, string Token)
{
    public bool CanManage() => Role is Role.Owner or Role.Admin;

    public static CurrentUser? From(HttpContext http, PlatformStore store)
    {
        var token = http.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(token)) return null;

        var tokenService = http.RequestServices.GetRequiredService<TokenService>();
        var principal = tokenService.Validate(token);
        if (principal is null) return null;

        var userIdValue = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var organizationIdValue = http.Request.Headers["X-Organization-ID"].FirstOrDefault()
            ?? principal.FindFirstValue("organization_id");

        if (!Guid.TryParse(userIdValue, out var userId) ||
            !Guid.TryParse(organizationIdValue, out var organizationId) ||
            !store.Users.TryGetValue(userId, out var user) ||
            !store.Organizations.TryGetValue(organizationId, out var organization))
        {
            return null;
        }

        var membership = store.Memberships.Values.FirstOrDefault(m => m.UserId == userId && m.OrganizationId == organizationId);
        return membership is null ? null : new CurrentUser(user, organization, membership.Role, token);
    }
}

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

public static class SeedData
{
    public static void AddDemoTenant(PlatformStore store)
    {
        if (!store.Users.IsEmpty) return;

        var user = new User(Guid.NewGuid(), "owner@example.com", "Demo Owner", PasswordHasher.Hash("ChangeMe123!"), DateTimeOffset.UtcNow);
        var organization = new Organization(Guid.NewGuid(), "Acme Cloud", "pro", "cus_demo", null, DateTimeOffset.UtcNow);
        var membership = new Membership(Guid.NewGuid(), user.Id, organization.Id, Role.Owner, DateTimeOffset.UtcNow);
        store.Users[user.Id] = user;
        store.UsersByEmail[user.Email] = user.Id;
        store.Organizations[organization.Id] = organization;
        store.Memberships[membership.Id] = membership;
        store.Subscriptions[organization.Id] = new Subscription(Guid.NewGuid(), organization.Id, "pro", "active", "sub_demo", DateTimeOffset.UtcNow.AddDays(18), DateTimeOffset.UtcNow);

        for (var i = 13; i >= 0; i--)
        {
            store.UsageEvents[Guid.NewGuid()] = new UsageEvent(Guid.NewGuid(), organization.Id, Guid.Empty, "/v1/events", "POST", i % 5 == 0 ? 429 : 202, 12 + i, DateTimeOffset.UtcNow.AddDays(-i));
        }
    }
}

public enum Role
{
    Owner,
    Admin,
    Member
}

public sealed record User(Guid Id, string Email, string FullName, string PasswordHash, DateTimeOffset CreatedAt);
public sealed record Organization(Guid Id, string Name, string Plan, string? StripeCustomerId, string? StripeSubscriptionId, DateTimeOffset CreatedAt);
public sealed record Membership(Guid Id, Guid UserId, Guid OrganizationId, Role Role, DateTimeOffset CreatedAt);
public sealed record Invitation(Guid Id, Guid OrganizationId, string Email, Role Role, string Status, DateTimeOffset CreatedAt);
public sealed record Subscription(Guid Id, Guid OrganizationId, string Plan, string Status, string? StripeSubscriptionId, DateTimeOffset? CurrentPeriodEnd, DateTimeOffset UpdatedAt);
public sealed record ApiKeyRecord(Guid Id, Guid OrganizationId, string Name, string Prefix, string Hash, DateTimeOffset CreatedAt, DateTimeOffset? LastUsedAt, DateTimeOffset? RevokedAt);
public sealed record UsageEvent(Guid Id, Guid OrganizationId, Guid ApiKeyId, string Path, string Method, int StatusCode, int Units, DateTimeOffset OccurredAt);
public sealed record CreatedApiKey(ApiKeyRecord Record, string PlainTextKey);
public sealed record ApiKeyAuth(ApiKeyRecord Key);

public sealed record RegisterRequest(string Email, string Password, string FullName, string OrganizationName);
public sealed record LoginRequest(string Email, string Password);
public sealed record CreateOrganizationRequest(string Name);
public sealed record InviteUserRequest(string Email, Role Role);
public sealed record CheckoutRequest(string Plan);
public sealed record CreateApiKeyRequest(string Name);
public sealed record UsageIngestRequest(string Path, string Method, int StatusCode, int Units);

public sealed record SessionResponse(string Token, UserDto User, OrganizationDto Organization)
{
    public static SessionResponse From(User user, Organization organization, string token) =>
        new(token, new UserDto(user.Id, user.Email, user.FullName), new OrganizationDto(organization.Id, organization.Name, "Owner", organization.Plan));
}

public sealed record UserDto(Guid Id, string Email, string FullName);
public sealed record OrganizationDto(Guid Id, string Name, string Role, string Plan);
public sealed record TeamMemberDto(Guid Id, string Email, string FullName, string Role, DateTimeOffset CreatedAt);
public sealed record InvitationDto(Guid Id, string Email, string Role, string Status, DateTimeOffset CreatedAt);
public sealed record SubscriptionDto(string Plan, string Status, string? StripeSubscriptionId, DateTimeOffset? CurrentPeriodEnd)
{
    public static SubscriptionDto From(Subscription subscription, string plan) => new(plan, subscription.Status, subscription.StripeSubscriptionId, subscription.CurrentPeriodEnd);
}
public sealed record CheckoutResponse(string SessionId, string Url, string Plan);
public sealed record ApiKeyDto(Guid Id, string Name, string Prefix, DateTimeOffset CreatedAt, DateTimeOffset? LastUsedAt, DateTimeOffset? RevokedAt)
{
    public static ApiKeyDto From(ApiKeyRecord key) => new(key.Id, key.Name, key.Prefix, key.CreatedAt, key.LastUsedAt, key.RevokedAt);
}
public sealed record CreatedApiKeyDto(ApiKeyDto ApiKey, string PlainTextKey);
public sealed record UsageSummaryDto(int TotalUnits, int TotalRequests, int ErrorCount, List<UsagePointDto> Daily, List<UsageEventDto> RecentEvents);
public sealed record UsagePointDto(string Date, int Units, int Requests);
public sealed record UsageEventDto(Guid Id, string Path, string Method, int StatusCode, int Units, DateTimeOffset OccurredAt)
{
    public static UsageEventDto From(UsageEvent usage) => new(usage.Id, usage.Path, usage.Method, usage.StatusCode, usage.Units, usage.OccurredAt);
}
