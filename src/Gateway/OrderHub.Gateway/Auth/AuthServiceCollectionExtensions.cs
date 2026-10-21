using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace OrderHub.Gateway.Auth;

// The Gateway is the first line of defense: unauthenticated requests are rejected
// before being proxied, and each downstream service validates the token again.
internal static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayAuth(
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
            // FallbackPolicy makes every route require authentication unless it
            // opts out with AllowAnonymous (the diagnostic endpoints do).
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
