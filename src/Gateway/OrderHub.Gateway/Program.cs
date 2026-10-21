using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OrderHub.Gateway.Auth;
using OrderHub.Gateway.Middleware;
using OrderHub.Gateway.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddJsonConsole(options =>
{
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
    options.IncludeScopes = true;
});

builder.Services.AddGatewayAuth(builder.Configuration);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

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

builder.Services.AddHealthChecks();

var app = builder.Build();

// CorrelationIdMiddleware must run first so the CorrelationId scope is attached
// to every record, including the 503 translation in the proxy pipeline below.
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => false,
}).AllowAnonymous();

app.MapPrometheusScrapingEndpoint().AllowAnonymous();

var proxyLogger = app.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("OrderHub.Gateway.ReverseProxy");

// Translate YARP forwarding failures (unreachable destination, timeout) into a 503.
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use((context, next) =>
        ProxyErrorHandler.InvokeAsync(context, next, proxyLogger));
});

app.Run();

// Exposes the implicit Program type for integration tests (WebApplicationFactory).
public partial class Program;
