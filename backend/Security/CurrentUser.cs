using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SaaS.Api.Domain;
using SaaS.Api.Persistence;

namespace SaaS.Api.Security;

public sealed record CurrentUser(User User, Organization Organization, Role Role, string Token)
{
    public bool CanManage() => Role is Role.Owner or Role.Admin;

    public static CurrentUser? From(HttpContext http, PlatformStore store)
    {
        var token = http.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(token)) return null;

        var tokenService = http.RequestServices.GetRequiredService<TokenService>();
        var principal = tokenService.Validate(token);
        if (principal is null) return null;

        var userIdValue = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var organizationIdValue = http.Request.Headers["X-Organization-ID"].FirstOrDefault()
            ?? principal.FindFirstValue("organization_id");

        if (!Guid.TryParse(userIdValue, out var userId) ||
            !Guid.TryParse(organizationIdValue, out var organizationId) ||
            !store.Users.TryGetValue(userId, out var user) ||
            !store.Organizations.TryGetValue(organizationId, out var organization))
        {
            return null;
        }

        var membership = store.Memberships.Values.FirstOrDefault(m => m.UserId == userId && m.OrganizationId == organizationId);
        return membership is null ? null : new CurrentUser(user, organization, membership.Role, token);
    }
}
