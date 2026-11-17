using OrderHub.Catalog.Domain.Common;

namespace OrderHub.Catalog.Domain.ValueObjects;

/// <summary>
/// Represents a monetary value in BRL currency.
/// This is an immutable value object that enforces the constraint that the amount must be positive.
/// </summary>
public sealed record Money
{
    public const string Currency = "BRL";

    private Money(decimal amount)
    {
        Amount = amount;
    }

    public decimal Amount { get; }

    /// <summary>
    /// Creates a Money value object with the given amount in BRL.
    /// </summary>
    /// <param name="amount">The amount in BRL currency. Must be greater than zero.</param>
    /// <returns>A Result containing the Money value object, or an error if the amount is invalid.</returns>
    public static Result<Money> Create(decimal amount)
    {
        if (amount <= 0)
        {
            return Result.Fail<Money>(Error.Validation(
                nameof(Money),
                $"Amount must be greater than zero. Got: {amount}"));
        }

        return Result.Ok(new Money(amount));
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}
