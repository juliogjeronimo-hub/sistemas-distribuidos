using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Catalog.Application.Abstractions;
using OrderHub.Catalog.Application.Abstractions.Messaging;
using OrderHub.Catalog.Application.DependencyInjection;
using OrderHub.Catalog.Application.UseCases.Commands;
using OrderHub.Catalog.Application.UseCases.DTOs;
using OrderHub.Catalog.Application.UseCases.Queries;
using OrderHub.Catalog.Domain.Aggregates;
using OrderHub.Catalog.Domain.Common;
using OrderHub.Catalog.Domain.ValueObjects;
using Xunit;

namespace OrderHub.Catalog.Application.UnitTests.UseCases.Handlers;

public class ValidationDecoratorChainTests
{
    [Fact]
    public void ApplicationServices_ShouldRegisterHandlers_WithValidationDecorators()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        var sp = services.BuildServiceProvider();

        var handler = sp.GetRequiredService<ICommandHandler<CreateProductCommand, Guid>>();
        handler.Should().NotBeNull();
        handler.GetType().Name.Should().Contain("Decorator");
    }

    [Fact]
    public async Task CreateProductCommandHandler_ShouldValidate_AndReturnValidationError_WhenSkuIsEmpty()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        var mockRepository = new MockProductRepository();
        services.AddScoped<IProductRepository>(_ => mockRepository);
        services.AddScoped<IUnitOfWork, MockUnitOfWork>();
        services.AddScoped(_ => TimeProvider.System);

        var sp = services.BuildServiceProvider();

        var handler = sp.GetRequiredService<ICommandHandler<CreateProductCommand, Guid>>();

        var command = new CreateProductCommand(
            Sku: "",
            Name: "Test Product",
            Description: "A test product",
            Price: 100m);

        var result = await handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().StartWith("Validation");
    }

    [Fact]
    public async Task CreateProductCommandHandler_ShouldValidate_AndReturnValidationError_WhenPriceIsZero()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        var mockRepository = new MockProductRepository();
        services.AddScoped<IProductRepository>(_ => mockRepository);
        services.AddScoped<IUnitOfWork, MockUnitOfWork>();
        services.AddScoped(_ => TimeProvider.System);

        var sp = services.BuildServiceProvider();

        var handler = sp.GetRequiredService<ICommandHandler<CreateProductCommand, Guid>>();

        var command = new CreateProductCommand(
            Sku: "PROD-001",
            Name: "Test Product",
            Description: "A test product",
            Price: 0);

        var result = await handler.HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().StartWith("Validation");
    }

    [Fact]
    public async Task ListProductsQueryHandler_ShouldValidate_AndReturnValidationError_WhenPageIsZero()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        var mockRepository = new MockProductRepository();
        services.AddScoped<IProductRepository>(_ => mockRepository);
        services.AddScoped<ICurrentUser, MockCurrentUser>();

        var sp = services.BuildServiceProvider();

        var handler = sp.GetRequiredService<IQueryHandler<ListProductsQuery, PaginatedResponse<ProductResponse>>>();

        var query = new ListProductsQuery(
            Page: 0,
            PageSize: 20);

        var result = await handler.HandleAsync(query);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().StartWith("Validation");
    }

    [Fact]
    public async Task ListProductsQueryHandler_ShouldValidate_AndReturnValidationError_WhenPageSizeExceedsMax()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        var mockRepository = new MockProductRepository();
        services.AddScoped<IProductRepository>(_ => mockRepository);
        services.AddScoped<ICurrentUser, MockCurrentUser>();

        var sp = services.BuildServiceProvider();

        var handler = sp.GetRequiredService<IQueryHandler<ListProductsQuery, PaginatedResponse<ProductResponse>>>();

        var query = new ListProductsQuery(
            Page: 1,
            PageSize: 100);

        var result = await handler.HandleAsync(query);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().StartWith("Validation");
    }


    private sealed class MockProductRepository : IProductRepository
    {
        private readonly List<Product> _products = [];

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return Task.FromResult(_products.FirstOrDefault(p => p.Id == id));
        }

        public Task<Product?> GetBySkuAsync(string sku, CancellationToken ct)
        {
            return Task.FromResult(_products.FirstOrDefault(p => p.Sku == sku));
        }

        public Task<List<Product>> ListAsync(bool includeInactive, CancellationToken ct)
        {
            var result = includeInactive
                ? _products
                : _products.Where(p => p.IsActive).ToList();

            return Task.FromResult(result);
        }

        public Task AddAsync(Product product, CancellationToken ct)
        {
            _products.Add(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product product, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class MockUnitOfWork : IUnitOfWork
    {
        public Task CommitAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class MockCurrentUser : ICurrentUser
    {
        public string? UserId => "test-user";
        public string? UserName => "testuser";
        public IReadOnlyCollection<string> Roles => new[] { "admin" };
        public IReadOnlyCollection<string> Scopes => new[] { "catalog.read", "catalog.write" };
        public bool IsAuthenticated => true;

        public bool HasScope(string scope)
        {
            return Scopes.Contains(scope);
        }
    }
}

// Extensions and helper DTOs needed by the test
internal sealed record PaginatedResponse<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

internal sealed record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
