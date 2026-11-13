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


    /// <summary>
    /// The monthly loan payment (EMI), total payment and total interest.
    /// The rate is a percentage per year. Months must be between 1 and 1200.
    /// Fails if the loan amount is not above zero, the rate is negative, or the months are out of range.
    /// </summary>
    CalcResult<LoanSummary> Emi(decimal loanAmount, decimal annualRatePercent, int months);

    /// <summary>
    /// A month-by-month repayment schedule. The last payment clears whatever remains, so the final balance is 0.
    /// The same inputs and rules as <see cref="Emi"/>.
    /// </summary>
    CalcResult<IReadOnlyList<AmortizationRow>> AmortizationSchedule(decimal loanAmount, decimal annualRatePercent, int months);

    /// <summary>
    /// The future value of investing the same amount at the start of every month (a SIP).
    /// The return is a percentage per year, applied monthly. Months must be between 1 and 1200.
    /// </summary>
    CalcResult<SipResult> SipFutureValue(decimal monthlyInvestment, decimal expectedAnnualReturnPercent, int months);

    /// <summary>The gain and the return on investment as a percentage. The amount invested must be above zero.</summary>
    CalcResult<RoiResult> ReturnOnInvestment(decimal amountInvested, decimal finalValue);

    /// <summary>Adds GST to a price, or takes it out of one, depending on the mode. The rate is a percentage.</summary>
    CalcResult<GstResult> Gst(decimal amount, decimal gstRatePercent, GstMode mode = GstMode.Exclusive);
}