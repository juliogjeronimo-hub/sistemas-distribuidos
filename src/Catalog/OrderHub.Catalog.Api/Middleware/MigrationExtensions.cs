using Microsoft.EntityFrameworkCore;
using OrderHub.Catalog.Infrastructure.Data;

namespace OrderHub.Catalog.Api.Middleware;

public static class MigrationExtensions
{
    public static WebApplication ApplyMigrations(this WebApplication app, IHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            dbContext.Database.Migrate();
        }

        return app;
    }
}
