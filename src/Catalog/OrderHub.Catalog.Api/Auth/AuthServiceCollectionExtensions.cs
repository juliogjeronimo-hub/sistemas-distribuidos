using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using OrderHub.Catalog.Application.Abstractions;

namespace OrderHub.Catalog.Api.Auth;

/// <summary>
/// Registers JWT bearer authentication and scope-based authorization policies
/// for the Catalog service.
///
/// Defense-in-depth: the Gateway already validates the token; the Catalog validates
/// again so it stays independently deployable and secure.
/// </summary>
internal static class AuthServiceCollectionExtensions
{
    /// <summary>
    /// Adds JWT bearer auth bound to <see cref="AuthOptions"/> plus scope policies.
    /// Call this from the Api composition root.
    /// </summary>
    public static IServiceCollection AddCatalogAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        var authOptions = configuration
            .GetSection(AuthOptions.SectionName)
            .Get<AuthOptions>() ?? new AuthOptions();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authOptions.Authority;
                options.Audience = authOptions.Audience;
                options.RequireHttpsMetadata = authOptions.RequireHttpsMetadata;

                options.TokenValidationParameters = new()
                {
                    // Always validate signature, issuer, audience and expiry.
                    // These MUST NOT be disabled, even in dev environments.
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    // Clock skew tolerance — keep small to reduce the replay window.
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorization(options =>
        {
            // Scope-based policies. Handlers check policies, never raw claims.
            // Keycloak emits scopes as a space-separated string in the "scope" claim;
            // ScopeAuthorizationRequirement splits internally.
            options.AddScopePolicy(CatalogScopes.Read);
            options.AddScopePolicy(CatalogScopes.Write);
        });

        // ScopeAuthorizationHandler must be registered so the DI-based authorization
        // middleware can discover and invoke it for ScopeRequirement.
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
            ScopeAuthorizationHandler>();

        // ICurrentUser adapter — Scoped to follow the per-request HttpContext lifetime.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        return services;
    }

    // Extends AuthorizationOptions with a helper to keep policy registration DRY.
    private static void AddScopePolicy(
        this Microsoft.AspNetCore.Authorization.AuthorizationOptions options,
        string scope)
    {
        options.AddPolicy(scope, policy =>
            policy
                .RequireAuthenticatedUser()
                // ScopeRequirement splits the space-separated "scope" claim before
                // comparing, handling Keycloak's token format correctly.
                .AddRequirements(new ScopeRequirement(scope)));
    }
}
