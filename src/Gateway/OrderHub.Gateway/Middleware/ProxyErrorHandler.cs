using Yarp.ReverseProxy.Forwarder;

namespace OrderHub.Gateway.Middleware;

/// <summary>
/// Translates YARP forwarding failures (destination unreachable, timeout) into a
/// clean <c>503 Service Unavailable</c> response and logs the failure with the
/// request <c>CorrelationId</c> already present in the log scope (Req. 4).
///
/// This runs inside the reverse-proxy pipeline, after the forwarder has
/// attempted the call, so <see cref="IForwarderErrorFeature"/> is populated when
/// the downstream service is down or slow (e.g. the Catalog service crashed).
/// </summary>
internal static partial class ProxyErrorHandler
{
    /// <summary>
    /// Inspects the forwarder error feature and, when present, logs it and sets
    /// a 503 status code (unless the response has already started streaming).
    /// </summary>
    public static async Task InvokeAsync(HttpContext context, Func<Task> next, ILogger logger)
    {
        await next();

        var errorFeature = context.Features.Get<IForwarderErrorFeature>();
        if (errorFeature is null)
        {
            return;
        }

        var correlationId =
            context.Items[CorrelationIdMiddleware.HttpContextItemKey] as string ?? "-";

        LogForwardingFailure(
            logger,
            errorFeature.Error,
            context.Request.Method,
            context.Request.Path,
            correlationId,
            errorFeature.Exception);

        // Only override the status if the response has not started yet.
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }
    }

    // Source-generated, high-performance structured log (satisfies CA1848).
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Proxy forwarding failed with {ForwarderError} for {Method} {Path}. CorrelationId={CorrelationId}")]
    private static partial void LogForwardingFailure(
        ILogger logger,
        ForwarderError forwarderError,
        string method,
        string path,
        string correlationId,
        Exception? exception);
}
