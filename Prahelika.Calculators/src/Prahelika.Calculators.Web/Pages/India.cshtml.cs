using Microsoft.AspNetCore.Mvc;
using Prahelika.Calculators.Core.Finance;
using Prahelika.Calculators.Web.Models;

namespace Prahelika.Calculators.Web.Pages;

public class IndiaModel(
    ISavingsCalculator savings,
    IGstSplitCalculator gstSplit,
    IIncomeTaxCalculator incomeTax) : CalculatorPageModel
{
    [BindProperty(SupportsGet = true)]
    public IndiaCalculation Calculation { get; set; } = IndiaCalculation.FixedDeposit;

    [BindProperty]
    public decimal? Amount { get; set; }

    [BindProperty]
    public decimal? Rate { get; set; }

    [BindProperty]
    public int? Months { get; set; }

    [BindProperty]
    public int? Years { get; set; }

    [BindProperty]
    public CompoundingFrequency Frequency { get; set; } = CompoundingFrequency.Quarterly;

    [BindProperty]
    public GstMode Mode { get; set; } = GstMode.Exclusive;

    [BindProperty]
    public bool Salaried { get; set; } = true;

    public string AmountLabel => Calculation switch
    {
        IndiaCalculation.FixedDeposit => "Deposit amount",
        IndiaCalculation.RecurringDeposit => "Monthly deposit",
        IndiaCalculation.Ppf => "Yearly deposit (500 to 1,50,000)",
        IndiaCalculation.IncomeTax => "Income for the year, before tax",
        _ => "Amount"
    };

    public string RateLabel => Calculation switch
    {
        IndiaCalculation.Ppf => "PPF interest rate per year (%)",
        IndiaCalculation.GstSplit => "Total GST rate (%)",
        _ => "Interest rate per year (%)"
    };

    public string Description => Calculation switch
    {
        IndiaCalculation.FixedDeposit => "A lump sum earning compound interest. Banks usually add the interest every quarter.",
        IndiaCalculation.RecurringDeposit => "The same amount deposited at the start of every month, with interest added every quarter.",
        IndiaCalculation.Ppf => "A deposit at the start of every year, with interest added at the end of each year. The government revises the rate, so enter the current one.",
        IndiaCalculation.GstSplit => "GST for a sale within one state is split equally into CGST (central) and SGST (state). A rate of 18 gives 9% CGST and 9% SGST.",
        IndiaCalculation.IncomeTax => "An estimate of income tax under the new regime: standard deduction, slabs, rebate and cess.",
        _ => string.Empty
    };

    public void OnGet()
    {
    }

    public void OnPost()
    {
        if (!ModelState.IsValid)
        {
            Fail("Please enter valid numbers. Months and years must be whole numbers.");
            return;
        }

        bool usesRate = Calculation != IndiaCalculation.IncomeTax;
        bool usesMonths = Calculation is IndiaCalculation.FixedDeposit or IndiaCalculation.RecurringDeposit;
        bool usesYears = Calculation == IndiaCalculation.Ppf;

        var missing = new List<string>();
        if (Amount is null) missing.Add(AmountLabel);
        if (usesRate && Rate is null) missing.Add(RateLabel);
        if (usesMonths && Months is null) missing.Add("Time (months)");
        if (usesYears && Years is null) missing.Add("Time (years)");

        if (missing.Count > 0)
        {
            Fail("Please fill in: " + string.Join(", ", missing) + ".");
            return;
        }

        decimal amount = Amount!.Value;
        decimal rate = Rate ?? 0;

        switch (Calculation)
        {
            case IndiaCalculation.FixedDeposit:
                Show(savings.FixedDeposit(amount, rate, Months!.Value, Frequency), money: true);
                break;
            case IndiaCalculation.RecurringDeposit:
                Show(savings.RecurringDeposit(amount, rate, Months!.Value), money: true);
                break;
            case IndiaCalculation.Ppf:
                Show(savings.Ppf(amount, rate, Years!.Value), money: true);
                break;
            case IndiaCalculation.GstSplit:
                Show(gstSplit.GstSplit(amount, rate, Mode), money: true);
                break;
            case IndiaCalculation.IncomeTax:
                Show(incomeTax.EstimateNewRegime(amount, Salaried), money: true);
                Note = $"New regime, FY {incomeTax.Rules.FinancialYear}. This is an estimate: surcharge, other kinds of income and "
                     + "tax-saving deductions are not included. Check incometax.gov.in for the latest rules.";
                break;
        }
    }
}