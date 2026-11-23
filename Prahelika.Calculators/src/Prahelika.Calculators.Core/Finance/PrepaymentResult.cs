namespace Prahelika.Calculators.Core.Finance;

/// <summary>What a lump-sum prepayment does to a loan when the monthly payment stays the same.</summary>
/// <param name="MonthlyPayment">The EMI, which does not change.</param>
/// <param name="OriginalMonths">How many months the loan would have taken.</param>
/// <param name="NewMonths">How many months it takes after the prepayment.</param>
/// <param name="MonthsSaved">Original months minus new months.</param>
/// <param name="PrepaymentApplied">The lump sum that was used. It is smaller than the amount entered if the loan was closed with less.</param>
/// <param name="OriginalTotalInterest">The interest paid over the whole loan without a prepayment.</param>
/// <param name="NewTotalInterest">The interest paid with the prepayment.</param>
/// <param name="InterestSaved">Original total interest minus new total interest.</param>
public sealed record PrepaymentResult(
    decimal MonthlyPayment,
    int OriginalMonths,
    int NewMonths,
    int MonthsSaved,
    decimal PrepaymentApplied,
    decimal OriginalTotalInterest,
    decimal NewTotalInterest,
    decimal InterestSaved);