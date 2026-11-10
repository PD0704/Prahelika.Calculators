namespace Prahelika.Calculators.Core.Basic;

/// <summary>Everyday arithmetic operations.</summary>
public interface IBasicCalculator
{
    /// <summary>Returns a + b.</summary>
    CalcResult<decimal> Add(decimal a, decimal b);

    /// <summary>Returns a - b.</summary>
    CalcResult<decimal> Subtract(decimal a, decimal b);

    /// <summary>Returns a * b.</summary>
    CalcResult<decimal> Multiply(decimal a, decimal b);

    /// <summary>Returns a / b. Fails with <see cref="CalcError.DivisionByZero"/> when b is 0.</summary>
    CalcResult<decimal> Divide(decimal a, decimal b);

    /// <summary>Returns the remainder of a / b. Fails with <see cref="CalcError.DivisionByZero"/> when b is 0.</summary>
    CalcResult<decimal> Modulo(decimal a, decimal b);

    /// <summary>Returns the given percentage of a value, e.g. 20% of 150 is 30.</summary>
    CalcResult<decimal> Percentage(decimal value, decimal percent);
}