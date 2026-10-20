namespace OrderHub.Catalog.Api.Auth;

/// <summary>
/// Scope constants for the Catalog service.
/// Used both in policy registration and in endpoint authorization declarations.
/// </summary>
public static class CatalogScopes
{
    /// <summary>Grants read-only access to catalog resources.</summary>
    public const string Read = "catalog.read";

    /// <summary>Grants write access to catalog resources.</summary>
    public const string Write = "catalog.write";
}
