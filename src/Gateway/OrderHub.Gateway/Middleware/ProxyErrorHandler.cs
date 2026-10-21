using Yarp.ReverseProxy.Forwarder;

namespace OrderHub.Gateway.Middleware;

/// <summary>
/// Translates YARP forwarding failures into a 503 response. It runs after the
/// forwarder has attempted the call, so <see cref="IForwarderErrorFeature"/> is
/// populated when the downstream service is unreachable or slow.
/// </summary>
internal static partial class ProxyErrorHandler
{
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

        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }
    }

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
