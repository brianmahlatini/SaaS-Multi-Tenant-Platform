using System.Security.Cryptography;
using System.Text;
using SaaS.Api.Domain;
using SaaS.Api.Persistence;

namespace SaaS.Api.Services;

public sealed class ApiKeyService
{
    public CreatedApiKey Create(Guid organizationId, string name)
    {
        var secret = $"sk_test_{Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant()}";
        var prefix = secret[..16];

        return new CreatedApiKey(
            new ApiKeyRecord(Guid.NewGuid(), organizationId, string.IsNullOrWhiteSpace(name) ? "Default key" : name.Trim(), prefix, Sha256(secret), DateTimeOffset.UtcNow, null, null),
            secret);
    }

    public ApiKeyAuth? Authenticate(HttpContext http, PlatformStore store)
    {
        var header = http.Request.Headers["X-API-Key"].FirstOrDefault()
            ?? http.Request.Headers.Authorization.FirstOrDefault()?.Replace("Api-Key ", "", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(header)) return null;

        var hash = Sha256(header.Trim());
        var record = store.ApiKeys.Values.FirstOrDefault(k => k.Hash == hash && k.RevokedAt is null);

        return record is null ? null : new ApiKeyAuth(record);
    }

    private static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
