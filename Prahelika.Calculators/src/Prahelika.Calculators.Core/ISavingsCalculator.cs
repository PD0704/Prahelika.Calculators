namespace Prahelika.Calculators.Core.Finance;

/// <summary>Savings and deposit calculations, using <see cref="decimal"/> arithmetic.</summary>
public interface ISavingsCalculator
{
    /// <summary>The number of decimal places that money amounts are rounded to.</summary>
    int DecimalPlaces { get; }

    /// <summary>
    /// Fixed deposit (FD): a lump sum earning compound interest for a number of months.
    /// The rate is a percentage per year. Indian banks usually compound FDs quarterly, which is the default.
    /// The deposit must be above zero, the rate cannot be negative, and months must be between 1 and 1200.
    /// </summary>
    CalcResult<MaturityResult> FixedDeposit(
        decimal principal,
        decimal annualRatePercent,
        int months,
        CompoundingFrequency frequency = CompoundingFrequency.Quarterly);

    /// <summary>
    /// Recurring deposit (RD): the same amount deposited at the start of every month, compounded quarterly.
    /// The rate is a percentage per year. Months must be between 1 and 1200.
    /// </summary>
    CalcResult<MaturityResult> RecurringDeposit(decimal monthlyDeposit, decimal annualRatePercent, int months);

    /// <summary>
    /// Public Provident Fund (PPF): the same amount deposited at the start of every year, with interest added
    /// at the end of each year. The yearly deposit must be between 500 and 150,000. The term must be at least
    /// 15 years, or longer in blocks of 5 (15, 20, 25, up to 50). The government revises the rate, so it is an input.
    /// </summary>
    CalcResult<MaturityResult> Ppf(decimal yearlyDeposit, decimal annualRatePercent, int years);
}