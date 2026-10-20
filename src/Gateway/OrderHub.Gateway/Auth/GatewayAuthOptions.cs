namespace OrderHub.Gateway.Auth;

/// <summary>
/// Typed options for JWT bearer authentication in the Gateway.
/// Bound from the "Auth" section in appsettings / environment variables.
/// </summary>
public sealed class GatewayAuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Keycloak issuer URL, e.g. http://keycloak:8080/realms/orderhub</summary>
    public string Authority { get; init; } = string.Empty;

    /// <summary>Expected audience claim in the token.</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Set to false only in local/compose dev environments where Keycloak runs over plain HTTP.
    /// NEVER false in staging or production.
    /// </summary>
    public bool RequireHttpsMetadata { get; init; } = true;
}
