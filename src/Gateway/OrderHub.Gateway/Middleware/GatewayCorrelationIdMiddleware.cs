using System.Diagnostics;

namespace OrderHub.Gateway.Middleware;

/// <summary>
/// Reuses or creates an <c>X-Correlation-Id</c> header for every inbound request and
/// opens a log scope so every record in the request shares the same correlation id.
/// The header is written back onto the request so YARP forwards it downstream.
/// </summary>
internal sealed class GatewayCorrelationIdMiddleware(RequestDelegate next, ILogger<GatewayCorrelationIdMiddleware> logger)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
        var correlationId = string.IsNullOrEmpty(incoming)
            ? Guid.CreateVersion7().ToString()
            : incoming;

        context.Request.Headers[CorrelationIdHeader] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId);
            return Task.CompletedTask;
        });

        var traceId = Activity.Current?.TraceId.ToString() ?? string.Empty;

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = traceId,
        }))
        {
            context.Items[CorrelationIdHeader] = correlationId;

            await next(context);
        }
    }
}
