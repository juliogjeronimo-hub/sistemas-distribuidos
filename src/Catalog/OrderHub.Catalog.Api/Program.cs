using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OrderHub.Catalog.Application.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();

builder.Services.AddHealthChecks();

var auth = builder.Configuration.GetSection("Auth");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MetadataAddress = auth["MetadataAddress"]!;
        o.RequireHttpsMetadata = bool.Parse(auth["RequireHttpsMetadata"] ?? "true");
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = auth["ValidIssuer"],
            ValidAudience = auth["Audience"],
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("catalog.read", p => p.RequireAssertion(ctx =>
        // Keycloak sends "scope" as a single space-separated string.
        ctx.User.FindFirst("scope")?.Value.Split(' ').Contains("catalog.read") == true));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live");

app.MapHealthChecks("/health/ready");

app.MapGet("/v1/products", () => new[]
{
    new { Id = Guid.NewGuid(), Name = "Keyboard", Price = 199.90m },
    new { Id = Guid.NewGuid(), Name = "Mouse", Price = 89.90m }
}).RequireAuthorization("catalog.read");

app.Run();

// Exposes the implicit Program type for integration tests (WebApplicationFactory).
public partial class Program;
