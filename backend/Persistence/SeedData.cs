using SaaS.Api.Domain;
using SaaS.Api.Security;

namespace SaaS.Api.Persistence;

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
            var id = Guid.NewGuid();
            store.UsageEvents[id] = new UsageEvent(id, organization.Id, Guid.Empty, "/v1/events", "POST", i % 5 == 0 ? 429 : 202, 12 + i, DateTimeOffset.UtcNow.AddDays(-i));
        }
    }
}
