namespace SaaS.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", () => Results.Ok(new
        {
            status = "ok",
            service = "SaaS.Api",
            time = DateTimeOffset.UtcNow
        }));

        return app;
    }
}
