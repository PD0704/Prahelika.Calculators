using Prahelika.Calculators.Core.Basic;

namespace Prahelika.Calculators.Core.Tests.Basic;

public class BasicCalculatorTests
{
    private readonly BasicCalculator _calculator = new();

    // [Fact]: a single test with fixed values, written in full Arrange-Act-Assert style.
    [Fact]
    public void Add_TwoPositiveNumbers_ReturnsSum()
    {
        // Arrange
        decimal a = 2m;
        decimal b = 3m;

        // Act
        var result = _calculator.Add(a, b);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5m, result.Value);
    }

    // [Theory]: the same test runs once per [InlineData] line.
    [Theory]
    [InlineData(2, 3, 15)]
    [InlineData(-2, -3, -5)]
    [InlineData(0, 0, 0)]
    [InlineData(1.5, 2.5, 4)]
    public void Add_VariousNumbers_ReturnsSum(decimal a, decimal b, decimal expected)
    {
        var result = _calculator.Add(a, b);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData(10, 4, 6)]
    [InlineData(4, 10, -6)]
    [InlineData(0, 5, -5)]
    public void Subtract_VariousNumbers_ReturnsDifference(decimal a, decimal b, decimal expected)
    {
        var result = _calculator.Subtract(a, b);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData(3, 4, 12)]
    [InlineData(-3, 4, -12)]
    [InlineData(-3, -4, 12)]
    [InlineData(5, 0, 0)]
    public void Multiply_VariousNumbers_ReturnsProduct(decimal a, decimal b, decimal expected)
    {
        var result = _calculator.Multiply(a, b);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData(10, 2, 5)]
    [InlineData(7, 2, 3.5)]
    [InlineData(-9, 3, -3)]
    public void Divide_ValidNumbers_ReturnsQuotient(decimal a, decimal b, decimal expected)
    {
        var result = _calculator.Divide(a, b);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData(10, 3, 1)]
    [InlineData(9, 3, 0)]
    [InlineData(-7, 3, -1)]
    public void Modulo_ValidNumbers_ReturnsRemainder(decimal a, decimal b, decimal expected)
    {
        var result = _calculator.Modulo(a, b);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData(150, 20, 30)]
    [InlineData(200, 0, 0)]
    [InlineData(80, 12.5, 10)]
    public void Percentage_ValidNumbers_ReturnsPartOfValue(decimal value, decimal percent, decimal expected)
    {
        var result = _calculator.Percentage(value, percent);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }
}