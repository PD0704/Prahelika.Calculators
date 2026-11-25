using Prahelika.Calculators.Core.Finance;

namespace Prahelika.Calculators.Api.Endpoints;

public static class LoanEndpoints
{
    public static IEndpointRouteBuilder MapLoanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/loans").WithTags("Loans");

        // ILoanPlanner is supplied by dependency injection (registered in Program.cs).
        // All rates are percentages per year: 9 means 9%. Amounts are in rupees.

        group.MapGet("/prepayment",
                (decimal loanAmount, decimal rate, int months, int prepaymentMonth, decimal prepaymentAmount, ILoanPlanner planner) =>
                    planner.Prepay(loanAmount, rate, months, prepaymentMonth, prepaymentAmount).ToHttpResult())
            .WithSummary("What a lump-sum prepayment does to a loan: new number of months and interest saved. The EMI stays the same. The prepayment month is the EMI after which you pay (1 to months - 1).")
            .ProducesCalculation<PrepaymentResult>();

        group.MapGet("/compare",
                (decimal firstAmount, decimal firstRate, int firstMonths,
                 decimal secondAmount, decimal secondRate, int secondMonths,
                 ILoanPlanner planner, decimal firstFee = 0m, decimal secondFee = 0m) =>
                    planner.CompareLoans(
                            new LoanOffer(firstAmount, firstRate, firstMonths, firstFee),
                            new LoanOffer(secondAmount, secondRate, secondMonths, secondFee))
                        .ToHttpResult())
            .WithSummary("Two loans side by side: EMI, total interest, processing fee and total cost of each, and which is cheaper. The fees are optional.")
            .ProducesCalculation<LoanComparison>();

        group.MapGet("/max-amount",
                (decimal monthlyPayment, decimal rate, int months, ILoanPlanner planner) =>
                    planner.MaxLoanAmount(monthlyPayment, rate, months).ToHttpResult())
            .WithSummary("How much can I borrow? The largest loan whose EMI fits the monthly payment you can afford.")
            .ProducesCalculation<BorrowingResult>();

        return app;
    }
}