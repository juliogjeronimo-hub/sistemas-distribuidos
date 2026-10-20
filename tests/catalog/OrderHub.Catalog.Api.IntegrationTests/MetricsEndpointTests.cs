using System.Net;
using Xunit;

namespace OrderHub.Catalog.Api.IntegrationTests;

/// <summary>
/// Integration tests for the /metrics scrape endpoint.
///
/// Covers:
///   Req. 5 — while a service is running it must expose metrics for Prometheus.
/// </summary>
public sealed class MetricsEndpointTests(CatalogApiFactory factory)
    : IClassFixture<CatalogApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetMetrics_WhenServiceIsRunning_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMetrics_WhenServiceIsRunning_ReturnsPrometheusContentType()
    {
        // Act
        var response = await _client.GetAsync("/metrics");

        // Assert — Prometheus scraper requires text/plain; charset=utf-8 or
        // application/openmetrics-text. Both are acceptable; the exporter
        // negotiates based on the Accept header. Without an explicit Accept we
        // expect the legacy text/plain Prometheus format.
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        Assert.NotNull(mediaType);
        Assert.True(
            mediaType.StartsWith("text/plain", StringComparison.OrdinalIgnoreCase) ||
            mediaType.StartsWith("application/openmetrics-text", StringComparison.OrdinalIgnoreCase),
            $"Unexpected Content-Type: {mediaType}");
    }

    [Fact]
    public async Task GetMetrics_WhenServiceIsRunning_ContainsAspNetCoreMetrics()
    {
        // Arrange — trigger at least one HTTP request so ASP.NET Core metrics
        // have a sample to report. The /v1/ping endpoint is cheap and has no
        // external dependency.
        await _client.GetAsync("/v1/ping");

        // Act
        var response = await _client.GetAsync("/metrics");
        var body = await response.Content.ReadAsStringAsync();

        // Assert — the ASP.NET Core instrumentation must emit the standard
        // http.server.request.duration metric (OTEL semantic conventions).
        Assert.Contains("http_server_request_duration", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMetrics_WhenServiceIsRunning_ContainsDotNetRuntimeMetrics()
    {
        // Act
        var response = await _client.GetAsync("/metrics");
        var body = await response.Content.ReadAsStringAsync();

        // Assert — the runtime instrumentation must emit GC/memory metrics.
        // The exact metric name follows OTEL semantic conventions for .NET runtime.
        Assert.Contains("dotnet_gc", body, StringComparison.OrdinalIgnoreCase);
    }
}
