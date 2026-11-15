using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
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
}
