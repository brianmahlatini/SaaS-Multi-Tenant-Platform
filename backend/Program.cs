using SaaS.Api.Endpoints;
using SaaS.Api.Infrastructure.Jobs;
using SaaS.Api.Infrastructure.Messaging;
using SaaS.Api.Infrastructure.Monitoring;
using SaaS.Api.Persistence;
using SaaS.Api.Persistence.Mongo;
using SaaS.Api.Persistence.Postgres;
using SaaS.Api.Realtime;
using SaaS.Api.Security;
using SaaS.Api.Services;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("dashboard", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 20,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }));

    options.AddPolicy("ingest", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Request.Headers["X-API-Key"].FirstOrDefault() ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 10,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }));
});

var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection("MongoDB"));
var postgresConnection = builder.Configuration.GetConnectionString("Postgres");
if (!string.IsNullOrWhiteSpace(postgresConnection))
{
    builder.Services.AddDbContext<PlatformDbContext>(options => options.UseNpgsql(postgresConnection));
}

builder.Services.AddSingleton<PlatformStore>();
builder.Services.AddSingleton<PostgresProjectionService>();
builder.Services.AddSingleton<MongoUsageService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<ApiKeyService>();
builder.Services.AddSingleton<BillingService>();
builder.Services.AddSingleton<StripeWebhookService>();
builder.Services.AddSingleton<AppMetrics>();
builder.Services.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();
builder.Services.AddHostedService<QueuedHostedService>();
builder.Services.AddHostedService<RabbitMqConsumerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("frontend");
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapOrganizationEndpoints();
app.MapTeamEndpoints();
app.MapBillingEndpoints();
app.MapApiKeyEndpoints();
app.MapUsageEndpoints();
app.MapHub<RealtimeHub>("/hubs/realtime");

SeedData.AddDemoTenant(app.Services.GetRequiredService<PlatformStore>());
await app.Services.GetRequiredService<PostgresProjectionService>().TryInitializeAsync(app.Services.GetRequiredService<PlatformStore>());

app.Run();
