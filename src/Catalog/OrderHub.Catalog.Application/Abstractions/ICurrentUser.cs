namespace OrderHub.Catalog.Application.Abstractions;

/// <summary>
/// Port that exposes the authenticated caller's identity to application handlers.
/// Implemented in the Api layer via <c>HttpContext</c>; can be stubbed in tests.
/// </summary>
public interface ICurrentUser
{
    /// <summary>JWT subject claim (<c>sub</c>). Null when the caller is anonymous.</summary>
    string? Subject { get; }

    /// <summary>Preferred username extracted from the token.</summary>
    string? UserName { get; }

    /// <summary>True when an authenticated token is present.</summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Returns true when the access token contains the given scope.
    /// The Keycloak <c>scope</c> claim is a single space-separated string;
    /// this method handles the split internally.
    /// </summary>
    bool HasScope(string scope);
}
