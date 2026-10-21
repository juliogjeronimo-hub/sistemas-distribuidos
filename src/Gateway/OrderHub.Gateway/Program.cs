using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health/live");

app.MapHealthChecks("/health/ready");

app.MapReverseProxy();

app.Run();

// Exposes the implicit Program type for integration tests (WebApplicationFactory).
public partial class Program;
