namespace OrderHub.Gateway.Telemetry;

/// <summary>Typed options for OpenTelemetry service identity in the Gateway.</summary>
public sealed class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    public string ServiceName { get; init; } = "gateway";
    public string ServiceVersion { get; init; } = "1.0.0";
}
