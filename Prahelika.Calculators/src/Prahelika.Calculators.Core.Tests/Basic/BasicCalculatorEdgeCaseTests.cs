using Prahelika.Calculators.Core.Basic;

namespace Prahelika.Calculators.Core.Tests.Basic;

public class BasicCalculatorEdgeCaseTests
{
    private readonly BasicCalculator _calculator = new();

    // ---- Division by zero ----

    [Theory]
    [InlineData(10, 0)]
    [InlineData(0, 0)]
    [InlineData(-5, 0)]
    public void Divide_ByZero_ReturnsDivisionByZeroError(decimal a, decimal b)
    {
        var result = _calculator.Divide(a, b);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.DivisionByZero, result.Error);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Theory]
    [InlineData(10, 0)]
    [InlineData(0, 0)]
    [InlineData(-5, 0)]
    public void Modulo_ByZero_ReturnsDivisionByZeroError(decimal a, decimal b)
    {
        var result = _calculator.Modulo(a, b);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.DivisionByZero, result.Error);
    }

    // ---- Overflow ----
    // decimal.MaxValue can't go inside [InlineData] (it isn't a compile-time constant),
    // so these are separate [Fact] tests.

    [Fact]
    public void Add_ResultTooLarge_ReturnsOverflowError()
    {
        var result = _calculator.Add(decimal.MaxValue, decimal.MaxValue);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.Overflow, result.Error);
    }

    [Fact]
    public void Subtract_ResultTooSmall_ReturnsOverflowError()
    {
        var result = _calculator.Subtract(decimal.MinValue, decimal.MaxValue);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.Overflow, result.Error);
    }

    [Fact]
    public void Multiply_ResultTooLarge_ReturnsOverflowError()
    {
        var result = _calculator.Multiply(decimal.MaxValue, 2m);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.Overflow, result.Error);
    }

    [Fact]
    public void Percentage_ResultTooLarge_ReturnsOverflowError()
    {
        var result = _calculator.Percentage(decimal.MaxValue, 200m);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.Overflow, result.Error);
    }

    [Fact]
    public void Add_MaxValuePlusZero_StillSucceeds()
    {
        // Boundary check: the largest legal value must NOT be treated as an overflow.
        var result = _calculator.Add(decimal.MaxValue, 0m);

        Assert.True(result.IsSuccess);
        Assert.Equal(decimal.MaxValue, result.Value);
    }

    // ---- Rounding and precision ----

    [Fact]
    public void Add_PointOnePlusPointTwo_IsExactlyPointThree()
    {
        // With double, 0.1 + 0.2 gives 0.30000000000000004. decimal is exact here.
        var result = _calculator.Add(0.1m, 0.2m);

        Assert.True(result.IsSuccess);
        Assert.Equal(0.3m, result.Value);
    }

    [Fact]
    public void Divide_OneByThree_ReturnsDecimalWithinItsPrecision()
    {
        var result = _calculator.Divide(1m, 3m);

        Assert.True(result.IsSuccess);
        Assert.Equal(0.3333333333333333333333333333m, result.Value);
    }

    [Fact]
    public void Multiply_SmallDecimals_KeepsPrecision()
    {
        var result = _calculator.Multiply(0.001m, 0.002m);

        Assert.True(result.IsSuccess);
        Assert.Equal(0.000002m, result.Value);
    }

    // ---- Negative numbers ----

    [Theory]
    [InlineData(-10, 4, -2.5)]
    [InlineData(10, -4, -2.5)]
    [InlineData(-10, -4, 2.5)]
    public void Divide_MixedSigns_ReturnsCorrectSign(decimal a, decimal b, decimal expected)
    {
        var result = _calculator.Divide(a, b);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData(-150, 20, -30)]
    [InlineData(150, -20, -30)]
    [InlineData(-150, -20, 30)]
    public void Percentage_NegativeInputs_ReturnsCorrectSign(decimal value, decimal percent, decimal expected)
    {
        var result = _calculator.Percentage(value, percent);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    // ---- Zero ----

    [Fact]
    public void Divide_ZeroByNumber_ReturnsZero()
    {
        var result = _calculator.Divide(0m, 7m);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value);
    }
}