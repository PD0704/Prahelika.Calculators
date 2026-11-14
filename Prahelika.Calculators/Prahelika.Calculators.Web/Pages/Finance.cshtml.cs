using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Prahelika.Calculators.Core.Finance;
using Prahelika.Calculators.Web.Models;

namespace Prahelika.Calculators.Web.Pages;

public class FinanceModel : PageModel
{
    // SupportsGet lets this come from the address (/Finance?calculation=Emi) as well as from the form.
    [BindProperty(SupportsGet = true)]
    public FinanceCalculation Calculation { get; set; } = FinanceCalculation.SimpleInterest;

    // One "amount" box whose label depends on the calculation: principal, loan amount, and so on.
    [BindProperty]
    public decimal? Amount { get; set; }

    [BindProperty]
    public decimal? Rate { get; set; }

    [BindProperty]
    public decimal? Years { get; set; }

    [BindProperty]
    public int? Months { get; set; }

    [BindProperty]
    public decimal? FinalValue { get; set; }

    [BindProperty]
    public CompoundingFrequency Frequency { get; set; } = CompoundingFrequency.Annually;

    [BindProperty]
    public GstMode Mode { get; set; } = GstMode.Exclusive;

    public string AmountLabel => Calculation switch
    {
        FinanceCalculation.SimpleInterest or FinanceCalculation.CompoundInterest => "Principal (starting amount)",
        FinanceCalculation.Emi or FinanceCalculation.Amortization => "Loan amount",
        FinanceCalculation.Sip => "Monthly investment",
        FinanceCalculation.Roi => "Amount invested",
        _ => "Amount"
    };

    public string RateLabel => Calculation switch
    {
        FinanceCalculation.Sip => "Expected return per year (%)",
        FinanceCalculation.Gst => "GST rate (%)",
        _ => "Interest rate per year (%)"
    };

    public string Description => Calculation switch
    {
        FinanceCalculation.SimpleInterest => "Interest earned on the original amount only: principal x rate x years.",
        FinanceCalculation.CompoundInterest => "Interest is added to the balance, so it earns interest too.",
        FinanceCalculation.Emi => "The fixed monthly payment on a loan, with the total paid and total interest.",
        FinanceCalculation.Amortization => "A month-by-month plan showing how much of each payment is principal and how much is interest.",
        FinanceCalculation.Sip => "What a fixed monthly investment could grow to.",
        FinanceCalculation.Roi => "How much you gained or lost, as a percentage of what you invested.",
        FinanceCalculation.Gst => "Add GST to a price, or take it out of a price that already includes it.",
        _ => string.Empty
    };

    public void OnGet()
    {
    }

    public void OnPost()
    {
        // The calculation is connected in task 3.6.
    }
}