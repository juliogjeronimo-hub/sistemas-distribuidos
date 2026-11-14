using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OrderHub.Catalog.Application.Abstractions;

namespace OrderHub.Catalog.Api.Auth;

// Registered as Scoped so it captures the per-request HttpContext.
internal sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal =>
        httpContextAccessor.HttpContext?.User;

    public string? UserId =>
        Principal?.FindFirstValue("sub")
        ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName =>
        Principal?.FindFirstValue("preferred_username")
        ?? Principal?.FindFirstValue(ClaimTypes.Name);

    public IReadOnlyCollection<string> Roles
    {
        get
        {
            // Keycloak typically puts realm roles in "realm_access.roles".
            var realmAccess = Principal?.FindFirstValue("realm_access");
            if (!string.IsNullOrEmpty(realmAccess))
            {
                // realm_access is a JSON object like: {"roles":["admin","customer"]}
                try
                {
                    var json = System.Text.Json.JsonDocument.Parse(realmAccess);
                    var roles = json.RootElement.GetProperty("roles");
                    var roleList = new List<string>();
                    foreach (var role in roles.EnumerateArray())
                    {
                        roleList.Add(role.GetString() ?? string.Empty);
                    }
                    return roleList;
                }
                catch
                {
                    // Invalid JSON, fall through to empty list.
                }
            }

            // Fallback: check for direct "roles" claim.
            var directRoles = Principal?.FindAll("roles")
                .Select(c => c.Value)
                .ToList() ?? new List<string>();

            return directRoles;
        }
    }

    public IReadOnlyCollection<string> Scopes
    {
        get
        {
            var scopeClaim = Principal?.FindFirstValue("scope") ?? string.Empty;
            return scopeClaim
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }
    }

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated == true;

    public bool HasScope(string scope) =>
        Scopes.Contains(scope, StringComparer.Ordinal);
}
