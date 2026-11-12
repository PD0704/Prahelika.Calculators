namespace Prahelika.Calculators.Core.Scientific;

/// <summary>Default implementation of <see cref="IScientificCalculator"/>.</summary>
public sealed class ScientificCalculator : IScientificCalculator
{
    // In floating point, sin(pi) is 1.2e-16 instead of 0. Trig results closer to zero
    // than this are treated as exactly zero, which is what users expect to see.
    private const double ZeroTolerance = 1e-12;

    /// <summary>Creates a calculator that measures angles in the given unit (radians by default).</summary>
    public ScientificCalculator(AngleUnit angleUnit = AngleUnit.Radians)
    {
        AngleUnit = angleUnit;
    }
    
    /// <inheritdoc />
    public AngleUnit AngleUnit { get; }

    /// <inheritdoc />
    public CalcResult<double> Power(double value, double exponent)
    {
        if (value == 0d && exponent < 0d)
        {
            return CalcResult<double>.Failure(CalcError.DivisionByZero, "Zero cannot be raised to a negative power.");
        }

        return Finish(Math.Pow(value, exponent), "A negative number cannot be raised to a fractional power.");
    }

    /// <inheritdoc />
    public CalcResult<double> SquareRoot(double value)
    {
        if (value < 0d)
        {
            return CalcResult<double>.Failure(CalcError.InvalidInput, "Cannot take the square root of a negative number.");
        }

        return Finish(Math.Sqrt(value), "The input must be a valid number.");
    }

    /// <inheritdoc />
    public CalcResult<double> NthRoot(double value, int degree)
    {
        if (degree == 0)
        {
            return CalcResult<double>.Failure(CalcError.InvalidInput, "The root degree cannot be zero.");
        }

        if (value == 0d && degree < 0)
        {
            return CalcResult<double>.Failure(CalcError.DivisionByZero, "Zero has no negative-degree root.");
        }

        if (value < 0d)
        {
            if (degree % 2 == 0)
            {
                return CalcResult<double>.Failure(CalcError.InvalidInput, "An even root of a negative number is not a real number.");
            }

            // Odd roots of negative numbers exist: the cube root of -8 is -2.
            return Finish(-Math.Pow(-value, 1d / degree), "The input must be a valid number.");
        }

        return Finish(Math.Pow(value, 1d / degree), "The input must be a valid number.");
    }

    /// <inheritdoc />
    public CalcResult<double> NaturalLog(double value)
    {
        if (value <= 0d)
        {
            return CalcResult<double>.Failure(CalcError.InvalidInput, "A logarithm is only defined for positive numbers.");
        }

        return Finish(Math.Log(value), "The input must be a valid number.");
    }

    /// <inheritdoc />
    public CalcResult<double> Log10(double value)
    {
        if (value <= 0d)
        {
            return CalcResult<double>.Failure(CalcError.InvalidInput, "A logarithm is only defined for positive numbers.");
        }

        return Finish(Math.Log10(value), "The input must be a valid number.");
    }

    /// <inheritdoc />
    public CalcResult<double> Log(double value, double newBase)
    {
        if (value <= 0d || newBase <= 0d || newBase == 1d)
        {
            return CalcResult<double>.Failure(
                CalcError.InvalidInput,
                "The value and base must be positive, and the base cannot be 1.");
        }

        return Finish(Math.Log(value, newBase), "The input must be a valid number.");
    }

    /// <inheritdoc />
    public CalcResult<double> Sin(double angle) => Trig(Math.Sin(ToRadians(angle)));

    /// <inheritdoc />
    public CalcResult<double> Cos(double angle) => Trig(Math.Cos(ToRadians(angle)));

    /// <inheritdoc />
    public CalcResult<double> Tan(double angle)
    {
        if (AngleUnit == AngleUnit.Degrees && Math.Abs(angle % 180d) == 90d)
        {
            return CalcResult<double>.Failure(
                CalcError.InvalidInput,
                "The tangent is undefined at 90 degrees plus any multiple of 180 degrees.");
        }

        return Trig(Math.Tan(ToRadians(angle)));
    }

    /// <inheritdoc />
    public CalcResult<double> Asin(double value)
    {
        if (value < -1d || value > 1d)
        {
            return CalcResult<double>.Failure(CalcError.InvalidInput, "The input must be between -1 and 1.");
        }

        return Finish(FromRadians(Math.Asin(value)), "The input must be a valid number.");
    }

    /// <inheritdoc />
    public CalcResult<double> Acos(double value)
    {
        if (value < -1d || value > 1d)
        {
            return CalcResult<double>.Failure(CalcError.InvalidInput, "The input must be between -1 and 1.");
        }

        return Finish(FromRadians(Math.Acos(value)), "The input must be a valid number.");
    }

    /// <inheritdoc />
    public CalcResult<double> Atan(double value) =>
        Finish(FromRadians(Math.Atan(value)), "The input must be a valid number.");

    private double ToRadians(double angle) =>
        AngleUnit == AngleUnit.Degrees ? angle * Math.PI / 180d : angle;

    private double FromRadians(double radians) =>
        AngleUnit == AngleUnit.Degrees ? radians * 180d / Math.PI : radians;

    private static CalcResult<double> Trig(double result) =>
        Finish(Math.Abs(result) < ZeroTolerance ? 0d : result, "The angle must be a finite number.");

    // .NET math functions return NaN or Infinity instead of throwing.
    // This converts those silent failures into proper CalcResult errors.
    private static CalcResult<double> Finish(double result, string invalidInputMessage)
    {
        if (double.IsNaN(result))
        {
            return CalcResult<double>.Failure(CalcError.InvalidInput, invalidInputMessage);
        }

        if (double.IsInfinity(result))
        {
            return CalcResult<double>.Failure(CalcError.Overflow, "The result is too large to represent.");
        }

        return CalcResult<double>.Success(result);
    }
}