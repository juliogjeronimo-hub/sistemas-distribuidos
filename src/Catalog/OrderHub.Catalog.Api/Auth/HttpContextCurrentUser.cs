using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OrderHub.Catalog.Application.Abstractions;

namespace OrderHub.Catalog.Api.Auth;

/// <summary>
/// Resolves the current user identity from the active <see cref="HttpContext"/>.
/// Registered as Scoped so it captures the per-request HttpContext.
/// </summary>
internal sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal =>
        httpContextAccessor.HttpContext?.User;

    /// <inheritdoc/>
    public string? Subject =>
        Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Principal?.FindFirstValue("sub");

    /// <inheritdoc/>
    public string? UserName =>
        Principal?.FindFirstValue("preferred_username")
        ?? Principal?.FindFirstValue(ClaimTypes.Name);

    /// <inheritdoc/>
    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated == true;

    /// <inheritdoc/>
    public bool HasScope(string scope)
    {
        // Keycloak emits scopes as a single space-separated string in the "scope" claim.
        var scopeClaim = Principal?.FindFirstValue("scope") ?? string.Empty;
        return scopeClaim
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Contains(scope, StringComparer.Ordinal);
    }
}
