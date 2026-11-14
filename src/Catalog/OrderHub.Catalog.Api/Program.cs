using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OrderHub.BuildingBlocks.Auth;
using OrderHub.Catalog.Application.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddHealthChecks();
builder.Services.AddOrderHubJwtAuth(builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.MapGet("/v1/products", () => new[]
{
    new { Id = Guid.NewGuid(), Name = "Keyboard", Price = 199.90m },
    new { Id = Guid.NewGuid(), Name = "Mouse", Price = 89.90m }
}).RequireAuthorization();

app.Run();

// Exposes the implicit Program type for integration tests (WebApplicationFactory).
public partial class Program;