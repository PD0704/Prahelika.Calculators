namespace Prahelika.Calculators.Core.Finance;

/// <summary>The monthly payment on a loan, and what the whole loan costs.</summary>
/// <param name="MonthlyPayment">The EMI: the fixed amount paid each month.</param>
/// <param name="TotalPayment">The monthly payment multiplied by the number of months.</param>
/// <param name="TotalInterest">The total payment minus the loan amount.</param>
public sealed record LoanSummary(decimal MonthlyPayment, decimal TotalPayment, decimal TotalInterest);

/// <summary>One month of a loan repayment schedule.</summary>
/// <param name="Month">The month number, starting at 1.</param>
/// <param name="Payment">The amount paid this month (principal plus interest).</param>
/// <param name="PrincipalPaid">The part of the payment that reduces the loan.</param>
/// <param name="InterestPaid">The part of the payment that is interest.</param>
/// <param name="RemainingBalance">What is still owed after this payment.</param>
public sealed record AmortizationRow(int Month, decimal Payment, decimal PrincipalPaid, decimal InterestPaid, decimal RemainingBalance);

/// <summary>The outcome of a SIP (monthly investment) calculation.</summary>
/// <param name="InvestedAmount">The total money put in.</param>
/// <param name="EstimatedReturns">The growth: future value minus the invested amount.</param>
/// <param name="FutureValue">What the investment is expected to be worth at the end.</param>
public sealed record SipResult(decimal InvestedAmount, decimal EstimatedReturns, decimal FutureValue);

/// <summary>The outcome of a return-on-investment calculation.</summary>
/// <param name="Gain">Final value minus amount invested. Negative for a loss.</param>
/// <param name="RoiPercent">The gain as a percentage of the amount invested.</param>
public sealed record RoiResult(decimal Gain, decimal RoiPercent);

/// <summary>Whether an amount already includes GST.</summary>
public enum GstMode
{
    /// <summary>The amount is the price before GST, so GST is added on top.</summary>
    Exclusive = 0,

    /// <summary>The amount already includes GST, so GST is taken out of it.</summary>
    Inclusive = 1
}

/// <summary>The outcome of a GST calculation.</summary>
/// <param name="NetAmount">The price without GST.</param>
/// <param name="GstAmount">The GST.</param>
/// <param name="TotalAmount">The price with GST (net plus GST).</param>
public sealed record GstResult(decimal NetAmount, decimal GstAmount, decimal TotalAmount);