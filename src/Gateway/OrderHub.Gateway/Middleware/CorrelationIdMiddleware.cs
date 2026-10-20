using System.Diagnostics;

namespace OrderHub.Gateway.Middleware;

/// <summary>
/// Entry point of the <c>X-Correlation-Id</c> flow for the whole platform.
///
/// The Gateway is where a correlation identifier is born: if the incoming
/// request already carries <c>X-Correlation-Id</c> it is reused; otherwise a
/// new <see cref="Guid"/> is generated. The value is written back onto the
/// <em>request</em> headers so that YARP forwards it verbatim to the downstream
/// service, and onto the <em>response</em> headers so the original caller can
/// correlate its own logs.
///
/// A log scope carrying <c>CorrelationId</c> and <c>TraceId</c> is opened for
/// the duration of the request so every log record — including the 503 emitted
/// when a downstream service is unreachable — is automatically tagged (Req. 4).
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    /// <summary>Key used to store the correlation ID in <see cref="HttpContext.Items"/>.</summary>
    public const string HttpContextItemKey = "CorrelationId";

    /// <summary>Incoming / outgoing HTTP header name agreed across all services.</summary>
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

        // Overwrite the request header so YARP propagates a single, canonical
        // value to the upstream service — even when the Gateway generated it.
        context.Request.Headers[HeaderName] = correlationId;

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
