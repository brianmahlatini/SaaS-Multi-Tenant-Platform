using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SaaS.Api.Domain;
using SaaS.Api.Infrastructure.Messaging;

namespace SaaS.Api.Persistence.Mongo;

public sealed class MongoUsageService(IOptions<MongoOptions> options, ILogger<MongoUsageService> logger)
{
    private readonly MongoOptions _options = options.Value;
    private IMongoCollection<UsageDocument>? _usageEvents;
    private IMongoCollection<AuditDocument>? _auditEvents;

    public async Task StoreUsageAsync(UsageEvent usage, CancellationToken cancellationToken = default)
    {
        var collection = GetUsageCollection();
        if (collection is null) return;

        try
        {
            await collection.InsertOneAsync(new UsageDocument
            {
                UsageEventId = usage.Id,
                OrganizationId = usage.OrganizationId,
                ApiKeyId = usage.ApiKeyId,
                Path = usage.Path,
                Method = usage.Method,
                StatusCode = usage.StatusCode,
                Units = usage.Units,
                OccurredAt = usage.OccurredAt
            }, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MongoDB usage insert failed");
        }
    }

    public async Task StoreAuditAsync(PlatformEvent platformEvent, CancellationToken cancellationToken = default)
    {
        var collection = GetAuditCollection();
        if (collection is null) return;

        try
        {
            await collection.InsertOneAsync(new AuditDocument
            {
                EventType = platformEvent.Type,
                OrganizationId = platformEvent.OrganizationId,
                Payload = platformEvent.Payload,
                OccurredAt = platformEvent.OccurredAt
            }, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MongoDB audit insert failed");
        }
    }

    private IMongoCollection<UsageDocument>? GetUsageCollection()
    {
        if (_usageEvents is not null) return _usageEvents;
        var database = GetDatabase();
        _usageEvents = database?.GetCollection<UsageDocument>("usage_events");
        return _usageEvents;
    }

    private IMongoCollection<AuditDocument>? GetAuditCollection()
    {
        if (_auditEvents is not null) return _auditEvents;
        var database = GetDatabase();
        _auditEvents = database?.GetCollection<AuditDocument>("audit_events");
        return _auditEvents;
    }

    private IMongoDatabase? GetDatabase()
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString)) return null;
        return new MongoClient(_options.ConnectionString).GetDatabase(_options.DatabaseName);
    }
}
