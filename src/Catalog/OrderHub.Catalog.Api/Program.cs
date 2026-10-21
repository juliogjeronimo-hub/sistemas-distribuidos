using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrderHub.Catalog.Api.Auth;
using OrderHub.Catalog.Api.Middleware;
using OrderHub.Catalog.Api.Telemetry;
using OrderHub.Catalog.Application.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddJsonConsole(options =>
{
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});

builder.Services.AddApplicationServices();

builder.Services.AddCatalogAuth(builder.Configuration);

builder.Services.Configure<TelemetryOptions>(
    builder.Configuration.GetSection(TelemetryOptions.SectionName));

var telemetry = builder.Configuration
    .GetSection(TelemetryOptions.SectionName)
    .Get<TelemetryOptions>() ?? new TelemetryOptions();

var resource = ResourceBuilder.CreateDefault()
    .AddService(serviceName: telemetry.ServiceName, serviceVersion: telemetry.ServiceVersion);

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(resource)
        .AddAspNetCoreInstrumentation(opts =>
        {
            opts.Filter = ctx =>
                !ctx.Request.Path.StartsWithSegments("/health") &&
                !ctx.Request.Path.StartsWithSegments("/metrics");
        })
        .AddHttpClientInstrumentation())
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(resource)
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

var connectionString = builder.Configuration.GetConnectionString("Catalog")
    ?? throw new InvalidOperationException(
        "Connection string 'Catalog' is missing from configuration.");

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);

var app = builder.Build();

// CorrelationIdMiddleware must run before anything that logs so the CorrelationId
// scope is attached to every record within a request.
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/v1/ping", () => new { status = "ok", service = "catalog" })
    .AllowAnonymous();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
}).AllowAnonymous();

app.MapPrometheusScrapingEndpoint().AllowAnonymous();

app.Run();

// Exposes the implicit Program type for integration tests (WebApplicationFactory).
public partial class Program;
