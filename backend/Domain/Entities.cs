namespace SaaS.Api.Domain;

public enum Role
{
    Owner,
    Admin,
    Member
}

public sealed record User(Guid Id, string Email, string FullName, string PasswordHash, DateTimeOffset CreatedAt);

public sealed record Organization(
    Guid Id,
    string Name,
    string Plan,
    string? StripeCustomerId,
    string? StripeSubscriptionId,
    DateTimeOffset CreatedAt);

public sealed record Membership(Guid Id, Guid UserId, Guid OrganizationId, Role Role, DateTimeOffset CreatedAt);

public sealed record Invitation(Guid Id, Guid OrganizationId, string Email, Role Role, string Status, DateTimeOffset CreatedAt);

public sealed record Subscription(
    Guid Id,
    Guid OrganizationId,
    string Plan,
    string Status,
    string? StripeSubscriptionId,
    DateTimeOffset? CurrentPeriodEnd,
    DateTimeOffset UpdatedAt);

public sealed record ApiKeyRecord(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string Prefix,
    string Hash,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset? RevokedAt);

public sealed record UsageEvent(
    Guid Id,
    Guid OrganizationId,
    Guid ApiKeyId,
    string Path,
    string Method,
    int StatusCode,
    int Units,
    DateTimeOffset OccurredAt);

public sealed record CreatedApiKey(ApiKeyRecord Record, string PlainTextKey);

public sealed record ApiKeyAuth(ApiKeyRecord Key);
