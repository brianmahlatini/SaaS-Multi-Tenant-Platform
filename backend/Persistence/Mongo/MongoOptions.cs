namespace SaaS.Api.Persistence.Mongo;

public sealed class MongoOptions
{
    public string ConnectionString { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = "saas_platform";
}
