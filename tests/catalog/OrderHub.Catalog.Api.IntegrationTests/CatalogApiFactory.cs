using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace OrderHub.Catalog.Api.IntegrationTests;

/// <summary>
/// Spins up a real PostgreSQL container before the test session and tears it down
/// after. The container's connection string overrides whatever is in
/// appsettings.json so the API always connects to the ephemeral test database.
/// </summary>
public sealed class CatalogApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("catalog_test")
        .WithUsername("catalog")
        .WithPassword("catalog_test_pw")
        .Build();

    /// <summary>
    /// Returns the connection string for the running container.
    /// Exposed so tests that stop the container can rebuild a client afterwards.
    /// </summary>
    public string ConnectionString => _postgres.GetConnectionString();

    // ── IAsyncLifetime ────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    // ── Helpers for failure-injection tests ───────────────────────────────────

    /// <summary>
    /// Stops the PostgreSQL container to simulate a dependency failure.
    /// Used by tests that verify /health/ready returns 503 when the DB is down.
    /// </summary>
    public Task StopPostgresAsync() => _postgres.StopAsync();

    /// <summary>
    /// Restarts the PostgreSQL container after a failure-injection test so
    /// subsequent tests in the same session start from a clean state.
    /// </summary>
    public Task RestartPostgresAsync() => _postgres.StartAsync();

    // ── WebApplicationFactory ─────────────────────────────────────────────────

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override the connection string so the API uses the Testcontainers DB.
        // This runs after the app's own appsettings.json, ensuring the test
        // value wins without touching production configuration files.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Catalog"] = _postgres.GetConnectionString(),
            });
        });

        // Use the Test environment so the app doesn't try to talk to any
        // real infrastructure that isn't present in CI.
        builder.UseEnvironment("Test");
    }
}
