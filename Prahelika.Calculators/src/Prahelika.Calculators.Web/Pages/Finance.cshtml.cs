using Microsoft.AspNetCore.Mvc;
using Prahelika.Calculators.Core.Finance;
using Prahelika.Calculators.Web.Models;

namespace Prahelika.Calculators.Web.Pages;

public class FinanceModel(IFinanceCalculator calculator) : CalculatorPageModel
{
    [BindProperty(SupportsGet = true)]
    public FinanceCalculation Calculation { get; set; } = FinanceCalculation.SimpleInterest;

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

    /// <summary>The chart numbers as JSON, only when a loan schedule has just been calculated.</summary>
    public string? ChartJson { get; private set; }

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
        if (!ModelState.IsValid)
        {
            Fail("Please enter valid numbers. Months must be a whole number.");
            return;
        }

        bool usesYears = Calculation is FinanceCalculation.SimpleInterest or FinanceCalculation.CompoundInterest;
        bool usesMonths = Calculation is FinanceCalculation.Emi or FinanceCalculation.Amortization or FinanceCalculation.Sip;

        var missing = new List<string>();
        if (Amount is null) missing.Add(AmountLabel);
        if (Calculation == FinanceCalculation.Roi)
        {
            if (FinalValue is null) missing.Add("Final value");
        }
        else if (Rate is null)
        {
            missing.Add(RateLabel);
        }
        if (usesYears && Years is null) missing.Add("Time (years)");
        if (usesMonths && Months is null) missing.Add("Time (months)");

        if (missing.Count > 0)
        {
            Fail("Please fill in: " + string.Join(", ", missing) + ".");
            return;
        }

        decimal amount = Amount!.Value;
        decimal rate = Rate ?? 0;

        switch (Calculation)
        {
            case FinanceCalculation.SimpleInterest:
                Show(calculator.SimpleInterest(amount, rate, Years!.Value), money: true);
                break;
            case FinanceCalculation.CompoundInterest:
                Show(calculator.CompoundInterest(amount, rate, Years!.Value, Frequency), money: true);
                break;
            case FinanceCalculation.Emi:
                Show(calculator.Emi(amount, rate, Months!.Value), money: true);
                break;
            case FinanceCalculation.Amortization:
                {
                    var schedule = calculator.AmortizationSchedule(amount, rate, Months!.Value);
                    Show(schedule, money: true);

                    if (schedule.IsSuccess)
                    {
                        ChartJson = LoanChartData.From(schedule.Value!).ToJson();
                    }

                    break;
                }
            case FinanceCalculation.Sip:
                Show(calculator.SipFutureValue(amount, rate, Months!.Value), money: true);
                break;
            case FinanceCalculation.Roi:
                Show(calculator.ReturnOnInvestment(amount, FinalValue!.Value), money: true);
                break;
            case FinanceCalculation.Gst:
                Show(calculator.Gst(amount, rate, Mode), money: true);
                break;
        }
    }
}