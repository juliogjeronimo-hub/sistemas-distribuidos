using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrderHub.Gateway.Auth;
using OrderHub.Gateway.Middleware;
using OrderHub.Gateway.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// ── Structured JSON logging ───────────────────────────────────────────────────
builder.Logging.AddJsonConsole(options =>
{
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});

// ── Telemetry options ─────────────────────────────────────────────────────────
builder.Services.Configure<TelemetryOptions>(
    builder.Configuration.GetSection(TelemetryOptions.SectionName));

var telemetry = builder.Configuration
    .GetSection(TelemetryOptions.SectionName)
    .Get<TelemetryOptions>() ?? new TelemetryOptions();

var resource = ResourceBuilder.CreateDefault()
    .AddService(serviceName: telemetry.ServiceName, serviceVersion: telemetry.ServiceVersion);

// ── OpenTelemetry ─────────────────────────────────────────────────────────────
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

// ── Health checks ─────────────────────────────────────────────────────────────
// Liveness: process alive.
// Readiness: Keycloak discovery endpoint reachable (Gateway depends on it for JWT validation).
var keycloakDiscoveryUri = new Uri(
    $"{builder.Configuration["Auth:Authority"]}/.well-known/openid-configuration");

builder.Services.AddHealthChecks()
    .AddUrlGroup(
        uri: keycloakDiscoveryUri,
        name: "keycloak",
        tags: ["ready"]);

// ── Authentication — JWT bearer (Req. 2, 3) ───────────────────────────────────
// The Gateway is the first line of defense. Requests without a valid token are
// rejected here and never reach downstream services.
builder.Services.Configure<GatewayAuthOptions>(
    builder.Configuration.GetSection(GatewayAuthOptions.SectionName));

var authOpts = builder.Configuration
    .GetSection(GatewayAuthOptions.SectionName)
    .Get<GatewayAuthOptions>() ?? new GatewayAuthOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authOpts.Authority;
        options.Audience = authOpts.Audience;
        options.RequireHttpsMetadata = authOpts.RequireHttpsMetadata;

        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorization();

// ── YARP reverse proxy ────────────────────────────────────────────────────────
// Routes and clusters are loaded from appsettings ("ReverseProxy" section).
// The 5 s proxy timeout is declared in the cluster config (HttpClient.RequestTimeout).
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
// CorrelationId must run first so every downstream log line is tagged.
app.UseMiddleware<GatewayCorrelationIdMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints ─────────────────────────────────────────────────────────────────
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
}).AllowAnonymous();

app.MapPrometheusScrapingEndpoint().AllowAnonymous();

// All proxied routes require an authenticated caller.
// Individual service policies (scope checks) are enforced at each downstream service.
app.MapReverseProxy().RequireAuthorization();

app.Run();

public partial class Program;
