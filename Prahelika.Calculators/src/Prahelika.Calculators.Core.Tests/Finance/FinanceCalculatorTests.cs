using Prahelika.Calculators.Core.Finance;

namespace Prahelika.Calculators.Core.Tests.Finance;

public class FinanceCalculatorTests
{
    private readonly FinanceCalculator _calculator = new();

    // Checks that a calculation succeeded and returns its value, so tests stay short.
    private static InterestResult Succeeded(CalcResult<InterestResult> result)
    {
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        return result.Value;
    }

    // ---- Settings ----

    [Fact]
    public void DecimalPlaces_DefaultsToTwo()
    {
        Assert.Equal(2, _calculator.DecimalPlaces);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Constructor_InvalidDecimalPlaces_Throws(int decimalPlaces)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FinanceCalculator(decimalPlaces));
    }

    [Fact]
    public void SimpleInterest_ZeroDecimalPlaces_RoundsHalfAwayFromZero()
    {
        var calculator = new FinanceCalculator(decimalPlaces: 0);

        // 1000 x 5.55% x 1 year = 55.5 exactly, which rounds up to 56.
        var value = Succeeded(calculator.SimpleInterest(1000m, 5.55m, 1m));

        Assert.Equal(56m, value.Interest);
        Assert.Equal(1056m, value.TotalAmount);
    }

    // ---- Simple interest ----

    [Theory]
    [InlineData(1000, 5, 3, 150, 1150)]
    [InlineData(2500, 4.5, 2, 225, 2725)]
    [InlineData(1000, 5, 0.5, 25, 1025)]
    [InlineData(0, 5, 3, 0, 0)]
    [InlineData(1000, 0, 3, 0, 1000)]
    [InlineData(1000, 5, 0, 0, 1000)]
    public void SimpleInterest_ValidInput_ReturnsInterestAndTotal(
        decimal principal, decimal rate, decimal years, decimal expectedInterest, decimal expectedTotal)
    {
        var value = Succeeded(_calculator.SimpleInterest(principal, rate, years));

        Assert.Equal(principal, value.Principal);
        Assert.Equal(expectedInterest, value.Interest);
        Assert.Equal(expectedTotal, value.TotalAmount);
    }

    [Theory]
    [InlineData(-1, 5, 1)]
    [InlineData(1000, -5, 1)]
    [InlineData(1000, 5, -1)]
    public void SimpleInterest_NegativeInput_ReturnsInvalidInput(decimal principal, decimal rate, decimal years)
    {
        var result = _calculator.SimpleInterest(principal, rate, years);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Fact]
    public void SimpleInterest_ResultTooLarge_ReturnsOverflow()
    {
        var result = _calculator.SimpleInterest(decimal.MaxValue, 100m, 100m);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.Overflow, result.Error);
    }

    // ---- Compound interest ----
    // Expected values are worked out by hand, for example 1000 x 1.05^10 = 1628.89.

    [Theory]
    [InlineData(1000, 5, 10, CompoundingFrequency.Annually, 628.89, 1628.89)]
    [InlineData(1000, 12, 1, CompoundingFrequency.Monthly, 126.83, 1126.83)]
    [InlineData(5000, 8, 2, CompoundingFrequency.Quarterly, 858.30, 5858.30)]
    [InlineData(1000, 5, 1, CompoundingFrequency.Daily, 51.27, 1051.27)]
    [InlineData(1000, 6, 2.5, CompoundingFrequency.Annually, 156.82, 1156.82)]   // part of a period
    public void CompoundInterest_ValidInput_ReturnsInterestAndTotal(
        decimal principal, decimal rate, decimal years, CompoundingFrequency frequency,
        decimal expectedInterest, decimal expectedTotal)
    {
        var value = Succeeded(_calculator.CompoundInterest(principal, rate, years, frequency));

        Assert.Equal(principal, value.Principal);
        Assert.Equal(expectedInterest, value.Interest);
        Assert.Equal(expectedTotal, value.TotalAmount);
    }

    [Fact]
    public void CompoundInterest_DefaultFrequency_IsAnnual()
    {
        var value = Succeeded(_calculator.CompoundInterest(1000m, 5m, 10m));

        Assert.Equal(628.89m, value.Interest);
    }

    [Fact]
    public void CompoundInterest_ExactMidpoint_RoundsAwayFromZero()
    {
        // 1000 x (1.025^2 - 1) = 50.625 exactly. Banker's rounding would give 50.62; we expect 50.63.
        var value = Succeeded(_calculator.CompoundInterest(1000m, 5m, 1m, CompoundingFrequency.SemiAnnually));

        Assert.Equal(50.63m, value.Interest);
    }

    [Fact]
    public void CompoundInterest_OneYearAnnually_EqualsSimpleInterest()
    {
        var simple = Succeeded(_calculator.SimpleInterest(1000m, 5m, 1m));
        var compound = Succeeded(_calculator.CompoundInterest(1000m, 5m, 1m, CompoundingFrequency.Annually));

        Assert.Equal(simple.Interest, compound.Interest);
    }

    [Theory]
    [InlineData(0, 5, 3)]
    [InlineData(1000, 0, 3)]
    [InlineData(1000, 5, 0)]
    public void CompoundInterest_NothingToGrow_ReturnsZeroInterest(decimal principal, decimal rate, decimal years)
    {
        var value = Succeeded(_calculator.CompoundInterest(principal, rate, years, CompoundingFrequency.Monthly));

        Assert.Equal(0m, value.Interest);
        Assert.Equal(principal, value.TotalAmount);
    }

    [Fact]
    public void CompoundInterest_ZeroRateWithHugeYears_StillSucceeds()
    {
        var value = Succeeded(_calculator.CompoundInterest(1000m, 0m, 1_000_000_000m, CompoundingFrequency.Daily));

        Assert.Equal(0m, value.Interest);
    }

    [Theory]
    [InlineData(-1, 5, 1)]
    [InlineData(1000, -5, 1)]
    [InlineData(1000, 5, -1)]
    public void CompoundInterest_NegativeInput_ReturnsInvalidInput(decimal principal, decimal rate, decimal years)
    {
        var result = _calculator.CompoundInterest(principal, rate, years);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Fact]
    public void CompoundInterest_UndefinedFrequency_ReturnsInvalidInput()
    {
        var result = _calculator.CompoundInterest(1000m, 5m, 1m, (CompoundingFrequency)99);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Fact]
    public void CompoundInterest_ResultTooLarge_ReturnsOverflow()
    {
        // Doubling a billion 200 times is far beyond what decimal can hold.
        var result = _calculator.CompoundInterest(1_000_000_000m, 100m, 200m, CompoundingFrequency.Annually);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.Overflow, result.Error);
    }

    [Fact]
    public void CompoundInterest_TotalIsPrincipalPlusInterest()
    {
        var value = Succeeded(_calculator.CompoundInterest(2500m, 7.5m, 3m, CompoundingFrequency.Monthly));

        Assert.Equal(value.Principal + value.Interest, value.TotalAmount);
    }
}