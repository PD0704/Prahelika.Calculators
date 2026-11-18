namespace Prahelika.Calculators.Core.Finance;

/// <summary>
/// Income-tax estimates for individuals under the new regime, using <see cref="decimal"/> arithmetic.
/// The rules (slabs, rebate, cess) come from an <see cref="IncomeTaxRules"/> table, so they can change without touching this code.
/// Amounts are rounded to the chosen number of decimal places, half away from zero.
/// </summary>
public sealed class IncomeTaxCalculator : IIncomeTaxCalculator
{
    /// <param name="rules">The tax rules to use. The default is the current new regime, <see cref="IncomeTaxRules.NewRegime202627"/>.</param>
    /// <param name="decimalPlaces">How many decimal places to round tax amounts to (0 to 10). The default is 2.</param>
    public IncomeTaxCalculator(IncomeTaxRules? rules = null, int decimalPlaces = 2)
    {
        // Keep this range the same as FinanceCalculator's constructor.
        if (decimalPlaces < 0 || decimalPlaces > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "Decimal places must be between 0 and 10.");
        }

        Rules = rules ?? IncomeTaxRules.NewRegime202627;
        CheckRules(Rules);
        DecimalPlaces = decimalPlaces;
    }

    /// <inheritdoc />
    public IncomeTaxRules Rules { get; }

    /// <inheritdoc />
    public int DecimalPlaces { get; }

    /// <inheritdoc />
    public CalcResult<IncomeTaxResult> EstimateNewRegime(decimal annualIncome, bool isSalaried = true)
    {
        if (annualIncome < 0)
        {
            return Invalid("The income cannot be negative.");
        }

        decimal standardDeduction = isSalaried ? Math.Min(annualIncome, Rules.StandardDeduction) : 0m;
        decimal taxableIncome = annualIncome - standardDeduction;

        if (taxableIncome > Rules.MaxSupportedIncome)
        {
            return Invalid($"Taxable income above {Rules.MaxSupportedIncome:0} is not supported, because surcharge applies there and is not calculated.");
        }

        // Tax in each slab that the income reaches.
        var breakdown = new List<SlabTax>();
        decimal taxOnSlabs = 0m;

        for (int i = 0; i < Rules.Slabs.Count && taxableIncome > Rules.Slabs[i].From; i++)
        {
            TaxSlab slab = Rules.Slabs[i];
            decimal? upperLimit = i + 1 < Rules.Slabs.Count ? Rules.Slabs[i + 1].From : null;

            decimal top = upperLimit is null ? taxableIncome : Math.Min(taxableIncome, upperLimit.Value);
            decimal incomeInSlab = top - slab.From;
            decimal tax = Round(incomeInSlab * slab.RatePercent / 100m);

            breakdown.Add(new SlabTax(slab.From, upperLimit, slab.RatePercent, incomeInSlab, tax));
            taxOnSlabs += tax;
        }

        // Up to the limit the rebate covers the tax (to its maximum). Just above the limit, marginal relief
        // makes sure the tax is never more than the income above the limit.
        decimal rebate = taxableIncome <= Rules.RebateIncomeLimit
            ? Math.Min(taxOnSlabs, Rules.RebateMaximum)
            : Math.Max(0m, taxOnSlabs - (taxableIncome - Rules.RebateIncomeLimit));

        decimal taxAfterRebate = taxOnSlabs - rebate;
        decimal cess = Round(taxAfterRebate * Rules.CessPercent / 100m);
        decimal totalTax = taxAfterRebate + cess;

        decimal effectiveRate = annualIncome > 0
            ? Math.Round(totalTax / annualIncome * 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;

        return CalcResult<IncomeTaxResult>.Success(new IncomeTaxResult(
            Rules.FinancialYear,
            annualIncome,
            standardDeduction,
            taxableIncome,
            taxOnSlabs,
            rebate,
            taxAfterRebate,
            cess,
            totalTax,
            effectiveRate,
            breakdown));
    }

    private decimal Round(decimal value) => decimal.Round(value, DecimalPlaces, MidpointRounding.AwayFromZero);

    private static CalcResult<IncomeTaxResult> Invalid(string message) =>
        CalcResult<IncomeTaxResult>.Failure(CalcError.InvalidInput, message);

    // Rules that do not make sense are a programming error, so they throw.
    private static void CheckRules(IncomeTaxRules rules)
    {
        if (rules.Slabs is null || rules.Slabs.Count == 0)
        {
            throw new ArgumentException("There must be at least one tax slab.", nameof(rules));
        }

        if (rules.Slabs[0].From != 0m)
        {
            throw new ArgumentException("The first tax slab must start at 0.", nameof(rules));
        }

        for (int i = 0; i < rules.Slabs.Count; i++)
        {
            if (rules.Slabs[i].RatePercent < 0m || rules.Slabs[i].RatePercent > 100m)
            {
                throw new ArgumentException("Tax rates must be between 0 and 100.", nameof(rules));
            }

            if (i > 0 && rules.Slabs[i].From <= rules.Slabs[i - 1].From)
            {
                throw new ArgumentException("The tax slabs must be in ascending order.", nameof(rules));
            }
        }

        if (rules.StandardDeduction < 0m || rules.RebateIncomeLimit < 0m || rules.RebateMaximum < 0m
            || rules.CessPercent < 0m || rules.MaxSupportedIncome <= 0m)
        {
            throw new ArgumentException("The deduction, rebate, cess and income limit cannot be negative.", nameof(rules));
        }
    }
}