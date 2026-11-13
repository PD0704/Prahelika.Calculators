namespace Prahelika.Calculators.Core.Finance;

/// <summary>Money calculations, using <see cref="decimal"/> arithmetic.</summary>
public interface IFinanceCalculator
{
    /// <summary>The number of decimal places that money amounts are rounded to.</summary>
    int DecimalPlaces { get; }

    /// <summary>
    /// Simple interest: principal x rate x years. The rate is a percentage per year, so 5 means 5%.
    /// Fails if any input is negative.
    /// </summary>
    CalcResult<InterestResult> SimpleInterest(decimal principal, decimal annualRatePercent, decimal years);

    /// <summary>
    /// Compound interest: interest is added to the balance as often as the frequency says.
    /// The rate is a percentage per year. Years can include fractions, such as 2.5.
    /// Fails if any input is negative or the frequency is not valid.
    /// </summary>
    CalcResult<InterestResult> CompoundInterest(
        decimal principal,
        decimal annualRatePercent,
        decimal years,
        CompoundingFrequency frequency = CompoundingFrequency.Annually);
}