using System.Security.Claims;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OrderHub.BuildingBlocks.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();

builder.Services.AddOrderHubJwtAuth(builder.Configuration);

var app = builder.Build();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

// Endpoint /me returns user info from token (excluded from YARP)
app.MapGet("/me", (HttpContext context) =>
{
    var user = context.User;
    var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
    var scopeClaim = user.FindFirst("scope")?.Value ?? string.Empty;

    return Results.Ok(new
    {
        sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        preferred_username = user.FindFirst("preferred_username")?.Value,
        roles,
        scopes = scopeClaim,
        exp = user.FindFirst(ClaimTypes.Expiration)?.Value
    });
}).RequireAuthorization();

app.MapReverseProxy().RequireAuthorization();

app.Run();

// Exposes the implicit Program type for integration tests (WebApplicationFactory).
public partial class Program;