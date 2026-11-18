namespace Prahelika.Calculators.Core.Finance;

/// <summary>The outcome of a deposit calculation (FD, RD or PPF).</summary>
/// <param name="InvestedAmount">The total money put in.</param>
/// <param name="InterestEarned">The interest earned: maturity amount minus invested amount.</param>
/// <param name="MaturityAmount">What the deposit is worth at the end.</param>
public sealed record MaturityResult(decimal InvestedAmount, decimal InterestEarned, decimal MaturityAmount);