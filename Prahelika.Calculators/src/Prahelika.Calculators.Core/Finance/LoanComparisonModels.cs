namespace Prahelika.Calculators.Core.Finance;

/// <summary>A loan offer to compare.</summary>
/// <param name="LoanAmount">The amount borrowed. It must be above zero.</param>
/// <param name="AnnualRatePercent">The interest rate as a percentage per year. It cannot be negative.</param>
/// <param name="Months">How many months the loan runs, from 1 to 1200.</param>
/// <param name="ProcessingFee">A one-time fee charged by the lender. The default is 0, and it cannot be negative.</param>
public sealed record LoanOffer(decimal LoanAmount, decimal AnnualRatePercent, int Months, decimal ProcessingFee = 0m);

/// <summary>What one loan offer costs.</summary>
/// <param name="MonthlyPayment">The EMI.</param>
/// <param name="TotalPayment">The EMI multiplied by the number of months.</param>
/// <param name="TotalInterest">The total payment minus the loan amount.</param>
/// <param name="ProcessingFee">The one-time fee.</param>
/// <param name="TotalCost">Total interest plus the processing fee: what borrowing costs above the amount borrowed.</param>
public sealed record LoanOfferSummary(
    decimal MonthlyPayment,
    decimal TotalPayment,
    decimal TotalInterest,
    decimal ProcessingFee,
    decimal TotalCost);

/// <summary>Which of two loans costs less overall.</summary>
public enum LoanChoice
{
    /// <summary>Both cost the same.</summary>
    Same = 0,

    /// <summary>The first loan costs less.</summary>
    First = 1,

    /// <summary>The second loan costs less.</summary>
    Second = 2
}

/// <summary>Two loans side by side.</summary>
/// <param name="First">The first loan.</param>
/// <param name="Second">The second loan.</param>
/// <param name="Cheaper">Which loan has the lower total cost.</param>
/// <param name="CostDifference">How much lower the cheaper loan's total cost is. It is never negative.</param>
public sealed record LoanComparison(
    LoanOfferSummary First,
    LoanOfferSummary Second,
    LoanChoice Cheaper,
    decimal CostDifference);