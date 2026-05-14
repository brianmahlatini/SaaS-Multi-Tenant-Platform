using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SaaS.Api.Persistence.Mongo;

public sealed class UsageDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    public Guid UsageEventId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ApiKeyId { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public int Units { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

public sealed class AuditDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string EventType { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public object Payload { get; set; } = new();
    public DateTimeOffset OccurredAt { get; set; }
}
