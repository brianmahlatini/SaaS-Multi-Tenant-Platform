using SaaS.Api.Domain;

namespace SaaS.Api.Contracts;

public sealed record SessionResponse(string Token, UserDto User, OrganizationDto Organization)
{
    public static SessionResponse From(User user, Organization organization, string token, Role role = Role.Owner) =>
        new(token, new UserDto(user.Id, user.Email, user.FullName), new OrganizationDto(organization.Id, organization.Name, role.ToString(), organization.Plan));
}

public sealed record UserDto(Guid Id, string Email, string FullName);

public sealed record OrganizationDto(Guid Id, string Name, string Role, string Plan);

public sealed record TeamMemberDto(Guid Id, string Email, string FullName, string Role, DateTimeOffset CreatedAt);

public sealed record InvitationDto(Guid Id, string Email, string Role, string Status, DateTimeOffset CreatedAt);

public sealed record SubscriptionDto(string Plan, string Status, string? StripeSubscriptionId, DateTimeOffset? CurrentPeriodEnd)
{
    public static SubscriptionDto From(Subscription subscription, string plan) =>
        new(plan, subscription.Status, subscription.StripeSubscriptionId, subscription.CurrentPeriodEnd);
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
