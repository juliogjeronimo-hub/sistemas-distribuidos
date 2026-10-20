using System.Net;
using Xunit;

namespace OrderHub.Catalog.Api.IntegrationTests;

/// <summary>
/// Integration tests for the /health/live and /health/ready endpoints.
///
/// Covers:
///   Req. 5 — service must expose /health/live and /health/ready while running.
///   Req. 7 — /health/ready must return failure when an essential dependency is unavailable.
/// </summary>
public sealed class HealthCheckTests(CatalogApiFactory factory)
    : IClassFixture<CatalogApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // ── /health/live ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLive_WhenProcessIsRunning_Returns200()
    {
        // Arrange — no setup needed; liveness is independent of dependencies.

        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── /health/ready ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetReady_WhenPostgresIsUp_Returns200()
    {
        // Arrange — container is already running (started in CatalogApiFactory.InitializeAsync).

        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetReady_WhenPostgresIsDown_Returns503()
    {
        // Arrange
        // Stop the container to simulate a dependency failure.
        // A new client is created against the same factory so the app instance
        // (and its health check registration) is reused — only the DB is gone.
        await factory.StopPostgresAsync();

        var clientWithDownDb = factory.CreateClient();

        try
        {
            // Act
            var response = await clientWithDownDb.GetAsync("/health/ready");

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
        finally
        {
            // Restart the container so subsequent tests in the session are not
            // affected. Tests must not depend on each other's state.
            await factory.RestartPostgresAsync();
        }
    }
}
