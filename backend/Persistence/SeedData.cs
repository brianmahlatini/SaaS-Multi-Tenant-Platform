using SaaS.Api.Domain;
using SaaS.Api.Security;

namespace SaaS.Api.Persistence;

public static class SeedData
{
    public static void AddDemoTenant(PlatformStore store)
    {
        if (!store.Users.IsEmpty) return;

        var organization = new Organization(Guid.NewGuid(), "Acme Cloud", "pro", "cus_demo", null, DateTimeOffset.UtcNow);
        var users = new[]
        {
            new { User = new User(Guid.NewGuid(), "owner@example.com", "Demo Owner", PasswordHasher.Hash("ChangeMe123!"), DateTimeOffset.UtcNow), Role = Role.Owner },
            new { User = new User(Guid.NewGuid(), "admin@example.com", "Demo Admin", PasswordHasher.Hash("ChangeMe123!"), DateTimeOffset.UtcNow), Role = Role.Admin },
            new { User = new User(Guid.NewGuid(), "member@example.com", "Demo Member", PasswordHasher.Hash("ChangeMe123!"), DateTimeOffset.UtcNow), Role = Role.Member }
        };

        store.Organizations[organization.Id] = organization;
        foreach (var demo in users)
        {
            store.Users[demo.User.Id] = demo.User;
            store.UsersByEmail[demo.User.Email] = demo.User.Id;
            var membership = new Membership(Guid.NewGuid(), demo.User.Id, organization.Id, demo.Role, DateTimeOffset.UtcNow);
            store.Memberships[membership.Id] = membership;
        }

        store.Subscriptions[organization.Id] = new Subscription(Guid.NewGuid(), organization.Id, "pro", "active", "sub_demo", DateTimeOffset.UtcNow.AddDays(18), DateTimeOffset.UtcNow);

        for (var i = 13; i >= 0; i--)
        {
            var id = Guid.NewGuid();
            store.UsageEvents[id] = new UsageEvent(id, organization.Id, Guid.Empty, "/v1/events", "POST", i % 5 == 0 ? 429 : 202, 12 + i, DateTimeOffset.UtcNow.AddDays(-i));
        }
    }
}
