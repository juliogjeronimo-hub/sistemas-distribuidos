using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Catalog.Application.Abstractions;
using OrderHub.Catalog.Domain.Aggregates;
using OrderHub.Catalog.Domain.ValueObjects;
using OrderHub.Catalog.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace OrderHub.Catalog.Api.IntegrationTests;

/// <summary>
/// Integration test for ProductRepository using a real PostgreSQL container.
/// This test verifies the complete flow: create → list → deactivate.
///
/// Covers:
///   - Real database operations via EF Core and Testcontainers
///   - Product creation with domain invariants enforced
///   - Listing products with and without inactive items
///   - Deactivation persistence across queries
///   - No mocks; all operations use actual PostgreSQL
/// </summary>
public sealed class ProductRepositoryIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    private CatalogDbContext _dbContext = null!;
    private IProductRepository _repository = null!;
    private ServiceProvider _serviceProvider = null!;

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("catalog_test")
            .WithUsername("catalog")
            .WithPassword("catalog_test_pw")
            .Build();

        await _container.StartAsync();

        // Create DbContext with test connection string
        var connectionString = _container.GetConnectionString();
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _dbContext = new CatalogDbContext(options);

        // Apply migrations to test database
        await _dbContext.Database.MigrateAsync();

        // Create service collection for DI
        var services = new ServiceCollection();
        services.AddScoped<CatalogDbContext>(_ => _dbContext);
        services.AddScoped<IProductRepository>(sp =>
        {
            var context = sp.GetRequiredService<CatalogDbContext>();
            // Create ProductRepository using reflection to access the internal type
            var repoType = typeof(CatalogDbContext).Assembly
                .GetType("OrderHub.Catalog.Infrastructure.Data.Repositories.ProductRepository")
                ?? throw new InvalidOperationException("ProductRepository type not found");
            return (IProductRepository)Activator.CreateInstance(repoType, context)!;
        });

        _serviceProvider = services.BuildServiceProvider();
        _repository = _serviceProvider.GetRequiredService<IProductRepository>();
    }

    public async Task DisposeAsync()
    {
        // Cleanup DbContext and provider
        if (_dbContext != null)
        {
            await _dbContext.DisposeAsync();
        }

        if (_serviceProvider != null)
        {
            await _serviceProvider.DisposeAsync();
        }

        // Cleanup container
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }

    [Fact]
    public async Task Create_List_Deactivate_Flow_Succeeds()
    {
        // Arrange: Create 2-3 products with different SKUs and names
        var timeProvider = TimeProvider.System;

        // Create Money values for product prices
        var price1Result = Money.Create(5999.99m);
        var price2Result = Money.Create(129.99m);
        var price3Result = Money.Create(449.99m);

        Assert.True(price1Result.IsSuccess);
        Assert.True(price2Result.IsSuccess);
        Assert.True(price3Result.IsSuccess);

        var product1Result = Product.Create(
            sku: "LAPTOP-001",
            name: "Gaming Laptop",
            description: "High-performance gaming laptop",
            price: price1Result.Value,
            timeProvider: timeProvider);

        var product2Result = Product.Create(
            sku: "MOUSE-001",
            name: "Wireless Mouse",
            description: "Ergonomic wireless mouse",
            price: price2Result.Value,
            timeProvider: timeProvider);

        var product3Result = Product.Create(
            sku: "KEYBOARD-001",
            name: "Mechanical Keyboard",
            description: "RGB mechanical keyboard",
            price: price3Result.Value,
            timeProvider: timeProvider);

        // All products should be created successfully
        Assert.True(product1Result.IsSuccess);
        Assert.True(product2Result.IsSuccess);
        Assert.True(product3Result.IsSuccess);

        var product1 = product1Result.Value;
        var product2 = product2Result.Value;
        var product3 = product3Result.Value;

        // Act 1: Create (insert products via repository)
        await _repository.AddAsync(product1, CancellationToken.None);
        await _repository.AddAsync(product2, CancellationToken.None);
        await _repository.AddAsync(product3, CancellationToken.None);

        // Commit to database
        _dbContext.SaveChanges();

        // Assert 1: All products are created and active
        var allProducts = await _repository.ListAsync(includeInactive: true, CancellationToken.None);
        Assert.Equal(3, allProducts.Count);

        var listActive = await _repository.ListAsync(includeInactive: false, CancellationToken.None);
        Assert.Equal(3, listActive.Count);

        Assert.All(listActive, p => Assert.True(p.IsActive));

        // Verify each product exists with correct properties
        var createdProduct1 = await _repository.GetBySkuAsync("LAPTOP-001", CancellationToken.None);
        Assert.NotNull(createdProduct1);
        Assert.Equal("Gaming Laptop", createdProduct1.Name);
        Assert.Equal(5999.99m, createdProduct1.Price.Amount);
        Assert.True(createdProduct1.IsActive);

        var createdProduct2 = await _repository.GetBySkuAsync("MOUSE-001", CancellationToken.None);
        Assert.NotNull(createdProduct2);
        Assert.Equal("Wireless Mouse", createdProduct2.Name);
        Assert.Equal(129.99m, createdProduct2.Price.Amount);
        Assert.True(createdProduct2.IsActive);

        // Act 2: Deactivate the first product
        createdProduct1.Deactivate();
        await _repository.UpdateAsync(createdProduct1, CancellationToken.None);
        _dbContext.SaveChanges();

        // Assert 2: Verify deactivation persists across queries
        var updatedProduct1 = await _repository.GetBySkuAsync("LAPTOP-001", CancellationToken.None);
        Assert.NotNull(updatedProduct1);
        Assert.False(updatedProduct1.IsActive);

        // Act 3: List again with includeInactive flag
        var activeOnlyAfterDeactivate = await _repository.ListAsync(
            includeInactive: false,
            CancellationToken.None);

        var allProductsAfterDeactivate = await _repository.ListAsync(
            includeInactive: true,
            CancellationToken.None);

        // Assert 3: Active-only list excludes deactivated product
        Assert.Equal(2, activeOnlyAfterDeactivate.Count);
        Assert.All(activeOnlyAfterDeactivate, p => Assert.True(p.IsActive));

        // Total list still has 3 products
        Assert.Equal(3, allProductsAfterDeactivate.Count);

        // Verify the deactivated product is still in the total list
        var deactivatedInList = allProductsAfterDeactivate.FirstOrDefault(p => p.Sku == "LAPTOP-001");
        Assert.NotNull(deactivatedInList);
        Assert.False(deactivatedInList.IsActive);

        // Verify the other products remain active
        var product2InList = allProductsAfterDeactivate.FirstOrDefault(p => p.Sku == "MOUSE-001");
        Assert.NotNull(product2InList);
        Assert.True(product2InList.IsActive);

        var product3InList = allProductsAfterDeactivate.FirstOrDefault(p => p.Sku == "KEYBOARD-001");
        Assert.NotNull(product3InList);
        Assert.True(product3InList.IsActive);

        // Assert 4: No stale data — query again to confirm persistence
        var finalCheckProduct1 = await _repository.GetByIdAsync(product1.Id, CancellationToken.None);
        Assert.NotNull(finalCheckProduct1);
        Assert.False(finalCheckProduct1.IsActive);
        Assert.Equal("Gaming Laptop", finalCheckProduct1.Name);

        var finalCheckProduct2 = await _repository.GetByIdAsync(product2.Id, CancellationToken.None);
        Assert.NotNull(finalCheckProduct2);
        Assert.True(finalCheckProduct2.IsActive);
        Assert.Equal("Wireless Mouse", finalCheckProduct2.Name);
    }
}
