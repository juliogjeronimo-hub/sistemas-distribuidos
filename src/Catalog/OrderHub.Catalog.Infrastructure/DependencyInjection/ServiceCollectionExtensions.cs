using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Catalog.Application.Abstractions;
using OrderHub.Catalog.Infrastructure.Data;
using OrderHub.Catalog.Infrastructure.Data.Repositories;

namespace OrderHub.Catalog.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Catalog")
            ?? throw new InvalidOperationException("Connection string 'Catalog' not found in configuration.");

        services.AddDbContext<CatalogDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static IHealthChecksBuilder AddCatalogHealthChecks(this IHealthChecksBuilder healthChecks)
    {
        healthChecks.AddDbContextCheck<CatalogDbContext>();
        return healthChecks;
    }

    public static IApplicationBuilder ApplyMigrations(this IApplicationBuilder app, IHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            using var scope = app.ApplicationServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            dbContext.Database.Migrate();
        }

        return app;
    }
}
