using Microsoft.EntityFrameworkCore;
using OrderHub.Catalog.Domain.Aggregates;
using OrderHub.Catalog.Infrastructure.Data.Configurations;

namespace OrderHub.Catalog.Infrastructure.Data;

public sealed class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new ProductEntityTypeConfiguration());
    }
}
