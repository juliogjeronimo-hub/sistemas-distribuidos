using System.Diagnostics;

namespace OrderHub.Catalog.Api.Middleware;

/// <summary>
/// Reads the <c>X-Correlation-Id</c> request header and stores the value in
/// <see cref="HttpContext.Items"/> and in the response header so callers can
/// correlate logs across service boundaries.
///
/// If the header is absent a new <see cref="Guid"/> is generated, ensuring
/// every request always has a correlation identifier — even those that arrive
/// without one (e.g. direct calls that bypass the Gateway).
///
/// The middleware opens a log scope so every log record emitted downstream
/// within the same request automatically carries the <c>CorrelationId</c> and
/// <c>TraceId</c> fields without requiring call-site changes.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    /// <summary>Key used to store the correlation ID in <see cref="HttpContext.Items"/>.</summary>
    public const string HttpContextItemKey = "CorrelationId";

    /// <summary>Incoming / outgoing HTTP header name agreed with the Gateway.</summary>
    public const string HeaderName = "X-Correlation-Id";

    // DefineScope returns a Func that, when called, opens a log scope and returns
    // an IDisposable handle. Storing as Func avoids per-call delegate allocation.
    private static readonly Func<ILogger, string, string, IDisposable?> s_requestScope =
        LoggerMessage.DefineScope<string, string>("CorrelationId={CorrelationId} TraceId={TraceId}");

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        // Make the value available to downstream code (handlers, other middleware).
        context.Items[HttpContextItemKey] = correlationId;

        // Echo back so the caller can correlate its own logs with ours.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // TraceId is carried by the current Activity created by ASP.NET / OTEL.
        // Fall back to a dash when no Activity is active (e.g. unit tests).
        var traceId = Activity.Current?.TraceId.ToString() ?? "-";

        // Open the scope; dispose closes it after the request completes.
        // The null-forgiving operator is safe: DefineScope never returns null for
        // the scope factory itself; the IDisposable it produces may be null when
        // no logger provider is registered, which is benign.
        using var scope = s_requestScope(logger, correlationId, traceId);
        await next(context);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

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
