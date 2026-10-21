using Microsoft.AspNetCore.Authorization;

namespace OrderHub.Catalog.Api.Auth;

/// <summary>
/// Custom authorization requirement that checks whether the authenticated user's
/// token contains a specific scope value.
///
/// Keycloak (and most OAuth 2.0 servers) emit scopes as a single space-separated
/// string in the <c>scope</c> claim, e.g. <c>"openid profile catalog.read"</c>.
/// The built-in <c>RequireClaim</c> does an exact string comparison, which would
/// fail to match any individual scope in that string. This handler splits the claim
/// value before comparing.
/// </summary>
public sealed class ScopeRequirement(string scope) : IAuthorizationRequirement
{
    public string Scope { get; } = scope;
}

internal sealed class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ScopeRequirement requirement)
    {
        var scopeClaim = context.User.FindFirst("scope")?.Value ?? string.Empty;

        var scopes = scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (scopes.Contains(requirement.Scope, StringComparer.Ordinal))
        {
            context.Succeed(requirement);
        }

        // Do not call context.Fail() — let other handlers run if needed.
        return Task.CompletedTask;
    }
}
