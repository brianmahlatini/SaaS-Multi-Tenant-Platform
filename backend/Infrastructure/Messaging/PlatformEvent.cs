namespace SaaS.Api.Infrastructure.Messaging;

public sealed record PlatformEvent(
    string Type,
    Guid OrganizationId,
    object Payload,
    DateTimeOffset OccurredAt);
