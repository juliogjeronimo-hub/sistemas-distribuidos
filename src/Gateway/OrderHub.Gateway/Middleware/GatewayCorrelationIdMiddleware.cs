using System.Diagnostics;

namespace OrderHub.Gateway.Middleware;

/// <summary>
/// Reads or creates a <c>X-Correlation-Id</c> header for every inbound request,
/// adds it to the response and to the structured log scope so that every log line
/// in the Gateway carries the same correlation identifier as the downstream services.
///
/// The YARP transform pipeline propagates the header to upstream services automatically
/// via the "RequestHeader" transform defined in appsettings ("ReverseProxy" section).
/// </summary>
internal sealed class GatewayCorrelationIdMiddleware(RequestDelegate next, ILogger<GatewayCorrelationIdMiddleware> logger)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        // Prefer the incoming header; generate a new one if absent.
        var incoming = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
        var correlationId = string.IsNullOrEmpty(incoming)
            ? Guid.CreateVersion7().ToString()
            : incoming;

        // Ensure the request carries the header so YARP forwards it to the
        // downstream service (YARP copies request headers by default).
        context.Request.Headers[CorrelationIdHeader] = correlationId;

        // Attach to response so the caller can correlate on its side.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId);
            return Task.CompletedTask;
        });

        // Resolve TraceId from the active Activity (set by OpenTelemetry instrumentation).
        var traceId = Activity.Current?.TraceId.ToString() ?? string.Empty;

        // Open a log scope so CorrelationId and TraceId appear in every log record
        // emitted during this request, without threading it through every call-site.
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = traceId,
        }))
        {
            // Make the correlation ID available to YARP request transforms via Items.
            context.Items[CorrelationIdHeader] = correlationId;

            await next(context);
        }
    }
}
