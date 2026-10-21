namespace OrderHub.Catalog.Application.Abstractions;

// Port exposing the caller's identity to handlers; implemented in the Api layer
// via HttpContext and stubbable in tests.
public interface ICurrentUser
{
    // JWT subject claim (sub); null when the caller is anonymous.
    string? Subject { get; }

    string? UserName { get; }

    bool IsAuthenticated { get; }

    // The Keycloak "scope" claim is a single space-separated string; implementations
    // split it before checking for the given scope.
    bool HasScope(string scope);
}
