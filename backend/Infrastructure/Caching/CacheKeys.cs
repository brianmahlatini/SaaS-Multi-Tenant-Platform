namespace SaaS.Api.Infrastructure.Caching;

public static class CacheKeys
{
    public static string UsageSummary(Guid organizationId) => $"usage-summary:{organizationId:N}";
}
