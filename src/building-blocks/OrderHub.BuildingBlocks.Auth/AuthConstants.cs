namespace OrderHub.BuildingBlocks.Auth;

/// <summary>
/// Authorization policy names for OrderHub APIs.
/// </summary>
public static class Policies
{
    public const string CatalogRead = "CatalogRead";
    public const string CatalogWrite = "CatalogWrite";
    public const string InventoryRead = "InventoryRead";
    public const string InventoryWrite = "InventoryWrite";
    public const string AdminOnly = "AdminOnly";
}

/// <summary>
/// Claim type names used by OrderHub authentication.
/// </summary>
public static class Claims
{
    public const string Roles = "roles";
    public const string Scope = "scope";
}