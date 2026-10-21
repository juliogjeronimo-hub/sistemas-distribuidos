using System.Diagnostics;

namespace OrderHub.Catalog.Api.Middleware;

/// <summary>
/// Reads the <c>X-Correlation-Id</c> request header (generating one when absent, so
/// direct calls that bypass the Gateway still get an id), echoes it on the response,
/// and opens a log scope so every record in the request is tagged with it.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HttpContextItemKey = "CorrelationId";

    // Header name agreed with the Gateway.
    public const string HeaderName = "X-Correlation-Id";

    // Caching the scope factory avoids a per-call delegate allocation.
    private static readonly Func<ILogger, string, string, IDisposable?> s_requestScope =
        LoggerMessage.DefineScope<string, string>("CorrelationId={CorrelationId} TraceId={TraceId}");

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items[HttpContextItemKey] = correlationId;

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
