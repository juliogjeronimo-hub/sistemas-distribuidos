using Microsoft.Extensions.Options;
using OrderHub.Catalog.Application.Abstractions;

namespace OrderHub.Catalog.Api.Auth;

// Defense-in-depth: the Gateway validates the token and the Catalog validates it
// again so the service stays independently deployable and secure.
// NOTE: Base JWT authentication is registered by AddOrderHubJwtAuth in Program.
// This extension adds Catalog-specific authorization policies and scope-checking handler.
internal static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        // Add Catalog-specific authorization policies
        // These override the base policies from BuildingBlocks with more restrictive scope checking
        services.AddAuthorization(options =>
        {
            // CatalogRead: any authenticated user with the scope
            options.AddPolicy(CatalogScopes.Read, policy =>
                policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new ScopeRequirement(CatalogScopes.Read)));

            // CatalogWrite: admin role AND scope
            options.AddPolicy(CatalogScopes.Write, policy =>
                policy
                    .RequireAuthenticatedUser()
                    .RequireRole("admin")
                    .AddRequirements(new ScopeRequirement(CatalogScopes.Write)));
        });

        // Required so the authorization middleware can discover and invoke the
        // handler for ScopeRequirement.
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
            ScopeAuthorizationHandler>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        return services;
    }
}
