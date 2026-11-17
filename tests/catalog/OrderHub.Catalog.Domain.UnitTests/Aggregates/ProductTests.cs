using OrderHub.Catalog.Domain.Aggregates;
using OrderHub.Catalog.Domain.ValueObjects;
using Xunit;

namespace OrderHub.Catalog.Domain.UnitTests.Aggregates;

public class ProductTests
{
    private static readonly TimeProvider TimeProvider = TimeProvider.System;

    #region SKU Validation Invariants

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
        Assert.Equal("Validation.Sku", result.Error.Code);
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
        Assert.Equal("Validation.Sku", result.Error.Code);
        Assert.Contains("between 3 and 32", result.Error.Message);
    }

    [Fact]
    public void Create_WithSkuValidLength_Succeeds()
    {
        var price = Money.Create(99.99m).Value;
        var validSkus = new[] { "ABC", "PROD-001", "P1", "123", "ABCDEFGHIJKLMNOPQRSTUVWXYZ012345" };

        foreach (var sku in validSkus)
        {
            if (sku.Length >= 3 && sku.Length <= 32)
            {
                var result = Product.Create(sku, "Test", "Description", price, TimeProvider);
                Assert.True(result.IsSuccess, $"SKU '{sku}' (length {sku.Length}) should be valid");
            }
        }
    }

    [Fact]
    public void Create_WithSkuContainingInvalidCharacters_ReturnsFailure()
    {
        var price = Money.Create(99.99m).Value;
        var invalidSkus = new[] { "PROD@001", "SKU#123", "PROD 001", "SKU.NET", "PRÉ-001", "PROD/001" };

        foreach (var sku in invalidSkus)
        {
            var result = Product.Create(sku, "Test Product", "A test product", price, TimeProvider);
            Assert.True(result.IsFailure, $"SKU '{sku}' should fail validation");
            Assert.Equal("Validation.Sku", result.Error.Code);
            Assert.Contains("alphanumeric characters and hyphens", result.Error.Message);
        }
    }

    [Fact]
    public void Create_WithSkuEmpty_ReturnsFailure()
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
        Assert.Contains("cannot be empty", result.Error.Message);
    }

    [Fact]
    public void Create_WithSkuNull_ReturnsFailure()
    {
        var price = Money.Create(99.99m).Value;

        var result = Product.Create(
            null!,
            "Test Product",
            "A test product",
            price,
            TimeProvider);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Sku", result.Error.Code);
    }

    [Fact]
    public void Create_WithSkuWhitespace_ReturnsFailure()
    {
        var price = Money.Create(99.99m).Value;

        var result = Product.Create(
            "   ",
            "Test Product",
            "A test product",
            price,
            TimeProvider);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Sku", result.Error.Code);
    }

    [Fact]
    public void Create_WithSkuOnlyAlphanumericAndHyphens_Succeeds()
    {
        var price = Money.Create(99.99m).Value;
        var validSkus = new[] { "ABC", "PROD-001", "P-123", "1-2-3", "ABC123DEF", "a1b2-c3d" };

        foreach (var sku in validSkus)
        {
            var result = Product.Create(sku, "Test", "Description", price, TimeProvider);
            Assert.True(result.IsSuccess, $"SKU '{sku}' should be valid");
        }
    }

    [Fact]
    public void Create_WithSkuBoundaryLength3_Succeeds()
    {
        var price = Money.Create(99.99m).Value;

        var result = Product.Create("ABC", "Test", "Description", price, TimeProvider);

        Assert.True(result.IsSuccess);
        Assert.Equal("ABC", result.Value.Sku);
    }

    [Fact]
    public void Create_WithSkuBoundaryLength32_Succeeds()
    {
        var price = Money.Create(99.99m).Value;
        var sku32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ012345";

        var result = Product.Create(sku32, "Test", "Description", price, TimeProvider);

        Assert.True(result.IsSuccess);
        Assert.Equal(sku32, result.Value.Sku);
    }

    #endregion

    #region Price Validation Invariants

    [Fact]
    public void Create_WithPriceGreaterThanZero_Succeeds()
    {
        var price = Money.Create(0.01m).Value;

        var result = Product.Create(
            "PROD-001",
            "Test Product",
            "A test product",
            price,
            TimeProvider);

        Assert.True(result.IsSuccess);
        Assert.Equal(0.01m, result.Value.Price.Amount);
    }

    [Fact]
    public void Create_WithPriceZero_FailsAtMoneyLevel()
    {
        var priceResult = Money.Create(0m);

        Assert.True(priceResult.IsFailure);
        Assert.Equal("Validation.Money", priceResult.Error.Code);
    }

    [Fact]
    public void Create_WithNegativePrice_FailsAtMoneyLevel()
    {
        var priceResult = Money.Create(-10.50m);

        Assert.True(priceResult.IsFailure);
        Assert.Equal("Validation.Money", priceResult.Error.Code);
    }

    [Fact]
    public void Create_WithLargeValidPrice_Succeeds()
    {
        var price = Money.Create(99999.99m).Value;

        var result = Product.Create(
            "PROD-001",
            "Test Product",
            "A test product",
            price,
            TimeProvider);

        Assert.True(result.IsSuccess);
        Assert.Equal(99999.99m, result.Value.Price.Amount);
    }

    [Fact]
    public void Create_WithSmallValidPrice_Succeeds()
    {
        var price = Money.Create(0.01m).Value;

        var result = Product.Create(
            "PROD-001",
            "Test Product",
            "A test product",
            price,
            TimeProvider);

        Assert.True(result.IsSuccess);
        Assert.Equal(0.01m, result.Value.Price.Amount);
    }

    #endregion

    #region Product Creation

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
    }

    [Fact]
    public void Create_WithInvalidSku_ReturnsFailure()
    {
        var price = Money.Create(99.99m).Value;

        var result = Product.Create(
            "INVALID@SKU",
            "Test Product",
            "A test product",
            price,
            TimeProvider);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_GeneratesNonEmptyGuidV7Id()
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
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var price = Money.Create(99.99m).Value;

        var product1 = Product.Create("PROD-001", "Product 1", "Description 1", price, TimeProvider).Value;
        var product2 = Product.Create("PROD-002", "Product 2", "Description 2", price, TimeProvider).Value;
        var product3 = Product.Create("PROD-003", "Product 3", "Description 3", price, TimeProvider).Value;

        Assert.NotEqual(product1.Id, product2.Id);
        Assert.NotEqual(product2.Id, product3.Id);
        Assert.NotEqual(product1.Id, product3.Id);
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
    public void Create_SetsCorrectProductProperties()
    {
        var price = Money.Create(99.99m).Value;

        var result = Product.Create(
            "PROD-001",
            "Test Product",
            "A test product",
            price,
            TimeProvider);

        var product = result.Value;
        Assert.Equal("PROD-001", product.Sku);
        Assert.Equal("Test Product", product.Name);
        Assert.Equal("A test product", product.Description);
        Assert.Equal(price, product.Price);
    }

    #endregion

    #region Deactivation

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

        Assert.True(product.IsActive);

        product.Deactivate();

        Assert.False(product.IsActive);
    }

    [Fact]
    public void Deactivate_CanDeactivateActiveProduct()
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
    public void Deactivate_AlreadyDeactivatedProduct_RemainsInactive()
    {
        var price = Money.Create(99.99m).Value;
        var product = Product.Create(
            "PROD-001",
            "Test Product",
            "A test product",
            price,
            TimeProvider).Value;

        product.Deactivate();
        var firstDeactivate = product.IsActive;
        product.Deactivate();
        var secondDeactivate = product.IsActive;

        Assert.False(firstDeactivate);
        Assert.False(secondDeactivate);
    }

    #endregion

    #region Money Value Object

    [Fact]
    public void Money_Create_WithValidAmount_Succeeds()
    {
        var result = Money.Create(99.99m);

        Assert.True(result.IsSuccess);
        Assert.Equal(99.99m, result.Value.Amount);
    }

    [Fact]
    public void Money_Create_WithZero_Fails()
    {
        var result = Money.Create(0m);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Money", result.Error.Code);
    }

    [Fact]
    public void Money_Create_WithNegative_Fails()
    {
        var result = Money.Create(-10.50m);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Money", result.Error.Code);
    }

    [Fact]
    public void Money_CurrencyIsAlwaysBRL()
    {
        Assert.Equal("BRL", Money.Currency);
    }

    [Fact]
    public void Money_EqualityByValue()
    {
        var money1 = Money.Create(99.99m).Value;
        var money2 = Money.Create(99.99m).Value;

        Assert.Equal(money1, money2);
    }

    [Fact]
    public void Money_DifferentAmounts_AreNotEqual()
    {
        var money1 = Money.Create(99.99m).Value;
        var money2 = Money.Create(99.98m).Value;

        Assert.NotEqual(money1, money2);
    }

    [Fact]
    public void Money_IsValueObject_RecordSemantics()
    {
        var money1 = Money.Create(50.00m).Value;
        var money2 = Money.Create(50.00m).Value;

        Assert.Equal(money1, money2);
        Assert.True(money1.Equals(money2));
    }

    #endregion

    #region Update Invariants

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
    }

    [Fact]
    public void Update_UpdatesUpdatedAtTimestamp()
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
        Assert.True(product.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void Update_DoesNotChangeCreatedAt()
    {
        var price = Money.Create(99.99m).Value;
        var product = Product.Create(
            "PROD-001",
            "Original Name",
            "Original Description",
            price,
            TimeProvider).Value;

        var originalCreatedAt = product.CreatedAt;
        var newPrice = Money.Create(149.99m).Value;

        product.Update(
            "Updated Name",
            "Updated Description",
            newPrice,
            TimeProvider);

        Assert.Equal(originalCreatedAt, product.CreatedAt);
    }

    [Fact]
    public void Update_DoesNotChangeId()
    {
        var price = Money.Create(99.99m).Value;
        var product = Product.Create(
            "PROD-001",
            "Original Name",
            "Original Description",
            price,
            TimeProvider).Value;

        var originalId = product.Id;
        var newPrice = Money.Create(149.99m).Value;

        product.Update(
            "Updated Name",
            "Updated Description",
            newPrice,
            TimeProvider);

        Assert.Equal(originalId, product.Id);
    }

    [Fact]
    public void Update_DoesNotChangeSku()
    {
        var price = Money.Create(99.99m).Value;
        var product = Product.Create(
            "PROD-001",
            "Original Name",
            "Original Description",
            price,
            TimeProvider).Value;

        var originalSku = product.Sku;
        var newPrice = Money.Create(149.99m).Value;

        product.Update(
            "Updated Name",
            "Updated Description",
            newPrice,
            TimeProvider);

        Assert.Equal(originalSku, product.Sku);
    }

    #endregion

    #region Property Encapsulation

    [Fact]
    public void ProductId_HasNoPublicSetter()
    {
        var property = typeof(Product).GetProperty(nameof(Product.Id));
        Assert.Null(property?.SetMethod);
    }

    [Fact]
    public void ProductSku_HasNoPublicSetter()
    {
        var property = typeof(Product).GetProperty(nameof(Product.Sku));
        Assert.Null(property?.SetMethod);
    }

    [Fact]
    public void ProductCreatedAt_HasNoPublicSetter()
    {
        var property = typeof(Product).GetProperty(nameof(Product.CreatedAt));
        Assert.Null(property?.SetMethod);
    }

    [Fact]
    public void ProductIsActive_HasPrivateSetter()
    {
        var property = typeof(Product).GetProperty(nameof(Product.IsActive));
        Assert.NotNull(property?.SetMethod);
        Assert.True(property!.SetMethod!.IsPrivate);
    }

    [Fact]
    public void ProductName_HasPrivateSetter()
    {
        var property = typeof(Product).GetProperty(nameof(Product.Name));
        Assert.NotNull(property?.SetMethod);
        Assert.True(property!.SetMethod!.IsPrivate);
    }

    #endregion
}
