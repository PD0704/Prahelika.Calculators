namespace Prahelika.Calculators.Core.Finance;

/// <summary>How much a person can borrow for a monthly payment they can afford.</summary>
/// <param name="MonthlyPayment">The monthly payment that was entered.</param>
/// <param name="MaxLoanAmount">The largest loan whose EMI does not go above the monthly payment.</param>
/// <param name="TotalPayment">The monthly payment multiplied by the number of months.</param>
/// <param name="TotalInterest">The total payment minus the loan amount.</param>
public sealed record BorrowingResult(
    decimal MonthlyPayment,
    decimal MaxLoanAmount,
    decimal TotalPayment,
    decimal TotalInterest);