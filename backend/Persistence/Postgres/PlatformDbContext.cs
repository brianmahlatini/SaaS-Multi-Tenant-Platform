using Microsoft.EntityFrameworkCore;

namespace SaaS.Api.Persistence.Postgres;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<OrganizationEntity> Organizations => Set<OrganizationEntity>();
    public DbSet<MembershipEntity> Memberships => Set<MembershipEntity>();
    public DbSet<InvitationEntity> Invitations => Set<InvitationEntity>();
    public DbSet<SubscriptionEntity> Subscriptions => Set<SubscriptionEntity>();
    public DbSet<ApiKeyEntity> ApiKeys => Set<ApiKeyEntity>();
    public DbSet<ProcessedStripeEventEntity> ProcessedStripeEvents => Set<ProcessedStripeEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.Email).HasMaxLength(320);
            entity.Property(user => user.FullName).HasMaxLength(160);
        });

        modelBuilder.Entity<OrganizationEntity>(entity =>
        {
            entity.HasKey(organization => organization.Id);
            entity.Property(organization => organization.Name).HasMaxLength(160);
            entity.Property(organization => organization.Plan).HasMaxLength(40);
        });

        modelBuilder.Entity<MembershipEntity>(entity =>
        {
            entity.HasKey(membership => membership.Id);
            entity.HasIndex(membership => new { membership.UserId, membership.OrganizationId }).IsUnique();
            entity.Property(membership => membership.Role).HasMaxLength(40);
        });

        modelBuilder.Entity<InvitationEntity>(entity =>
        {
            entity.HasKey(invitation => invitation.Id);
            entity.Property(invitation => invitation.Email).HasMaxLength(320);
            entity.Property(invitation => invitation.Role).HasMaxLength(40);
            entity.Property(invitation => invitation.Status).HasMaxLength(40);
        });

        modelBuilder.Entity<SubscriptionEntity>(entity =>
        {
            entity.HasKey(subscription => subscription.Id);
            entity.HasIndex(subscription => subscription.OrganizationId).IsUnique();
            entity.Property(subscription => subscription.Plan).HasMaxLength(40);
            entity.Property(subscription => subscription.Status).HasMaxLength(40);
        });

        modelBuilder.Entity<ApiKeyEntity>(entity =>
        {
            entity.HasKey(apiKey => apiKey.Id);
            entity.Property(apiKey => apiKey.Name).HasMaxLength(160);
            entity.Property(apiKey => apiKey.Prefix).HasMaxLength(32);
            entity.Property(apiKey => apiKey.Hash).HasMaxLength(128);
        });

        modelBuilder.Entity<ProcessedStripeEventEntity>(entity =>
        {
            entity.HasKey(stripeEvent => stripeEvent.EventId);
            entity.Property(stripeEvent => stripeEvent.EventId).HasMaxLength(160);
        });
    }
}

public sealed class UserEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class OrganizationEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Plan { get; set; } = "free";
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class MembershipEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Role { get; set; } = "Member";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class InvitationEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
    public string Status { get; set; } = "pending";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class SubscriptionEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Plan { get; set; } = "free";
    public string Status { get; set; } = "active";
    public string? StripeSubscriptionId { get; set; }
    public DateTimeOffset? CurrentPeriodEnd { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ApiKeyEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class ProcessedStripeEventEntity
{
    public string EventId { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; }
}
