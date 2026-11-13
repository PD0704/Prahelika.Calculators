namespace Prahelika.Calculators.Core.Finance;

/// <summary>The outcome of an interest calculation.</summary>
/// <param name="Principal">The starting amount.</param>
/// <param name="Interest">The interest earned, rounded to the calculator's decimal places.</param>
/// <param name="TotalAmount">The principal plus the interest.</param>
public sealed record InterestResult(decimal Principal, decimal Interest, decimal TotalAmount);