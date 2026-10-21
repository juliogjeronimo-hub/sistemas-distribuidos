namespace OrderHub.Catalog.Api.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    // Keycloak issuer URL, e.g. http://keycloak:8080/realms/orderhub
    public string Authority { get; init; } = string.Empty;

    // Expected audience claim: the client-id of this service.
    public string Audience { get; init; } = string.Empty;

    // Set to false only in local/compose dev where Keycloak runs over plain HTTP.
    // NEVER false in staging or production.
    public bool RequireHttpsMetadata { get; init; } = true;
}
