namespace Prahelika.Calculators.Core.Finance;

/// <summary>"What if" calculations for loans, using <see cref="decimal"/> arithmetic.</summary>
public interface ILoanPlanner
{
    /// <summary>The number of decimal places that money amounts are rounded to.</summary>
    int DecimalPlaces { get; }

    /// <summary>
    /// What happens if you pay a lump sum right after one of your EMIs, and keep paying the same EMI.
    /// The loan then ends sooner. The rate is a percentage per year. Months must be between 1 and 1200.
    /// The prepayment month must be at least 1 and less than the number of months. If the lump sum is
    /// larger than what you still owe, it closes the loan.
    /// The interest totals add up the interest in each month of the repayment schedule, so they can differ
    /// by a few paise from <see cref="FinanceCalculator.Emi"/>.
    /// </summary>
    CalcResult<PrepaymentResult> Prepay(
        decimal loanAmount,
        decimal annualRatePercent,
        int months,
        int prepaymentMonth,
        decimal prepaymentAmount);
}