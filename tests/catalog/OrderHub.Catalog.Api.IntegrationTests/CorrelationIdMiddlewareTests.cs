using System.Net;
using Xunit;

namespace OrderHub.Catalog.Api.IntegrationTests;

/// <summary>
/// Integration tests for <c>CorrelationIdMiddleware</c>.
///
/// Covers:
///   Req. 4 — every request must produce a log record that carries a
///             CorrelationId.  The observable contract tested here is the
///             X-Correlation-Id response header, which the middleware sets from
///             the same value it pushes into the log scope.
/// </summary>
public sealed class CorrelationIdMiddlewareTests(CatalogApiFactory factory)
    : IClassFixture<CatalogApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // ── propagation ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPing_WithCorrelationIdHeader_EchoesHeaderInResponse()
    {
        // Arrange
        const string correlationId = "test-correlation-123";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/ping");
        request.Headers.Add("X-Correlation-Id", correlationId);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.True(
            response.Headers.TryGetValues("X-Correlation-Id", out var values),
            "Response must include X-Correlation-Id header.");

        Assert.Equal(correlationId, values!.First());
    }

    // ── generation ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPing_WithoutCorrelationIdHeader_ResponseHasGeneratedGuid()
    {
        // Arrange — no X-Correlation-Id sent.
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/ping");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.True(
            response.Headers.TryGetValues("X-Correlation-Id", out var values),
            "Response must include X-Correlation-Id header even when the request omits it.");

        var generated = values!.First();

        Assert.True(
            Guid.TryParse(generated, out _),
            $"Generated X-Correlation-Id must be a valid GUID. Got: '{generated}'.");
    }

    // ── idempotency of value ──────────────────────────────────────────────────

    [Fact]
    public async Task GetPing_WithCorrelationIdHeader_DoesNotOverwriteWithNewGuid()
    {
        // Arrange — a caller-supplied value must survive the round-trip unchanged.
        const string originalId = "caller-provided-id-abc";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/ping");
        request.Headers.Add("X-Correlation-Id", originalId);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.TryGetValues("X-Correlation-Id", out var values);
        Assert.Equal(originalId, values!.First());
    }
}
