using Microsoft.AspNetCore.SignalR;

namespace SaaS.Api.Realtime;

public sealed class RealtimeHub : Hub
{
    public Task JoinOrganization(string organizationId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, OrganizationGroup(organizationId));

    public Task LeaveOrganization(string organizationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, OrganizationGroup(organizationId));

    public static string OrganizationGroup(Guid organizationId) => $"organization:{organizationId:N}";

    private static string OrganizationGroup(string organizationId) => Guid.TryParse(organizationId, out var id)
        ? OrganizationGroup(id)
        : $"organization:{organizationId}";
}
