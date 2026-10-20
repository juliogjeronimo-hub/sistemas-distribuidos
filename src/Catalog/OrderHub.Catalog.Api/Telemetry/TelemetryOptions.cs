namespace OrderHub.Catalog.Api.Telemetry;

/// <summary>
/// Typed options for OpenTelemetry resource attributes.
/// Bound from the <c>Telemetry</c> section of <c>appsettings.json</c>.
/// Override with <c>OTEL_SERVICE_NAME</c> / <c>OTEL_SERVICE_VERSION</c>
/// environment variables in container deployments.
/// </summary>
public sealed class TelemetryOptions
{
    /// <summary>Configuration section key.</summary>
    public const string SectionName = "Telemetry";

    /// <summary>Logical service name reported in traces and metrics.</summary>
    public string ServiceName { get; init; } = "catalog";

    /// <summary>Service version reported in traces and metrics.</summary>
    public string ServiceVersion { get; init; } = "1.0.0";
}
