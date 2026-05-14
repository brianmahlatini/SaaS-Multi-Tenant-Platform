using System.Collections.Concurrent;
using SaaS.Api.Domain;

namespace SaaS.Api.Persistence;

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
