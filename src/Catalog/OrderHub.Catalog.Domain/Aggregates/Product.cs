using OrderHub.Catalog.Domain.Common;
using OrderHub.Catalog.Domain.ValueObjects;

namespace OrderHub.Catalog.Domain.Aggregates;

/// <summary>
/// Represents a product in the catalog. This is an aggregate root that enforces invariants
/// for product properties such as SKU format and price validity.
/// </summary>
public sealed class Product
{
    private Product(
        Guid id,
        string sku,
        string name,
        string description,
        Money price,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        Sku = sku;
        Name = name;
        Description = description;
        Price = price;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }
    public string Sku { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Money Price { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Creates a new Product with the given properties.
    /// </summary>
    /// <param name="sku">The product SKU (3-32 alphanumeric characters plus hyphen).</param>
    /// <param name="name">The product name.</param>
    /// <param name="description">The product description.</param>
    /// <param name="price">The product price in BRL.</param>
    /// <param name="timeProvider">The time provider for generating timestamps.</param>
    /// <returns>A Result containing the created Product, or an error if any invariant is violated.</returns>
    public static Result<Product> Create(
        string sku,
        string name,
        string description,
        Money price,
        TimeProvider timeProvider)
    {
        var skuValidation = ValidateSku(sku);
        if (skuValidation.IsFailure)
        {
            return Result.Fail<Product>(skuValidation.Error);
        }

        var id = Guid.CreateVersion7();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var product = new Product(
            id,
            sku,
            name,
            description,
            price,
            isActive: true,
            createdAt: now,
            updatedAt: now);

        return Result.Ok(product);
    }

    /// <summary>
    /// Deactivates the product, marking it as inactive without deleting it.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Updates the product properties.
    /// </summary>
    /// <param name="name">The new product name.</param>
    /// <param name="description">The new product description.</param>
    /// <param name="price">The new product price.</param>
    /// <param name="timeProvider">The time provider for updating the timestamp.</param>
    /// <returns>A Result indicating success or a validation error.</returns>
    public Result Update(
        string name,
        string description,
        Money price,
        TimeProvider timeProvider)
    {
        Name = name;
        Description = description;
        Price = price;
        UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;

        return Result.Success();
    }

    private static Result ValidateSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return Result.Failure(Error.Validation(
                nameof(Sku),
                "SKU cannot be empty."));
        }

        if (sku.Length < 3 || sku.Length > 32)
        {
            return Result.Failure(Error.Validation(
                nameof(Sku),
                "SKU must be between 3 and 32 characters."));
        }

        if (!IsValidSkuFormat(sku))
        {
            return Result.Failure(Error.Validation(
                nameof(Sku),
                "SKU must contain only alphanumeric characters and hyphens."));
        }

        return Result.Success();
    }

    private static bool IsValidSkuFormat(string sku)
    {
        return sku.All(c => char.IsLetterOrDigit(c) || c == '-');
    }
}
