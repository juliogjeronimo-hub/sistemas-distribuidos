using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OrderHub.BuildingBlocks.Auth;
using OrderHub.Catalog.Api.Endpoints;
using OrderHub.Catalog.Api.Middleware;
using OrderHub.Catalog.Application.DependencyInjection;
using OrderHub.Catalog.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCatalogHealthChecks();
builder.Services.AddOrderHubJwtAuth(builder.Configuration);
builder.Services.AddCatalogAuth(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.ApplyMigrations(app.Environment);

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.MapProductEndpoints();

app.Run();

// Exposes the implicit Program type for integration tests (WebApplicationFactory).
public partial class Program;