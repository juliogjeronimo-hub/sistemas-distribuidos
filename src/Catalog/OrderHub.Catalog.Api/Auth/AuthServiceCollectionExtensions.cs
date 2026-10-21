using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using OrderHub.Catalog.Application.Abstractions;

namespace OrderHub.Catalog.Api.Auth;

// Defense-in-depth: the Gateway validates the token and the Catalog validates it
// again so the service stays independently deployable and secure.
internal static class AuthServiceCollectionExtensions
{
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
                    // These validations MUST NOT be disabled, even in dev.
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    // Keep the skew small to reduce the replay window.
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddScopePolicy(CatalogScopes.Read);
            options.AddScopePolicy(CatalogScopes.Write);
        });

        // Required so the authorization middleware can discover and invoke the
        // handler for ScopeRequirement.
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
            ScopeAuthorizationHandler>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        return services;
    }

    private static void AddScopePolicy(
        this Microsoft.AspNetCore.Authorization.AuthorizationOptions options,
        string scope)
    {
        options.AddPolicy(scope, policy =>
            policy
                .RequireAuthenticatedUser()
                // ScopeRequirement splits the space-separated "scope" claim, which
                // is how Keycloak encodes scopes in the token.
                .AddRequirements(new ScopeRequirement(scope)));
    }
}
