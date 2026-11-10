namespace Prahelika.Calculators.Core;

/// <summary>Describes why a calculation failed.</summary>
public enum CalcError
{
    /// <summary>No error. Used only for successful results.</summary>
    None = 0,

    /// <summary>An attempt was made to divide by zero.</summary>
    DivisionByZero,

    /// <summary>One or more inputs are not valid for the operation.</summary>
    InvalidInput,

    /// <summary>The result is too large or too small to represent.</summary>
    Overflow
}