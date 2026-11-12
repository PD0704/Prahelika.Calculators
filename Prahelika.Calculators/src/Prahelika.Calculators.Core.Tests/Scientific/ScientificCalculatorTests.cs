using Prahelika.Calculators.Core.Scientific;

namespace Prahelika.Calculators.Core.Tests.Scientific;

public class ScientificCalculatorTests
{
    private const int Precision = 10;   // compare doubles to 10 decimal places

    private readonly ScientificCalculator _radians = new();
    private readonly ScientificCalculator _degrees = new(AngleUnit.Degrees);

    // ---- Angle unit ----

    [Fact]
    public void AngleUnit_DefaultsToRadians()
    {
        Assert.Equal(AngleUnit.Radians, _radians.AngleUnit);
    }

    [Fact]
    public void AngleUnit_CanBeSetToDegrees()
    {
        Assert.Equal(AngleUnit.Degrees, _degrees.AngleUnit);
    }

    // ---- Constants ----

    [Fact]
    public void Constants_HaveCorrectValues()
    {
        Assert.Equal(3.141592653589793, ScientificConstants.Pi, Precision);
        Assert.Equal(2.718281828459045, ScientificConstants.E, Precision);
    }

    // ---- Power ----

    [Theory]
    [InlineData(2, 10, 1024)]
    [InlineData(9, 0.5, 3)]
    [InlineData(5, 0, 1)]
    [InlineData(2, -2, 0.25)]
    public void Power_ValidInput_ReturnsResult(double value, double exponent, double expected)
    {
        var result = _radians.Power(value, exponent);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value, Precision);
    }

    [Fact]
    public void Power_ZeroToNegativeExponent_ReturnsDivisionByZero()
    {
        var result = _radians.Power(0, -1);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.DivisionByZero, result.Error);
    }

    [Fact]
    public void Power_NegativeBaseFractionalExponent_ReturnsInvalidInput()
    {
        var result = _radians.Power(-8, 0.5);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Fact]
    public void Power_ResultTooLarge_ReturnsOverflow()
    {
        var result = _radians.Power(10, 400);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.Overflow, result.Error);
    }

    // ---- Square root ----

    [Theory]
    [InlineData(144, 12)]
    [InlineData(0, 0)]
    [InlineData(2, 1.4142135623730951)]
    public void SquareRoot_ValidInput_ReturnsResult(double value, double expected)
    {
        var result = _radians.SquareRoot(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value, Precision);
    }

    [Fact]
    public void SquareRoot_NegativeNumber_ReturnsInvalidInput()
    {
        var result = _radians.SquareRoot(-4);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    // ---- Nth root ----

    [Theory]
    [InlineData(27, 3, 3)]
    [InlineData(16, 4, 2)]
    [InlineData(32, 5, 2)]
    [InlineData(-8, 3, -2)]     // odd root of a negative number is allowed
    public void NthRoot_ValidInput_ReturnsResult(double value, int degree, double expected)
    {
        var result = _radians.NthRoot(value, degree);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value, Precision);
    }

    [Fact]
    public void NthRoot_DegreeZero_ReturnsInvalidInput()
    {
        var result = _radians.NthRoot(8, 0);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Fact]
    public void NthRoot_EvenRootOfNegative_ReturnsInvalidInput()
    {
        var result = _radians.NthRoot(-16, 4);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Fact]
    public void NthRoot_ZeroWithNegativeDegree_ReturnsDivisionByZero()
    {
        var result = _radians.NthRoot(0, -2);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.DivisionByZero, result.Error);
    }

    // ---- Logarithms ----

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2.718281828459045, 1)]
    public void NaturalLog_ValidInput_ReturnsResult(double value, double expected)
    {
        var result = _radians.NaturalLog(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value, Precision);
    }

    [Theory]
    [InlineData(1000, 3)]
    [InlineData(1, 0)]
    [InlineData(0.01, -2)]
    public void Log10_ValidInput_ReturnsResult(double value, double expected)
    {
        var result = _radians.Log10(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value, Precision);
    }

    [Theory]
    [InlineData(8, 2, 3)]
    [InlineData(81, 3, 4)]
    public void Log_ValidInput_ReturnsResult(double value, double newBase, double expected)
    {
        var result = _radians.Log(value, newBase);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value, Precision);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void NaturalLog_ZeroOrNegative_ReturnsInvalidInput(double value)
    {
        var result = _radians.NaturalLog(value);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Log10_ZeroOrNegative_ReturnsInvalidInput(double value)
    {
        var result = _radians.Log10(value);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Theory]
    [InlineData(8, 1)]      // base 1 is not allowed
    [InlineData(8, 0)]
    [InlineData(8, -2)]
    [InlineData(-1, 2)]
    [InlineData(0, 2)]
    public void Log_InvalidValueOrBase_ReturnsInvalidInput(double value, double newBase)
    {
        var result = _radians.Log(value, newBase);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    // ---- Trigonometry in degrees ----

    [Theory]
    [InlineData(0, 0)]
    [InlineData(30, 0.5)]
    [InlineData(90, 1)]
    [InlineData(180, 0)]
    [InlineData(270, -1)]
    public void Sin_Degrees_ReturnsKnownValues(double angle, double expected)
    {
        var result = _degrees.Sin(angle);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value, Precision);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(60, 0.5)]
    [InlineData(90, 0)]
    [InlineData(180, -1)]
    public void Cos_Degrees_ReturnsKnownValues(double angle, double expected)
    {
        var result = _degrees.Cos(angle);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value, Precision);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(45, 1)]
    [InlineData(135, -1)]
    public void Tan_Degrees_ReturnsKnownValues(double angle, double expected)
    {
        var result = _degrees.Tan(angle);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value, Precision);
    }

    [Theory]
    [InlineData(90)]
    [InlineData(270)]
    [InlineData(-90)]
    [InlineData(450)]
    public void Tan_Degrees_AtUndefinedAngles_ReturnsInvalidInput(double angle)
    {
        var result = _degrees.Tan(angle);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    // ---- Trigonometry in radians ----

    [Fact]
    public void Sin_Radians_HalfPi_IsOne()
    {
        var result = _radians.Sin(Math.PI / 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(1d, result.Value, Precision);
    }

    [Fact]
    public void Sin_Radians_Pi_IsExactlyZero()
    {
        // In raw floating point sin(pi) is 1.2e-16. The calculator rounds that to 0.
        var result = _radians.Sin(Math.PI);

        Assert.True(result.IsSuccess);
        Assert.Equal(0d, result.Value);
    }

    [Fact]
    public void Cos_Radians_Zero_IsOne()
    {
        var result = _radians.Cos(0);

        Assert.True(result.IsSuccess);
        Assert.Equal(1d, result.Value, Precision);
    }

    [Fact]
    public void Tan_Radians_QuarterPi_IsOne()
    {
        var result = _radians.Tan(Math.PI / 4);

        Assert.True(result.IsSuccess);
        Assert.Equal(1d, result.Value, Precision);
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void Sin_NonFiniteAngle_ReturnsInvalidInput(double angle)
    {
        var result = _radians.Sin(angle);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    // ---- Inverse trigonometry ----

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0.5, 30)]
    [InlineData(1, 90)]
    [InlineData(-1, -90)]
    public void Asin_Degrees_ReturnsKnownValues(double value, double expected)
    {
        var result = _degrees.Asin(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value, Precision);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0.5, 60)]
    [InlineData(0, 90)]
    [InlineData(-1, 180)]
    public void Acos_Degrees_ReturnsKnownValues(double value, double expected)
    {
        var result = _degrees.Acos(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value, Precision);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 45)]
    [InlineData(-1, -45)]
    public void Atan_Degrees_ReturnsKnownValues(double value, double expected)
    {
        var result = _degrees.Atan(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value, Precision);
    }

    [Fact]
    public void Asin_Radians_One_IsHalfPi()
    {
        var result = _radians.Asin(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(Math.PI / 2, result.Value, Precision);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1.5)]
    public void Asin_OutOfRange_ReturnsInvalidInput(double value)
    {
        var result = _radians.Asin(value);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1.5)]
    public void Acos_OutOfRange_ReturnsInvalidInput(double value)
    {
        var result = _radians.Acos(value);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    // ---- Factorial ----

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(5, 120)]
    [InlineData(10, 3628800)]
    public void Factorial_ValidInput_ReturnsResult(int n, double expected)
    {
        var result = _radians.Factorial(n);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Factorial_Negative_ReturnsInvalidInput()
    {
        var result = _radians.Factorial(-1);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Fact]
    public void Factorial_170_StillSucceeds()
    {
        // Boundary check: 170! is the largest factorial a double can hold.
        var result = _radians.Factorial(170);

        Assert.True(result.IsSuccess);
        Assert.False(double.IsInfinity(result.Value));
    }

    [Fact]
    public void Factorial_171_ReturnsOverflow()
    {
        var result = _radians.Factorial(171);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.Overflow, result.Error);
    }
}