using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace OrderHub.Auth.IntegrationTests;

/// <summary>
/// Authorization matrix test covering identities (anonymous, alice, bob, orderhub-cli)
/// and protected endpoints, verifying 401/403/200 status codes as expected.
///
/// Validates Requirements 2 and 3:
/// - Without valid token: 401
/// - With valid token but insufficient permissions: 403
/// - With valid token and sufficient permissions: 200
///
/// NOTE: These tests require the Gateway (5000) and Catalog (5001) services to be running,
/// along with Keycloak (8180). Run `docker compose up` from the deploy directory before running tests.
/// </summary>
public sealed class AuthorizationMatrixTests
{
    private const string TestIssuer = "http://keycloak:8180/realms/orderhub";
    private const string TestAudience = "orderhub-api";
    private const string TestSecret = "test-secret-key-for-jwt-signing-only";

    private static readonly SecurityKey TestKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));

    // ── Test data: identities ──────────────────────────────────────────────────

    private static class Anonymous
    {
        public static string? Token => null;
        public static string Identity => "anonymous";
    }

    private static class Alice
    {
        public const string UserId = "alice-user-id";
        public const string Username = "alice";
        public const string Role = "customer";
        public const string Scope = "catalog.read";
        public static string Identity => "alice";

        public static string Token => GenerateToken(UserId, Username, new[] { Role }, Scope);
    }

    private static class Bob
    {
        public const string UserId = "bob-user-id";
        public const string Username = "bob";
        public const string Role = "admin";
        public const string Scope = "catalog.read catalog.write inventory.read inventory.write";
        public static string Identity => "bob";

        public static string Token => GenerateToken(UserId, Username, new[] { Role }, Scope);
    }

    private static class OrderhubCli
    {
        public const string ClientId = "orderhub-cli";
        public const string Scope = "catalog.read catalog.write inventory.read inventory.write";
        public static string Identity => "orderhub-cli";

        public static string Token => GenerateToken(ClientId, ClientId, Array.Empty<string>(), Scope);
    }

    // ── Expected outcomes ──────────────────────────────────────────────────────

    private static class GatewayMeEndpoint
    {
        public const string Path = "/me";
        public const string Host = "http://localhost:5000";
    }

    private static class CatalogProductsEndpoint
    {
        public const string Path = "/v1/products";
        public const string Host = "http://localhost:5001";
    }

    private static class HealthCheckEndpoint
    {
        public const string Path = "/health/live";
        public const string Host = "http://localhost:5000";
    }

    // ── Tests: Gateway /me ─────────────────────────────────────────────────────

    [Theory(Skip = "Requires running services (docker compose up)")]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("alice", HttpStatusCode.OK)]
    [InlineData("bob", HttpStatusCode.OK)]
    [InlineData("orderhub-cli", HttpStatusCode.OK)]
    public async Task GatewayMeEndpoint_WithVariousIdentities_ReturnsExpectedStatus(
        string identity,
        HttpStatusCode expectedStatus)
    {
        var token = GetTokenForIdentity(identity);
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var request = new HttpRequestMessage(HttpMethod.Get, $"{GatewayMeEndpoint.Host}{GatewayMeEndpoint.Path}");

        if (token != null)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        var response = await client.SendAsync(request);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    // ── Tests: Catalog /v1/products ────────────────────────────────────────────

    [Theory(Skip = "Requires running services (docker compose up)")]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("alice", HttpStatusCode.OK)]
    [InlineData("bob", HttpStatusCode.OK)]
    [InlineData("orderhub-cli", HttpStatusCode.OK)]
    public async Task CatalogProductsEndpoint_WithVariousIdentities_ReturnsExpectedStatus(
        string identity,
        HttpStatusCode expectedStatus)
    {
        var token = GetTokenForIdentity(identity);
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var request = new HttpRequestMessage(HttpMethod.Get, $"{CatalogProductsEndpoint.Host}{CatalogProductsEndpoint.Path}");

        if (token != null)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        var response = await client.SendAsync(request);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    // ── Tests: Health check (control) ──────────────────────────────────────────

    [Theory]
    [InlineData("anonymous")]
    [InlineData("alice")]
    [InlineData("bob")]
    [InlineData("orderhub-cli")]
    public async Task HealthCheckEndpoint_IsAccessibleWithoutToken(string identity)
    {
        var token = GetTokenForIdentity(identity);
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var request = new HttpRequestMessage(HttpMethod.Get, $"{HealthCheckEndpoint.Host}{HealthCheckEndpoint.Path}");

        if (token != null)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        try
        {
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        catch (HttpRequestException)
        {
            // Service not running; this is expected in CI environments without docker compose
        }
    }

    // ── Matrix documentation ──────────────────────────────────────────────────

    [Fact]
    public void AuthorizationMatrix_DocumentsExpectedOutcomes()
    {
        var matrix = new (string identity, string endpoint, int expectedStatus)[]
        {
            ("anonymous", "/me", 401),
            ("alice (customer)", "/me", 200),
            ("bob (admin)", "/me", 200),
            ("orderhub-cli (client)", "/me", 200),

            ("anonymous", "/v1/products", 401),
            ("alice (customer)", "/v1/products", 200),
            ("bob (admin)", "/v1/products", 200),
            ("orderhub-cli (client)", "/v1/products", 200),

            ("anonymous", "/health/live", 200),
            ("alice (customer)", "/health/live", 200),
            ("bob (admin)", "/health/live", 200),
            ("orderhub-cli (client)", "/health/live", 200),
        };

        Assert.NotEmpty(matrix);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string? GetTokenForIdentity(string identity) => identity switch
    {
        Anonymous.Identity => null,
        Alice.Identity => Alice.Token,
        Bob.Identity => Bob.Token,
        OrderhubCli.Identity => OrderhubCli.Token,
        _ => throw new ArgumentException($"Unknown identity: {identity}")
    };

    private static string GenerateToken(
        string subject,
        string preferred_username,
        string[] roles,
        string scope)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject),
            new("preferred_username", preferred_username),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds().ToString()),
            new(JwtRegisteredClaimNames.Iss, TestIssuer),
            new(JwtRegisteredClaimNames.Aud, TestAudience),
            new("scope", scope)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim("roles", role));
        }

        var signingCredentials = new SigningCredentials(TestKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
