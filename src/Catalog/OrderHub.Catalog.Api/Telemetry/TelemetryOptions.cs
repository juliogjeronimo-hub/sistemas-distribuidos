namespace OrderHub.Catalog.Api.Telemetry;

/// <summary>
/// OpenTelemetry resource attributes bound from the <c>Telemetry</c> section.
/// In containers, override via <c>OTEL_SERVICE_NAME</c> / <c>OTEL_SERVICE_VERSION</c>.
/// </summary>
public sealed class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    public string ServiceName { get; init; } = "catalog";

    public string ServiceVersion { get; init; } = "1.0.0";
}
