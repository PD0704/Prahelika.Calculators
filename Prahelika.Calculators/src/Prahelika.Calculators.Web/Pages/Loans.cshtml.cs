using Microsoft.AspNetCore.Mvc;
using Prahelika.Calculators.Core.Finance;
using Prahelika.Calculators.Web.Models;

namespace Prahelika.Calculators.Web.Pages;

public class LoansModel(ILoanPlanner planner) : CalculatorPageModel
{
    [BindProperty(SupportsGet = true)]
    public LoanTool Tool { get; set; } = LoanTool.Prepayment;

    // The loan (or the first loan, when comparing).
    [BindProperty]
    public decimal? Amount { get; set; }

    [BindProperty]
    public decimal? Rate { get; set; }

    [BindProperty]
    public int? Months { get; set; }

    [BindProperty]
    public decimal? Fee { get; set; }

    // Prepayment.
    [BindProperty]
    public int? PrepaymentMonth { get; set; }

    [BindProperty]
    public decimal? PrepaymentAmount { get; set; }

    // The second loan, when comparing.
    [BindProperty]
    public decimal? SecondAmount { get; set; }

    [BindProperty]
    public decimal? SecondRate { get; set; }

    [BindProperty]
    public int? SecondMonths { get; set; }

    [BindProperty]
    public decimal? SecondFee { get; set; }

    // How much can I borrow?
    [BindProperty]
    public decimal? MonthlyPayment { get; set; }

    public string Description => Tool switch
    {
        LoanTool.Prepayment => "Pay a lump sum after one of your EMIs. The EMI stays the same, so the loan ends sooner and you pay less interest.",
        LoanTool.Compare => "Put two loans side by side. Total cost is the interest plus the processing fee, so a lower rate does not always win.",
        LoanTool.Borrow => "Enter the monthly payment you can afford, and see the largest loan it can pay off.",
        _ => string.Empty
    };

    public void OnGet()
    {
    }

    public void OnPost()
    {
        if (!ModelState.IsValid)
        {
            Fail("Please enter valid numbers. Months must be whole numbers.");
            return;
        }

        var missing = new List<string>();
        switch (Tool)
        {
            case LoanTool.Prepayment:
                if (Amount is null) missing.Add("Loan amount");
                if (Rate is null) missing.Add("Interest rate");
                if (Months is null) missing.Add("Loan term (months)");
                if (PrepaymentMonth is null) missing.Add("Prepayment month");
                if (PrepaymentAmount is null) missing.Add("Prepayment amount");
                break;
            case LoanTool.Compare:
                if (Amount is null) missing.Add("First loan: amount");
                if (Rate is null) missing.Add("First loan: interest rate");
                if (Months is null) missing.Add("First loan: term (months)");
                if (SecondAmount is null) missing.Add("Second loan: amount");
                if (SecondRate is null) missing.Add("Second loan: interest rate");
                if (SecondMonths is null) missing.Add("Second loan: term (months)");
                break;
            case LoanTool.Borrow:
                if (MonthlyPayment is null) missing.Add("Monthly payment you can afford");
                if (Rate is null) missing.Add("Interest rate");
                if (Months is null) missing.Add("Loan term (months)");
                break;
        }

        if (missing.Count > 0)
        {
            Fail("Please fill in: " + string.Join(", ", missing) + ".");
            return;
        }

        switch (Tool)
        {
            case LoanTool.Prepayment:
                Show(planner.Prepay(Amount!.Value, Rate!.Value, Months!.Value, PrepaymentMonth!.Value, PrepaymentAmount!.Value),
                    money: true);
                break;
            case LoanTool.Compare:
                ShowComparison();
                break;
            case LoanTool.Borrow:
                Show(planner.MaxLoanAmount(MonthlyPayment!.Value, Rate!.Value, Months!.Value), money: true);
                break;
        }
    }

    private void ShowComparison()
    {
        var first = new LoanOffer(Amount!.Value, Rate!.Value, Months!.Value, Fee ?? 0m);
        var second = new LoanOffer(SecondAmount!.Value, SecondRate!.Value, SecondMonths!.Value, SecondFee ?? 0m);

        var result = planner.CompareLoans(first, second);
        if (!result.IsSuccess)
        {
            Show(result);   // shows the error, such as which loan is not valid
            return;
        }

        LoanComparison comparison = result.Value!;
        var view = new ResultView();

        view.Details.Add(new("Cheaper loan", comparison.Cheaper switch
        {
            LoanChoice.First => "First loan",
            LoanChoice.Second => "Second loan",
            _ => "Both cost the same"
        }));
        view.Details.Add(new("Difference in total cost", ResultView.Money(comparison.CostDifference)));

        view.Columns.AddRange(new[] { "Compare", "First loan", "Second loan" });
        AddRow(view, "Monthly payment (EMI)", comparison.First.MonthlyPayment, comparison.Second.MonthlyPayment);
        AddRow(view, "Total payment", comparison.First.TotalPayment, comparison.Second.TotalPayment);
        AddRow(view, "Total interest", comparison.First.TotalInterest, comparison.Second.TotalInterest);
        AddRow(view, "Processing fee", comparison.First.ProcessingFee, comparison.Second.ProcessingFee);
        AddRow(view, "Total cost", comparison.First.TotalCost, comparison.Second.TotalCost);

        Display(view);
        Note = "Total cost is the interest plus the processing fee. For a fair comparison, both loans should borrow about the same amount.";
    }

    private static void AddRow(ResultView view, string label, decimal first, decimal second) =>
        view.Rows.Add(new List<string> { label, ResultView.Money(first), ResultView.Money(second) });
}