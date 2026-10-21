using System.Diagnostics;

namespace OrderHub.Gateway.Middleware;

/// <summary>
/// Origin of the <c>X-Correlation-Id</c> flow: reuses an incoming id or generates
/// one, writes it onto the request headers (so YARP forwards it downstream) and the
/// response headers, and opens a log scope so every record in the request is tagged.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HttpContextItemKey = "CorrelationId";

    // Header name agreed across all services.
    public const string HeaderName = "X-Correlation-Id";

    // Caching the scope factory avoids a per-call delegate allocation.
    private static readonly Func<ILogger, string, string, IDisposable?> s_requestScope =
        LoggerMessage.DefineScope<string, string>("CorrelationId={CorrelationId} TraceId={TraceId}");

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items[HttpContextItemKey] = correlationId;

        // Overwrite the request header so YARP forwards a single canonical value
        // upstream even when the Gateway generated it.
        context.Request.Headers[HeaderName] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        var traceId = Activity.Current?.TraceId.ToString() ?? "-";

        using var scope = s_requestScope(logger, correlationId, traceId);
        await next(context);
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var values)
            && values.FirstOrDefault() is { Length: > 0 } existing)
        {
            return existing;
        }

        return Guid.CreateVersion7().ToString();
    }
}
