namespace OrderHub.Catalog.Application.Abstractions;

// Port exposing the caller's identity to handlers; implemented in the Api layer
// via HttpContext and stubbable in tests.
public interface ICurrentUser
{
    // Mapped from JWT "sub" claim; null when the caller is anonymous.
    string? UserId { get; }

    // Mapped from JWT "preferred_username" claim.
    string? UserName { get; }

    // Roles from JWT "roles" claim (typically from realm_access.roles).
    IReadOnlyCollection<string> Roles { get; }

    // Scopes from JWT "scope" claim (space-separated string).
    IReadOnlyCollection<string> Scopes { get; }

    bool IsAuthenticated { get; }

    bool HasScope(string scope);
}
