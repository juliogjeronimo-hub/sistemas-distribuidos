using OrderHub.Catalog.Domain.ValueObjects;
using Xunit;

namespace OrderHub.Catalog.Domain.UnitTests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidAmount_ReturnsSuccess()
    {
        var result = Money.Create(10.50m);

        Assert.True(result.IsSuccess);
        Assert.Equal(10.50m, result.Value.Amount);
    }

    [Fact]
    public void Create_WithZeroAmount_ReturnsFailure()
    {
        var result = Money.Create(0m);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Money", result.Error.Code);
        Assert.Contains("greater than zero", result.Error.Message);
    }

    [Fact]
    public void Create_WithNegativeAmount_ReturnsFailure()
    {
        var result = Money.Create(-5.25m);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Money", result.Error.Code);
    }

    [Fact]
    public void Currency_IsAlwaysBRL()
    {
        Assert.Equal("BRL", Money.Currency);
    }

    [Fact]
    public void Money_ImplementsValueObjectEquality()
    {
        var money1 = Money.Create(10.50m).Value;
        var money2 = Money.Create(10.50m).Value;

        Assert.Equal(money1, money2);
    }

    [Fact]
    public void Money_DifferentAmounts_AreNotEqual()
    {
        var money1 = Money.Create(10.50m).Value;
        var money2 = Money.Create(10.51m).Value;

        Assert.NotEqual(money1, money2);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var money = Money.Create(99.99m).Value;

        Assert.Equal("99.99 BRL", money.ToString());
    }
}
