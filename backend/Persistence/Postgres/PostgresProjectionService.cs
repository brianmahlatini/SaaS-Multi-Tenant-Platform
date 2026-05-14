using Microsoft.EntityFrameworkCore;
using SaaS.Api.Domain;

namespace SaaS.Api.Persistence.Postgres;

public sealed class PostgresProjectionService(IServiceScopeFactory scopeFactory, ILogger<PostgresProjectionService> logger)
{
    public async Task TryInitializeAsync(PlatformStore store, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetService<PlatformDbContext>();
        if (db is null) return;

        try
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
            if (!await db.Users.AnyAsync(cancellationToken))
            {
                await SaveSnapshotAsync(store, cancellationToken);
                return;
            }

            await LoadSnapshotAsync(store, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PostgreSQL initialization failed. Continuing with in-memory store.");
        }
    }

    public async Task SaveSnapshotAsync(PlatformStore store, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetService<PlatformDbContext>();
        if (db is null) return;

        await UpsertUsers(db, store, cancellationToken);
        await UpsertOrganizations(db, store, cancellationToken);
        await UpsertMemberships(db, store, cancellationToken);
        await UpsertInvitations(db, store, cancellationToken);
        await UpsertSubscriptions(db, store, cancellationToken);
        await UpsertApiKeys(db, store, cancellationToken);
        await UpsertStripeEvents(db, store, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task LoadSnapshotAsync(PlatformStore store, PlatformDbContext db, CancellationToken cancellationToken)
    {
        store.Users.Clear();
        store.UsersByEmail.Clear();
        store.Organizations.Clear();
        store.Memberships.Clear();
        store.Invitations.Clear();
        store.Subscriptions.Clear();
        store.ApiKeys.Clear();
        store.ProcessedStripeEvents.Clear();

        foreach (var user in await db.Users.AsNoTracking().ToListAsync(cancellationToken))
        {
            store.Users[user.Id] = new User(user.Id, user.Email, user.FullName, user.PasswordHash, user.CreatedAt);
            store.UsersByEmail[user.Email] = user.Id;
        }

        foreach (var organization in await db.Organizations.AsNoTracking().ToListAsync(cancellationToken))
        {
            store.Organizations[organization.Id] = new Organization(organization.Id, organization.Name, organization.Plan, organization.StripeCustomerId, organization.StripeSubscriptionId, organization.CreatedAt);
        }

        foreach (var membership in await db.Memberships.AsNoTracking().ToListAsync(cancellationToken))
        {
            store.Memberships[membership.Id] = new Membership(membership.Id, membership.UserId, membership.OrganizationId, Enum.Parse<Role>(membership.Role), membership.CreatedAt);
        }

        foreach (var invitation in await db.Invitations.AsNoTracking().ToListAsync(cancellationToken))
        {
            store.Invitations[invitation.Id] = new Invitation(invitation.Id, invitation.OrganizationId, invitation.Email, Enum.Parse<Role>(invitation.Role), invitation.Status, invitation.CreatedAt);
        }

        foreach (var subscription in await db.Subscriptions.AsNoTracking().ToListAsync(cancellationToken))
        {
            store.Subscriptions[subscription.OrganizationId] = new Subscription(subscription.Id, subscription.OrganizationId, subscription.Plan, subscription.Status, subscription.StripeSubscriptionId, subscription.CurrentPeriodEnd, subscription.UpdatedAt);
        }

        foreach (var apiKey in await db.ApiKeys.AsNoTracking().ToListAsync(cancellationToken))
        {
            store.ApiKeys[apiKey.Id] = new ApiKeyRecord(apiKey.Id, apiKey.OrganizationId, apiKey.Name, apiKey.Prefix, apiKey.Hash, apiKey.CreatedAt, apiKey.LastUsedAt, apiKey.RevokedAt);
        }

        foreach (var stripeEvent in await db.ProcessedStripeEvents.AsNoTracking().ToListAsync(cancellationToken))
        {
            store.ProcessedStripeEvents[stripeEvent.EventId] = stripeEvent.ProcessedAt;
        }
    }

    private async Task LoadSnapshotAsync(PlatformStore store, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        await LoadSnapshotAsync(store, db, cancellationToken);
    }

    private static async Task UpsertUsers(PlatformDbContext db, PlatformStore store, CancellationToken cancellationToken)
    {
        foreach (var user in store.Users.Values)
        {
            var entity = await db.Users.FindAsync([user.Id], cancellationToken);
            if (entity is null)
            {
                db.Users.Add(new UserEntity { Id = user.Id, Email = user.Email, FullName = user.FullName, PasswordHash = user.PasswordHash, CreatedAt = user.CreatedAt });
            }
            else
            {
                entity.Email = user.Email;
                entity.FullName = user.FullName;
                entity.PasswordHash = user.PasswordHash;
            }
        }
    }

    private static async Task UpsertOrganizations(PlatformDbContext db, PlatformStore store, CancellationToken cancellationToken)
    {
        foreach (var organization in store.Organizations.Values)
        {
            var entity = await db.Organizations.FindAsync([organization.Id], cancellationToken);
            if (entity is null)
            {
                db.Organizations.Add(new OrganizationEntity { Id = organization.Id, Name = organization.Name, Plan = organization.Plan, StripeCustomerId = organization.StripeCustomerId, StripeSubscriptionId = organization.StripeSubscriptionId, CreatedAt = organization.CreatedAt });
            }
            else
            {
                entity.Name = organization.Name;
                entity.Plan = organization.Plan;
                entity.StripeCustomerId = organization.StripeCustomerId;
                entity.StripeSubscriptionId = organization.StripeSubscriptionId;
            }
        }
    }

    private static async Task UpsertMemberships(PlatformDbContext db, PlatformStore store, CancellationToken cancellationToken)
    {
        foreach (var membership in store.Memberships.Values)
        {
            var entity = await db.Memberships.FindAsync([membership.Id], cancellationToken);
            if (entity is null)
            {
                db.Memberships.Add(new MembershipEntity { Id = membership.Id, UserId = membership.UserId, OrganizationId = membership.OrganizationId, Role = membership.Role.ToString(), CreatedAt = membership.CreatedAt });
            }
            else
            {
                entity.Role = membership.Role.ToString();
            }
        }
    }

    private static async Task UpsertInvitations(PlatformDbContext db, PlatformStore store, CancellationToken cancellationToken)
    {
        foreach (var invitation in store.Invitations.Values)
        {
            var entity = await db.Invitations.FindAsync([invitation.Id], cancellationToken);
            if (entity is null)
            {
                db.Invitations.Add(new InvitationEntity { Id = invitation.Id, OrganizationId = invitation.OrganizationId, Email = invitation.Email, Role = invitation.Role.ToString(), Status = invitation.Status, CreatedAt = invitation.CreatedAt });
            }
        }
    }

    private static async Task UpsertSubscriptions(PlatformDbContext db, PlatformStore store, CancellationToken cancellationToken)
    {
        foreach (var subscription in store.Subscriptions.Values)
        {
            var entity = await db.Subscriptions.FindAsync([subscription.Id], cancellationToken);
            if (entity is null)
            {
                db.Subscriptions.Add(new SubscriptionEntity { Id = subscription.Id, OrganizationId = subscription.OrganizationId, Plan = subscription.Plan, Status = subscription.Status, StripeSubscriptionId = subscription.StripeSubscriptionId, CurrentPeriodEnd = subscription.CurrentPeriodEnd, UpdatedAt = subscription.UpdatedAt });
            }
            else
            {
                entity.Plan = subscription.Plan;
                entity.Status = subscription.Status;
                entity.StripeSubscriptionId = subscription.StripeSubscriptionId;
                entity.CurrentPeriodEnd = subscription.CurrentPeriodEnd;
                entity.UpdatedAt = subscription.UpdatedAt;
            }
        }
    }

    private static async Task UpsertApiKeys(PlatformDbContext db, PlatformStore store, CancellationToken cancellationToken)
    {
        foreach (var apiKey in store.ApiKeys.Values)
        {
            var entity = await db.ApiKeys.FindAsync([apiKey.Id], cancellationToken);
            if (entity is null)
            {
                db.ApiKeys.Add(new ApiKeyEntity { Id = apiKey.Id, OrganizationId = apiKey.OrganizationId, Name = apiKey.Name, Prefix = apiKey.Prefix, Hash = apiKey.Hash, CreatedAt = apiKey.CreatedAt, LastUsedAt = apiKey.LastUsedAt, RevokedAt = apiKey.RevokedAt });
            }
            else
            {
                entity.Name = apiKey.Name;
                entity.LastUsedAt = apiKey.LastUsedAt;
                entity.RevokedAt = apiKey.RevokedAt;
            }
        }
    }

    private static async Task UpsertStripeEvents(PlatformDbContext db, PlatformStore store, CancellationToken cancellationToken)
    {
        foreach (var stripeEvent in store.ProcessedStripeEvents)
        {
            if (await db.ProcessedStripeEvents.FindAsync([stripeEvent.Key], cancellationToken) is null)
            {
                db.ProcessedStripeEvents.Add(new ProcessedStripeEventEntity { EventId = stripeEvent.Key, ProcessedAt = stripeEvent.Value });
            }
        }
    }
}
