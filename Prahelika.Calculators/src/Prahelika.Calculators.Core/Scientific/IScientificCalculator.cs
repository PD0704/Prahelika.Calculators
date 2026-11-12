namespace Prahelika.Calculators.Core.Scientific;

/// <summary>Powers, roots, logarithms and trigonometry, using <see cref="double"/> arithmetic.</summary>
public interface IScientificCalculator
{
    /// <summary>The unit used for angles: the input of Sin, Cos and Tan, and the output of Asin, Acos and Atan.</summary>
    AngleUnit AngleUnit { get; }

    /// <summary>Returns value raised to the given exponent.</summary>
    CalcResult<double> Power(double value, double exponent);

    /// <summary>Returns the square root. Fails for negative numbers.</summary>
    CalcResult<double> SquareRoot(double value);

    /// <summary>Returns the root of the given degree, e.g. degree 3 is the cube root.</summary>
    CalcResult<double> NthRoot(double value, int degree);

    /// <summary>Returns the natural logarithm (base e). Fails unless value is positive.</summary>
    CalcResult<double> NaturalLog(double value);

    /// <summary>Returns the base-10 logarithm. Fails unless value is positive.</summary>
    CalcResult<double> Log10(double value);

    /// <summary>Returns the logarithm of value in the given base.</summary>
    CalcResult<double> Log(double value, double newBase);

    /// <summary>Returns the sine of an angle in the current <see cref="AngleUnit"/>.</summary>
    CalcResult<double> Sin(double angle);

    /// <summary>Returns the cosine of an angle in the current <see cref="AngleUnit"/>.</summary>
    CalcResult<double> Cos(double angle);

    /// <summary>Returns the tangent of an angle in the current <see cref="AngleUnit"/>.</summary>
    CalcResult<double> Tan(double angle);

    /// <summary>Returns the angle whose sine is value (value must be between -1 and 1).</summary>
    CalcResult<double> Asin(double value);

    /// <summary>Returns the angle whose cosine is value (value must be between -1 and 1).</summary>
    CalcResult<double> Acos(double value);

    /// <summary>Returns the angle whose tangent is value.</summary>
    CalcResult<double> Atan(double value);
}