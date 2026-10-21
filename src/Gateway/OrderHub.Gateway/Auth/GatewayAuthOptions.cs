namespace OrderHub.Gateway.Auth;

public sealed class GatewayAuthOptions
{
    public const string SectionName = "Auth";

    // Keycloak issuer URL, e.g. http://keycloak:8080/realms/orderhub
    public string Authority { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    // Set to false only in local/compose dev where Keycloak runs over plain HTTP.
    // NEVER false in staging or production.
    public bool RequireHttpsMetadata { get; init; } = true;
}
