namespace Prahelika.Calculators.Core.Basic;

/// <summary>Default implementation of <see cref="IBasicCalculator"/> using <see cref="decimal"/> arithmetic.</summary>
public sealed class BasicCalculator : IBasicCalculator
{
    /// <inheritdoc />
    public CalcResult<decimal> Add(decimal a, decimal b) => Compute(() => a + b);

    /// <inheritdoc />
    public CalcResult<decimal> Subtract(decimal a, decimal b) => Compute(() => a - b);

    /// <inheritdoc />
    public CalcResult<decimal> Multiply(decimal a, decimal b) => Compute(() => a * b);

    /// <inheritdoc />
    public CalcResult<decimal> Divide(decimal a, decimal b)
    {
        if (b == 0m)
        {
            return CalcResult<decimal>.Failure(CalcError.DivisionByZero, "Cannot divide by zero.");
        }

        return Compute(() => a / b);
    }

    /// <inheritdoc />
    public CalcResult<decimal> Modulo(decimal a, decimal b)
    {
        if (b == 0m)
        {
            return CalcResult<decimal>.Failure(CalcError.DivisionByZero, "Cannot calculate a remainder when dividing by zero.");
        }

        return Compute(() => a % b);
    }

    /// <inheritdoc />
    public CalcResult<decimal> Percentage(decimal value, decimal percent) =>
        Compute(() => value * percent / 100m);

    // Runs an operation and converts a .NET overflow into a CalcResult failure,
    // so callers never have to catch exceptions themselves.
    private static CalcResult<decimal> Compute(Func<decimal> operation)
    {
        try
        {
            return CalcResult<decimal>.Success(operation());
        }
        catch (OverflowException)
        {
            return CalcResult<decimal>.Failure(CalcError.Overflow, "The result is too large or too small to represent.");
        }
    }
}