using SaaS.Api.Domain;

namespace SaaS.Api.Contracts;

public sealed record RegisterRequest(string Email, string Password, string FullName, string OrganizationName);

public sealed record LoginRequest(string Email, string Password);

public sealed record CreateOrganizationRequest(string Name);

public sealed record InviteUserRequest(string Email, Role Role);

public sealed record CheckoutRequest(string Plan);

public sealed record CreateApiKeyRequest(string Name);

public sealed record UsageIngestRequest(string Path, string Method, int StatusCode, int Units);
