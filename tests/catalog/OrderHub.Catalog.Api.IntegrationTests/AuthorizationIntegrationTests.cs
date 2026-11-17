using System.Net;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OrderHub.Catalog.Api.IntegrationTests;

/// <summary>
/// Integration tests for write endpoint authorization matrix.
///
/// Covers:
///   Req. 8 — endpoints must enforce authorization policies (CatalogRead, CatalogWrite).
///   Req. 2 — must verify 401 for anonymous, 403 for insufficient permissions, 200/201/204 for authorized.
///
/// Test Matrix (3 users × 3 endpoints = 9 tests):
///   Users:
///     1. Anonymous (no token)
///     2. Customer (alice) - has catalog.read but NOT catalog.write
///     3. Admin (bob) - has catalog.read, catalog.write, and admin role
///
///   Write Endpoints:
///     1. POST /v1/products (create)
///     2. PUT /v1/products/{id} (update)
///     3. POST /v1/products/{id}/deactivate (deactivate)
///
///   Expected Results:
///                           POST /v1/products    PUT /v1/products/{id}    POST /{id}/deactivate
///     Anonymous            401 Unauthorized     401 Unauthorized         401 Unauthorized
///     Customer (alice)     403 Forbidden        403 Forbidden            403 Forbidden
///     Admin (bob)          201 Created          200 OK                   204 No Content
/// </summary>
public sealed class AuthorizationIntegrationTests(CatalogApiFactory factory)
    : IClassFixture<CatalogApiFactory>
{
    private readonly CatalogApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    private readonly CreateProductRequest _createRequest = new(
        Sku: "TEST-001",
        Name: "Test Product",
        Description: "A test product",
        Price: 99.99m);

    private readonly UpdateProductRequest _updateRequest = new(
        Sku: "TEST-001-UPDATED",
        Name: "Updated Product",
        Description: "An updated test product",
        Price: 149.99m);

    // ── POST /v1/products ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateProduct_Anonymous_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        // No token added

        var response = await client.PostAsJsonAsync("/v1/products", _createRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_CustomerAlice_Returns403Forbidden()
    {
        var client = _factory.CreateClient();
        var token = TestTokenGenerator.GenerateToken(
            subject: "alice",
            scopes: ["catalog.read"], // Note: no catalog.write
            roles: null);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await client.PostAsJsonAsync("/v1/products", _createRequest);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_AdminBob_Returns201Created()
    {
        var client = _factory.CreateClient();
        var token = TestTokenGenerator.GenerateToken(
            subject: "bob",
            scopes: ["catalog.read", "catalog.write"],
            roles: ["admin"]);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await client.PostAsJsonAsync("/v1/products", _createRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith("/v1/products/", response.Headers.Location?.PathAndQuery);
    }

    // ── PUT /v1/products/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProduct_Anonymous_Returns401Unauthorized()
    {
        // Arrange: First create a product as admin so we have something to update
        var adminToken = TestTokenGenerator.GenerateToken(
            subject: "bob",
            scopes: ["catalog.read", "catalog.write"],
            roles: ["admin"]);
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new("Bearer", adminToken);

        var createResponse = await adminClient.PostAsJsonAsync("/v1/products", _createRequest);
        var productId = createResponse.Headers.Location?.Segments.Last() ?? "00000000-0000-0000-0000-000000000000";

        // Act: Try to update as anonymous user
        var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync($"/v1/products/{productId}", _updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_CustomerAlice_Returns403Forbidden()
    {
        // Arrange: First create a product as admin
        var adminToken = TestTokenGenerator.GenerateToken(
            subject: "bob",
            scopes: ["catalog.read", "catalog.write"],
            roles: ["admin"]);
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new("Bearer", adminToken);

        var createResponse = await adminClient.PostAsJsonAsync("/v1/products", _createRequest);
        var productId = createResponse.Headers.Location?.Segments.Last() ?? "00000000-0000-0000-0000-000000000000";

        // Act: Try to update as customer (read-only)
        var client = _factory.CreateClient();
        var token = TestTokenGenerator.GenerateToken(
            subject: "alice",
            scopes: ["catalog.read"], // Note: no catalog.write
            roles: null);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await client.PutAsJsonAsync($"/v1/products/{productId}", _updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_AdminBob_Returns200Ok()
    {
        // Arrange: First create a product as admin
        var adminToken = TestTokenGenerator.GenerateToken(
            subject: "bob",
            scopes: ["catalog.read", "catalog.write"],
            roles: ["admin"]);
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new("Bearer", adminToken);

        var createResponse = await adminClient.PostAsJsonAsync("/v1/products", _createRequest);
        var productId = createResponse.Headers.Location?.Segments.Last() ?? "00000000-0000-0000-0000-000000000000";

        // Act: Update as admin
        var response = await adminClient.PutAsJsonAsync($"/v1/products/{productId}", _updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── POST /v1/products/{id}/deactivate ────────────────────────────────────

    [Fact]
    public async Task DeactivateProduct_Anonymous_Returns401Unauthorized()
    {
        // Arrange: First create a product as admin so we have something to deactivate
        var adminToken = TestTokenGenerator.GenerateToken(
            subject: "bob",
            scopes: ["catalog.read", "catalog.write"],
            roles: ["admin"]);
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new("Bearer", adminToken);

        var createResponse = await adminClient.PostAsJsonAsync("/v1/products", _createRequest);
        var productId = createResponse.Headers.Location?.Segments.Last() ?? "00000000-0000-0000-0000-000000000000";

        // Act: Try to deactivate as anonymous user
        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/v1/products/{productId}/deactivate", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateProduct_CustomerAlice_Returns403Forbidden()
    {
        // Arrange: First create a product as admin
        var adminToken = TestTokenGenerator.GenerateToken(
            subject: "bob",
            scopes: ["catalog.read", "catalog.write"],
            roles: ["admin"]);
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new("Bearer", adminToken);

        var createResponse = await adminClient.PostAsJsonAsync("/v1/products", _createRequest);
        var productId = createResponse.Headers.Location?.Segments.Last() ?? "00000000-0000-0000-0000-000000000000";

        // Act: Try to deactivate as customer (read-only)
        var client = _factory.CreateClient();
        var token = TestTokenGenerator.GenerateToken(
            subject: "alice",
            scopes: ["catalog.read"], // Note: no catalog.write
            roles: null);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var response = await client.PostAsync($"/v1/products/{productId}/deactivate", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateProduct_AdminBob_Returns204NoContent()
    {
        // Arrange: First create a product as admin
        var adminToken = TestTokenGenerator.GenerateToken(
            subject: "bob",
            scopes: ["catalog.read", "catalog.write"],
            roles: ["admin"]);
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new("Bearer", adminToken);

        var createResponse = await adminClient.PostAsJsonAsync("/v1/products", _createRequest);
        var productId = createResponse.Headers.Location?.Segments.Last() ?? "00000000-0000-0000-0000-000000000000";

        // Act: Deactivate as admin
        var response = await adminClient.PostAsync($"/v1/products/{productId}/deactivate", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}

internal sealed record CreateProductRequest(
    string Sku,
    string Name,
    string Description,
    decimal Price);

internal sealed record UpdateProductRequest(
    string Sku,
    string Name,
    string Description,
    decimal Price);
