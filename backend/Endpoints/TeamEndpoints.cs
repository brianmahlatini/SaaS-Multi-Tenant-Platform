using SaaS.Api.Contracts;
using SaaS.Api.Domain;
using SaaS.Api.Infrastructure.Jobs;
using SaaS.Api.Infrastructure.Messaging;
using SaaS.Api.Persistence;
using SaaS.Api.Persistence.Mongo;
using SaaS.Api.Persistence.Postgres;
using SaaS.Api.Security;

namespace SaaS.Api.Endpoints;

public static class TeamEndpoints
{
    public static IEndpointRouteBuilder MapTeamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Team");

        group.MapGet("/", List).RequireRateLimiting("dashboard");
        group.MapPost("/invite", Invite).RequireRateLimiting("dashboard");

        return app;
    }

    private static IResult List(HttpContext http, PlatformStore store)
    {
        var auth = CurrentUser.From(http, store);
        if (auth is null) return Results.Unauthorized();

        var members = store.Memberships.Values
            .Where(m => m.OrganizationId == auth.Organization.Id)
            .Select(m =>
            {
                var user = store.Users[m.UserId];
                return new TeamMemberDto(user.Id, user.Email, user.FullName, m.Role.ToString(), m.CreatedAt);
            })
            .OrderBy(m => m.Email)
            .ToList();

        return Results.Ok(members);
    }

    private static async Task<IResult> Invite(
        HttpContext http,
        InviteUserRequest request,
        PlatformStore store,
        IBackgroundJobQueue jobs,
        IEventBus events,
        MongoUsageService mongo,
        PostgresProjectionService postgres,
        ILoggerFactory loggerFactory)
    {
        var auth = CurrentUser.From(http, store);
        if (auth is null) return Results.Unauthorized();
        if (!auth.CanManage()) return Results.Forbid();

        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email)) return Results.BadRequest(new { message = "Email is required." });

        var invitation = new Invitation(Guid.NewGuid(), auth.Organization.Id, email, request.Role, "pending", DateTimeOffset.UtcNow);
        store.Invitations[invitation.Id] = invitation;
        var platformEvent = new PlatformEvent("team.invitation.created", auth.Organization.Id, new { invitation.Id, invitation.Email, invitation.Role }, DateTimeOffset.UtcNow);
        await postgres.SaveSnapshotAsync(store, http.RequestAborted);
        await mongo.StoreAuditAsync(platformEvent, http.RequestAborted);
        await events.PublishAsync(platformEvent, http.RequestAborted);
        await jobs.QueueAsync(async cancellationToken =>
        {
            var logger = loggerFactory.CreateLogger("InvitationJobs");
            await Task.Delay(250, cancellationToken);
            logger.LogInformation("Invitation email job completed for {Email} in organization {OrganizationId}", invitation.Email, invitation.OrganizationId);
        });

        return Results.Ok(new InvitationDto(invitation.Id, invitation.Email, invitation.Role.ToString(), invitation.Status, invitation.CreatedAt));
    }
}
