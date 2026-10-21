using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OrderHub.Catalog.Application.Abstractions;

namespace OrderHub.Catalog.Api.Auth;

// Registered as Scoped so it captures the per-request HttpContext.
internal sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal =>
        httpContextAccessor.HttpContext?.User;

    public string? Subject =>
        Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Principal?.FindFirstValue("sub");

    public string? UserName =>
        Principal?.FindFirstValue("preferred_username")
        ?? Principal?.FindFirstValue(ClaimTypes.Name);

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated == true;

    public bool HasScope(string scope)
    {
        var scopeClaim = Principal?.FindFirstValue("scope") ?? string.Empty;
        return scopeClaim
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Contains(scope, StringComparer.Ordinal);
    }
}
