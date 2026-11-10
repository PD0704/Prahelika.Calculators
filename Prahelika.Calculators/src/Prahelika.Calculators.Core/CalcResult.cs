namespace Prahelika.Calculators.Core;

/// <summary>The outcome of a calculation: either a value or an error.</summary>
/// <typeparam name="T">The type of the calculated value.</typeparam>
public sealed record CalcResult<T>
{
    private CalcResult(bool isSuccess, T? value, CalcError error, string? message)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Message = message;
    }

    /// <summary>True when the calculation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>The calculated value. Only meaningful when <see cref="IsSuccess"/> is true.</summary>
    public T? Value { get; }

    /// <summary>The error category. <see cref="CalcError.None"/> on success.</summary>
    public CalcError Error { get; }

    /// <summary>A human-readable explanation. Null on success.</summary>
    public string? Message { get; }

    /// <summary>Creates a successful result.</summary>
    public static CalcResult<T> Success(T value) =>
        new(true, value, CalcError.None, null);

    /// <summary>Creates a failed result.</summary>
    public static CalcResult<T> Failure(CalcError error, string message) =>
        new(false, default, error, message);
}