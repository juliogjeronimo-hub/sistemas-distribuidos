using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrderHub.Catalog.Api.Auth;
using OrderHub.Catalog.Api.Middleware;
using OrderHub.Catalog.Api.Telemetry;
using OrderHub.Catalog.Application.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// ── Structured JSON logging (Req. 4) ──────────────────────────────────────────
// Emit JSON to stdout so log aggregators (Loki, CloudWatch, etc.) can parse
// fields without fragile regex. The console formatter is sufficient here;
// Serilog or OpenTelemetry log exporter can be added without
// changing the middleware or the call-sites.
builder.Logging.AddJsonConsole(options =>
{
    // UTC timestamps and a single JSON object per line ease ingestion.
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});

// ── Application layer (handlers, validators, decorators) ─────────────────────
builder.Services.AddApplicationServices();

// ── Authentication & Authorization (Req. 2, 3) ───────────────────────────────
// JWT bearer validation against Keycloak; scope-based policies.
// Defense-in-depth: Gateway also validates, but each service is independently secure.
builder.Services.AddCatalogAuth(builder.Configuration);

// ── Telemetry options (Req. 5) ────────────────────────────────────────────────
// Read service identity from config so containers can override via
// OTEL_SERVICE_NAME / OTEL_SERVICE_VERSION environment variables.
builder.Services.Configure<TelemetryOptions>(
    builder.Configuration.GetSection(TelemetryOptions.SectionName));

var telemetry = builder.Configuration
    .GetSection(TelemetryOptions.SectionName)
    .Get<TelemetryOptions>() ?? new TelemetryOptions();

// Resource attributes are shared by both traces and metrics so we build once.
var resource = ResourceBuilder.CreateDefault()
    .AddService(serviceName: telemetry.ServiceName, serviceVersion: telemetry.ServiceVersion);

// ── OpenTelemetry (Req. 5) ────────────────────────────────────────────────────
builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(resource)
        .AddAspNetCoreInstrumentation(opts =>
        {
            // Exclude diagnostic-only endpoints from traces to avoid noise.
            // The Activity itself is still created by the .NET runtime; we just
            // prevent it from being exported, keeping the backend lean.
            opts.Filter = ctx =>
                !ctx.Request.Path.StartsWithSegments("/health") &&
                !ctx.Request.Path.StartsWithSegments("/metrics");
        })
        .AddHttpClientInstrumentation())
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(resource)
        .AddAspNetCoreInstrumentation()   // request count, duration, active requests
        .AddRuntimeInstrumentation()      // GC, thread-pool, exception counts
        .AddPrometheusExporter());        // exposes /metrics for Prometheus scraping

// ── Health checks (Req. 5, 7) ────────────────────────────────────────────────
// The liveness probe has no predicates — it returns 200 as long as the process
// is running, independently of any external dependency.
// The readiness probe checks PostgreSQL connectivity so the orchestrator (or
// Compose healthcheck) knows when the service is ready to receive traffic.
var connectionString = builder.Configuration.GetConnectionString("Catalog")
    ?? throw new InvalidOperationException(
        "Connection string 'Catalog' is missing from configuration.");

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
// CorrelationId must be registered before any middleware that emits log records
// so that every log line in a request carries the CorrelationId scope.
// TraceId is resolved from Activity.Current, which is populated by the
// OpenTelemetry ASP.NET Core instrumentation registered above.
app.UseMiddleware<CorrelationIdMiddleware>();

// Authentication/Authorization middleware must run after routing so that
// endpoint metadata (AllowAnonymous, RequireAuthorization) is available.
app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints ────────────────────────────────────────────────────────────────

// Minimal probe endpoint — useful for smoke tests and manual checks.
app.MapGet("/v1/ping", () => new { status = "ok", service = "catalog" })
    .AllowAnonymous();

// Live: process is up — no dependency checks.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
}).AllowAnonymous();

// Ready: all checks tagged "ready" must pass (PostgreSQL).
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
}).AllowAnonymous();

// Prometheus scrape endpoint — anônimo; the Prometheus server pulls from here.
// Uses the /metrics path by convention (Prometheus default).
app.MapPrometheusScrapingEndpoint().AllowAnonymous();

app.Run();

// Exposes the implicit Program type for integration tests (WebApplicationFactory).
public partial class Program;
