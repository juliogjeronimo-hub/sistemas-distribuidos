using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace OrderHub.Catalog.Api.IntegrationTests;

/// <summary>
/// Generates valid JWT tokens for testing authorization policies.
/// Tokens include scope claims (space-separated) and realm roles as required by Keycloak/OAuth 2.0.
/// </summary>
internal static class TestTokenGenerator
{
    private const string Issuer = "https://keycloak:8080/realms/orderhub";
    private const string Audience = "catalog-api";

    /// <summary>
    /// Generates a JWT token with the specified scopes and roles.
    /// Uses a symmetric key for testing purposes.
    /// </summary>
    public static string GenerateToken(
        string subject = "test-user",
        IEnumerable<string>? scopes = null,
        IEnumerable<string>? roles = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            "this-is-a-test-secret-key-with-at-least-32-characters-for-hs256-algorithm"));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim("sub", subject),
            new Claim("iss", Issuer),
            new Claim("aud", Audience),
            new Claim("iat", ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString()),
            new Claim("exp", ((DateTimeOffset)DateTime.UtcNow.AddHours(1)).ToUnixTimeSeconds().ToString()),
        };

        // Add scopes as space-separated string (OAuth 2.0 standard)
        if (scopes != null)
        {
            var scopeString = string.Join(" ", scopes);
            claims.Add(new Claim("scope", scopeString));
        }

        // Add roles under realm_access structure (Keycloak-style)
        if (roles != null && roles.Any())
        {
            var roleList = string.Join("\",\"", roles);
            claims.Add(new Claim("realm_access", $"{{\"roles\":[\"{roleList}\"]}}"));

            // Also add individual role claims for ASP.NET Core role-based authorization
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(token);
    }
}
