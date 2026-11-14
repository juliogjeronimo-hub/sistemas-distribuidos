using OrderHub.Catalog.Domain.Aggregates;
using OrderHub.Catalog.Domain.ValueObjects;
using Xunit;

namespace OrderHub.Catalog.Domain.UnitTests.Aggregates;

public class ProductTests
{
    private static readonly TimeProvider TimeProvider = TimeProvider.System;

    [Fact]
    public void Create_WithValidData_ReturnsSuccess()
    {
        var price = Money.Create(99.99m).Value;

        var result = Product.Create(
            "PROD-001",
            "Test Product",
            "A test product",
            price,
            TimeProvider);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal("PROD-001", result.Value.Sku);
        Assert.Equal("Test Product", result.Value.Name);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public void Create_WithEmptySku_ReturnsFailure()
    {
        var price = Money.Create(99.99m).Value;

        var result = Product.Create(
            string.Empty,
            "Test Product",
            "A test product",
            price,
            TimeProvider);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Sku", result.Error.Code);
    }

    [Fact]
    public void Create_WithSkuTooShort_ReturnsFailure()
    {
        var price = Money.Create(99.99m).Value;

        var result = Product.Create(
            "AB",
            "Test Product",
            "A test product",
            price,
            TimeProvider);

        Assert.True(result.IsFailure);
        Assert.Contains("between 3 and 32", result.Error.Message);
    }

    [Fact]
    public void Create_WithSkuTooLong_ReturnsFailure()
    {
        var price = Money.Create(99.99m).Value;
        var longSku = new string('A', 33);

        var result = Product.Create(
            longSku,
            "Test Product",
            "A test product",
            price,
            TimeProvider);

        Assert.True(result.IsFailure);
        Assert.Contains("between 3 and 32", result.Error.Message);
    }

    [Fact]
    public void Create_WithSkuContainingInvalidCharacters_ReturnsFailure()
    {
        var price = Money.Create(99.99m).Value;

        var result = Product.Create(
            "PROD@001",
            "Test Product",
            "A test product",
            price,
            TimeProvider);

        Assert.True(result.IsFailure);
        Assert.Contains("alphanumeric characters and hyphens", result.Error.Message);
    }

    [Fact]
    public void Create_WithValidSkuFormat_ReturnsSuccess()
    {
        var price = Money.Create(99.99m).Value;
        var validSkus = new[] { "ABC", "PROD-001", "P-123", "1-2-3", "ABC123DEF" };

        foreach (var sku in validSkus)
        {
            var result = Product.Create(sku, "Test", "Description", price, TimeProvider);
            Assert.True(result.IsSuccess, $"SKU '{sku}' should be valid");
        }
    }

    [Fact]
    public void Create_SetsIsActiveToTrue()
    {
        var price = Money.Create(99.99m).Value;

        var result = Product.Create(
            "PROD-001",
            "Test Product",
            "A test product",
            price,
            TimeProvider);

        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public void Create_SetsCreatedAtAndUpdatedAtToNow()
    {
        var price = Money.Create(99.99m).Value;
        var beforeCreate = TimeProvider.GetUtcNow();

        var result = Product.Create(
            "PROD-001",
            "Test Product",
            "A test product",
            price,
            TimeProvider);

        var afterCreate = TimeProvider.GetUtcNow();
        var product = result.Value;

        Assert.True(product.CreatedAt >= beforeCreate.UtcDateTime);
        Assert.True(product.CreatedAt <= afterCreate.UtcDateTime);
        Assert.Equal(product.CreatedAt, product.UpdatedAt);
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var price = Money.Create(99.99m).Value;
        var product = Product.Create(
            "PROD-001",
            "Test Product",
            "A test product",
            price,
            TimeProvider).Value;

        product.Deactivate();

        Assert.False(product.IsActive);
    }

    [Fact]
    public void Update_UpdatesProductProperties()
    {
        var price = Money.Create(99.99m).Value;
        var product = Product.Create(
            "PROD-001",
            "Original Name",
            "Original Description",
            price,
            TimeProvider).Value;

        var originalUpdatedAt = product.UpdatedAt;
        var newPrice = Money.Create(149.99m).Value;

        var result = product.Update(
            "Updated Name",
            "Updated Description",
            newPrice,
            TimeProvider);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Name", product.Name);
        Assert.Equal("Updated Description", product.Description);
        Assert.Equal(newPrice, product.Price);
        Assert.True(product.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var price = Money.Create(99.99m).Value;

        var product1 = Product.Create("PROD-001", "Product 1", "Description 1", price, TimeProvider).Value;
        var product2 = Product.Create("PROD-002", "Product 2", "Description 2", price, TimeProvider).Value;

        Assert.NotEqual(product1.Id, product2.Id);
    }

    [Fact]
    public void ProductProperties_AreNotPublicallySettable()
    {
        var price = Money.Create(99.99m).Value;
        var product = Product.Create(
            "PROD-001",
            "Test Product",
            "A test product",
            price,
            TimeProvider).Value;

        var skuProperty = typeof(Product).GetProperty(nameof(Product.Sku));
        var createdAtProperty = typeof(Product).GetProperty(nameof(Product.CreatedAt));

        Assert.Null(skuProperty?.SetMethod);
        Assert.Null(createdAtProperty?.SetMethod);
    }

    [Fact]
    public void Create_WithMultipleValidSkuFormats_AllSucceed()
    {
        var price = Money.Create(99.99m).Value;
        var testCases = new[]
        {
            ("SKU", "Valid 3-char SKU"),
            ("PRODUCT-123", "Valid with hyphen"),
            ("P1-2-3-4", "Valid with multiple hyphens"),
            ("ABCDEFGHIJKLMNOPQRSTUVWXYZ012345", "Valid 32-char max"),
        };

        foreach (var (sku, description) in testCases)
        {
            var result = Product.Create(sku, "Test", description, price, TimeProvider);
            Assert.True(result.IsSuccess, $"Should succeed for: {description}");
        }
    }
}
