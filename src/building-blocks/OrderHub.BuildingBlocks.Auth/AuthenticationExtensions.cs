using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace OrderHub.BuildingBlocks.Auth;

/// <summary>
/// Authentication configuration options for OrderHub services.
/// Uses the Authority URL from the issuer (ValidIssuer), which automatically resolves
/// the .well-known/openid-configuration endpoint for metadata discovery.
/// </summary>
public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// The issuer URL (e.g., "http://keycloak:8080/realms/orderhub").
    /// Used as the Authority for automatic metadata discovery.
    /// </summary>
    public string ValidIssuer { get; set; } = string.Empty;
    
    /// <summary>
    /// The expected audience in the token (e.g., "orderhub-api").
    /// </summary>
    public string Audience { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether HTTPS is required for metadata discovery.
    /// Set to false in development when using HTTP.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}

/// <summary>
/// Extension methods for configuring OrderHub JWT authentication and authorization.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds OrderHub JWT authentication and authorization to the service collection.
    /// Reads configuration from the "Auth" section and registers authorization policies
    /// based on scopes (via "scope" claim) and roles (via "roles" claim).
    /// </summary>
    public static IServiceCollection AddOrderHubJwtAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{AuthOptions.SectionName}' is missing.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authOptions.ValidIssuer;
                options.Audience = authOptions.Audience;
                options.RequireHttpsMetadata = authOptions.RequireHttpsMetadata;
                options.MapInboundClaims = false;
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);
            });

        services.AddAuthorization(options =>
        {
            // Policies based on scope claim (space-separated scopes)
            options.AddPolicy(Policies.CatalogRead, policy =>
                policy.RequireAssertion(ctx => HasScope(ctx, "catalog.read")));
            options.AddPolicy(Policies.CatalogWrite, policy =>
                policy.RequireAssertion(ctx => HasScope(ctx, "catalog.write")));
            options.AddPolicy(Policies.InventoryRead, policy =>
                policy.RequireAssertion(ctx => HasScope(ctx, "inventory.read")));
            options.AddPolicy(Policies.InventoryWrite, policy =>
                policy.RequireAssertion(ctx => HasScope(ctx, "inventory.write")));

            // Policy based on roles claim
            options.AddPolicy(Policies.AdminOnly, policy =>
                policy.RequireClaim(Claims.Roles, "admin"));
        });

        return services;
    }

    private static bool HasScope(AuthorizationHandlerContext context, string requiredScope)
    {
        var scopeClaim = context.User.FindFirst(Claims.Scope);
        if (scopeClaim == null) return false;

        var scopes = scopeClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return scopes.Contains(requiredScope);
    }
}