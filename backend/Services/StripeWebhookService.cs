using System.Text.Json;
using SaaS.Api.Persistence;

namespace SaaS.Api.Services;

public sealed class StripeWebhookService
{
    public WebhookResult Apply(JsonElement root, PlatformStore store)
    {
        var eventId = root.TryGetProperty("id", out var idProperty) ? idProperty.GetString() : Guid.NewGuid().ToString("N");
        var eventType = root.TryGetProperty("type", out var typeProperty) ? typeProperty.GetString() : "unknown";

        if (!store.ProcessedStripeEvents.TryAdd(eventId!, DateTimeOffset.UtcNow))
        {
            return WebhookResult.Duplicate;
        }

        if (eventType is "checkout.session.completed" or "customer.subscription.updated" or "customer.subscription.created")
        {
            ActivateSubscription(root, store);
        }

        if (eventType is "customer.subscription.deleted")
        {
            CancelSubscription(root, store);
        }

        return WebhookResult.Processed;
    }

    private static void ActivateSubscription(JsonElement root, PlatformStore store)
    {
        var organizationId = TryReadOrganizationId(root);
        if (!organizationId.HasValue || !store.Organizations.TryGetValue(organizationId.Value, out var organization)) return;

        var plan = TryReadPlan(root) ?? organization.Plan;
        store.Organizations[organization.Id] = organization with
        {
            Plan = plan,
            StripeCustomerId = TryReadCustomerId(root) ?? organization.StripeCustomerId
        };

        store.Subscriptions[organization.Id] = store.Subscriptions[organization.Id] with
        {
            Plan = plan,
            Status = "active",
            StripeSubscriptionId = TryReadSubscriptionId(root) ?? store.Subscriptions[organization.Id].StripeSubscriptionId,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static void CancelSubscription(JsonElement root, PlatformStore store)
    {
        var organizationId = TryReadOrganizationId(root);
        if (!organizationId.HasValue || !store.Organizations.TryGetValue(organizationId.Value, out var organization)) return;

        store.Organizations[organization.Id] = organization with { Plan = "free" };
        store.Subscriptions[organization.Id] = store.Subscriptions[organization.Id] with
        {
            Plan = "free",
            Status = "canceled",
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static Guid? TryReadOrganizationId(JsonElement root)
    {
        if (TryReadMetadata(root, "organization_id", out var value) && Guid.TryParse(value, out var id)) return id;
        return null;
    }

    private static string? TryReadPlan(JsonElement root) => TryReadMetadata(root, "plan", out var value) ? value : null;

    private static string? TryReadCustomerId(JsonElement root)
    {
        var dataObject = TryGetDataObject(root);
        return dataObject.HasValue && dataObject.Value.TryGetProperty("customer", out var customer) ? customer.GetString() : null;
    }

    private static string? TryReadSubscriptionId(JsonElement root)
    {
        var dataObject = TryGetDataObject(root);
        return dataObject.HasValue && dataObject.Value.TryGetProperty("subscription", out var subscription) ? subscription.GetString() : null;
    }

    private static bool TryReadMetadata(JsonElement root, string key, out string? value)
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

    private static JsonElement? TryGetDataObject(JsonElement root)
    {
        return root.TryGetProperty("data", out var data) &&
               data.TryGetProperty("object", out var dataObject)
            ? dataObject
            : null;
    }
}

public enum WebhookResult
{
    Processed,
    Duplicate
}
