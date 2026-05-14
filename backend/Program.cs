using SaaS.Api.Endpoints;
using SaaS.Api.Persistence;
using SaaS.Api.Security;
using SaaS.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"])
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddOpenApi();
builder.Services.AddSingleton<PlatformStore>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<ApiKeyService>();
builder.Services.AddSingleton<BillingService>();
builder.Services.AddSingleton<StripeWebhookService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("frontend");
app.UseHttpsRedirection();

app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapOrganizationEndpoints();
app.MapTeamEndpoints();
app.MapBillingEndpoints();
app.MapApiKeyEndpoints();
app.MapUsageEndpoints();

SeedData.AddDemoTenant(app.Services.GetRequiredService<PlatformStore>());

app.Run();
