using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace OrderHub.Gateway.Auth;

/// <summary>
/// Registers JWT bearer authentication for the Gateway.
///
/// The Gateway is the first line of defense: requests without a valid token
/// are rejected with 401 before being proxied (Req. 2). Requests carrying a
/// valid token from the <c>orderhub</c> realm are forwarded to the destination
/// service (Req. 3). Each downstream service validates the token again
/// (defense-in-depth).
/// </summary>
internal static class AuthServiceCollectionExtensions
{
    /// <summary>
    /// Adds JWT bearer auth bound to <see cref="AuthOptions"/> plus a default
    /// policy that requires an authenticated user for every proxied route.
    /// Call this from the Gateway composition root.
    /// </summary>
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
            // Every proxied route requires an authenticated user by default.
            // Diagnostic endpoints opt out explicitly with AllowAnonymous.
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
