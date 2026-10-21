using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

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

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();

app.UseAuthorization();

app.MapHealthChecks("/health/live");

app.MapHealthChecks("/health/ready");

app.MapReverseProxy().RequireAuthorization();

app.Run();

// Exposes the implicit Program type for integration tests (WebApplicationFactory).
public partial class Program;
