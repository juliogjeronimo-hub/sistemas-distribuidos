using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrderHub.Gateway.Auth;
using OrderHub.Gateway.Middleware;
using OrderHub.Gateway.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// ── Structured JSON logging (Req. 4) ──────────────────────────────────────────
// Emit JSON to stdout so log aggregators (Loki, CloudWatch, etc.) can parse
// fields without fragile regex. The CorrelationId/TraceId scope is opened by
// CorrelationIdMiddleware and surfaces on every record as JSON scope fields.
builder.Logging.AddJsonConsole(options =>
{
    // UTC timestamps and a single JSON object per line ease ingestion.
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;

    // Promote the CorrelationId/TraceId scope into the JSON output.
    options.IncludeScopes = true;
});

// ── Authentication & Authorization (Req. 2, 3) ───────────────────────────────
// The Gateway rejects unauthenticated requests at the edge; a valid token from
// the orderhub realm is required before any route is proxied downstream.
builder.Services.AddGatewayAuth(builder.Configuration);

// ── Reverse proxy (YARP) (Req. 3) ─────────────────────────────────────────────
// Routes and clusters are bound from the "ReverseProxy" configuration section,
// so container deployments can override destinations via environment variables
// (e.g. ReverseProxy__Clusters__catalog__Destinations__catalog__Address).
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ── Telemetry options (Req. 4, 5) ─────────────────────────────────────────────
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

// ── Health checks (Req. 5) ────────────────────────────────────────────────────
// Liveness has no predicates — it returns 200 as long as the process runs.
// Readiness has no external dependency to probe for the Gateway itself; it
// simply reports the process is ready to accept traffic.
builder.Services.AddHealthChecks();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
// CorrelationId must be registered first so that every log line — including the
// 503 translation below — carries the CorrelationId/TraceId scope.
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints ────────────────────────────────────────────────────────────────

// Live: process is up — no dependency checks.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
}).AllowAnonymous();

// Ready: the Gateway has no external dependency of its own at this stage.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => false,
}).AllowAnonymous();

// Prometheus scrape endpoint — anonymous; the Prometheus server pulls from here.
app.MapPrometheusScrapingEndpoint().AllowAnonymous();

// Reverse proxy pipeline. The middleware registered here runs for every proxied
// request. We translate YARP forwarding failures (destination unreachable,
// timeout) into a clean 503 and log it with the CorrelationId already in scope.
var proxyLogger = app.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("OrderHub.Gateway.ReverseProxy");

app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use((context, next) =>
        ProxyErrorHandler.InvokeAsync(context, next, proxyLogger));
});

app.Run();

// Exposes the implicit Program type for integration tests (WebApplicationFactory).
public partial class Program;
