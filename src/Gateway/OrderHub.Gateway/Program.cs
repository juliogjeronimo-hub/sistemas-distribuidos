using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OrderHub.BuildingBlocks.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();

builder.Services.AddOrderHubJwtAuth(builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapReverseProxy().RequireAuthorization();

app.Run();

// Exposes the implicit Program type for integration tests (WebApplicationFactory).
public partial class Program;