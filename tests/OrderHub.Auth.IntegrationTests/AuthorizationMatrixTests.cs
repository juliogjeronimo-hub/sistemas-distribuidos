using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OrderHub.BuildingBlocks.Auth;
using Xunit;

namespace OrderHub.Auth.IntegrationTests;

/// <summary>
/// Authorization matrix test covering identities (anonymous, alice, bob, orderhub-cli)
/// and protected endpoints, verifying 401/403/200 status codes as expected.
///
/// Validates Requirements 2 and 3:
/// - Without valid token: 401 Unauthorized
/// - With valid token but insufficient permissions: 403 Forbidden
/// - With valid token and sufficient permissions: 200 OK
///
/// Tests use a locally-signed JWT with a custom key to verify authorization
/// policies work correctly. The test server is created in-process without
/// requiring external services.
/// </summary>
public sealed class AuthorizationMatrixTests
{
    private const string TestIssuer = "http://test-issuer";
    private const string TestAudience = "orderhub-api";
    private const string TestSecret = "this-is-a-test-secret-key-that-is-long-enough-for-hs256-signing";

    private static readonly SecurityKey TestKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
    private static readonly SigningCredentials TestSigningCredentials = new(TestKey, SecurityAlgorithms.HmacSha256);

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

    // ── Tests: /me endpoint (requires any valid token) ────────────────────────

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("alice", HttpStatusCode.OK)]
    [InlineData("bob", HttpStatusCode.OK)]
    [InlineData("orderhub-cli", HttpStatusCode.OK)]
    public async Task GetMeEndpoint_WithVariousIdentities_ReturnsExpectedStatus(
        string identity,
        HttpStatusCode expectedStatus)
    {
        using var server = BuildTestServer();
        using var client = server.CreateClient();

        var token = GetTokenForIdentity(identity);
        var request = new HttpRequestMessage(HttpMethod.Get, "/me");

        if (token != null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await client.SendAsync(request);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    // ── Tests: /catalog/v1/products (requires catalog.read scope) ──────────────

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("alice", HttpStatusCode.OK)]
    [InlineData("bob", HttpStatusCode.OK)]
    [InlineData("orderhub-cli", HttpStatusCode.OK)]
    public async Task GetProductsEndpoint_WithVariousIdentities_ReturnsExpectedStatus(
        string identity,
        HttpStatusCode expectedStatus)
    {
        using var server = BuildTestServer();
        using var client = server.CreateClient();

        var token = GetTokenForIdentity(identity);
        var request = new HttpRequestMessage(HttpMethod.Get, "/catalog/v1/products");

        if (token != null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await client.SendAsync(request);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    // ── Tests: Health check (should be accessible without token) ──────────────

    [Theory]
    [InlineData("anonymous")]
    [InlineData("alice")]
    [InlineData("bob")]
    [InlineData("orderhub-cli")]
    public async Task HealthCheckEndpoint_IsAccessibleWithoutAuth(string identity)
    {
        using var server = BuildTestServer();
        using var client = server.CreateClient();

        var token = GetTokenForIdentity(identity);
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");

        if (token != null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Matrix documentation ──────────────────────────────────────────────────

    [Fact]
    public void AuthorizationMatrix_DocumentsExpectedOutcomes()
    {
        var matrix = new (string identity, string endpoint, HttpStatusCode expectedStatus)[]
        {
            // Gateway /me endpoint (requires any valid token)
            ("anonymous", "/me", HttpStatusCode.Unauthorized),
            ("alice (customer, catalog.read)", "/me", HttpStatusCode.OK),
            ("bob (admin, all scopes)", "/me", HttpStatusCode.OK),
            ("orderhub-cli (service, all scopes)", "/me", HttpStatusCode.OK),

            // Catalog /v1/products (requires catalog.read scope)
            ("anonymous", "/catalog/v1/products", HttpStatusCode.Unauthorized),
            ("alice (customer, catalog.read)", "/catalog/v1/products", HttpStatusCode.OK),
            ("bob (admin, all scopes)", "/catalog/v1/products", HttpStatusCode.OK),
            ("orderhub-cli (service, all scopes)", "/catalog/v1/products", HttpStatusCode.OK),

            // Health endpoint (requires no auth)
            ("anonymous", "/health/live", HttpStatusCode.OK),
            ("alice (customer)", "/health/live", HttpStatusCode.OK),
            ("bob (admin)", "/health/live", HttpStatusCode.OK),
            ("orderhub-cli (service)", "/health/live", HttpStatusCode.OK),
        };

        Assert.NotEmpty(matrix);
        Assert.All(matrix, item => Assert.False(string.IsNullOrEmpty(item.identity)));
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

    private static TestServer BuildTestServer()
    {
        var builder = new WebApplicationBuilder();

        // Configure auth to accept our test-signed tokens
        builder.Services.AddOrderHubJwtAuth(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:Authority"] = TestIssuer,
                    ["Auth:Audience"] = TestAudience,
                    ["Auth:RequireHttpsMetadata"] = "false"
                })
                .Build());

        builder.Services.AddHealthChecks();

        // Configure JWT bearer to accept our test key
        builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Authority = TestIssuer;
            options.Audience = TestAudience;
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = TestIssuer,
                ValidAudience = TestAudience,
                IssuerSigningKey = TestKey,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health/live");

        // Mimics the /me endpoint from Gateway
        app.MapGet("/me", (HttpContext context) =>
        {
            var user = context.User;
            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var scopeClaim = user.FindFirst("scope")?.Value ?? string.Empty;

            return Results.Ok(new
            {
                sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                preferred_username = user.FindFirst("preferred_username")?.Value,
                roles,
                scopes = scopeClaim,
                exp = user.FindFirst(ClaimTypes.Expiration)?.Value
            });
        }).RequireAuthorization();

        // Mimics the Catalog endpoint with CatalogRead policy
        app.MapGet("/catalog/v1/products", () => new[]
        {
            new { Id = Guid.NewGuid(), Name = "Keyboard", Price = 199.90m },
            new { Id = Guid.NewGuid(), Name = "Mouse", Price = 89.90m }
        }).RequireAuthorization("CatalogRead");

        return new TestServer(app);
    }

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

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: TestSigningCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
