namespace Prahelika.Calculators.Core.Finance;

/// <summary>One step of an income-tax table: income from <see cref="From"/> upwards is taxed at <see cref="RatePercent"/>, until the next slab starts.</summary>
/// <param name="From">Where the slab starts. The first slab must start at 0.</param>
/// <param name="RatePercent">The tax rate for income in this slab, as a percentage.</param>
public sealed record TaxSlab(decimal From, decimal RatePercent);

/// <summary>The tax worked out for one slab.</summary>
/// <param name="From">Where the slab starts.</param>
/// <param name="To">Where the slab ends, or null for the last slab, which has no upper limit.</param>
/// <param name="RatePercent">The slab's rate, as a percentage.</param>
/// <param name="IncomeInSlab">How much of the taxable income falls in this slab.</param>
/// <param name="Tax">The tax on that part of the income.</param>
public sealed record SlabTax(decimal From, decimal? To, decimal RatePercent, decimal IncomeInSlab, decimal Tax);

/// <summary>
/// The tax rules for one financial year. They change with the Budget, so they are data, not code:
/// keep them here, or build your own and pass them to <see cref="IncomeTaxCalculator"/>.
/// </summary>
/// <param name="FinancialYear">For example "2026-27".</param>
/// <param name="StandardDeduction">The deduction for salaried people.</param>
/// <param name="RebateIncomeLimit">Taxable income up to this amount gets the rebate (section 87A in the 1961 Act).</param>
/// <param name="RebateMaximum">The largest rebate.</param>
/// <param name="CessPercent">Health and education cess, as a percentage of the tax.</param>
/// <param name="MaxSupportedIncome">The largest taxable income this estimate covers. Above it, surcharge applies, which is not calculated.</param>
/// <param name="Slabs">The slabs, in order, starting at 0.</param>
public sealed record IncomeTaxRules(
    string FinancialYear,
    decimal StandardDeduction,
    decimal RebateIncomeLimit,
    decimal RebateMaximum,
    decimal CessPercent,
    decimal MaxSupportedIncome,
    IReadOnlyList<TaxSlab> Slabs)
{
    /// <summary>The new tax regime for FY 2026-27 (AY 2027-28). Check incometax.gov.in for changes.</summary>
    public static IncomeTaxRules NewRegime202627 { get; } = new(
        FinancialYear: "2026-27",
        StandardDeduction: 75_000m,
        RebateIncomeLimit: 1_200_000m,
        RebateMaximum: 60_000m,
        CessPercent: 4m,
        MaxSupportedIncome: 5_000_000m,
        Slabs: new[]
        {
            new TaxSlab(0m, 0m),
            new TaxSlab(400_000m, 5m),
            new TaxSlab(800_000m, 10m),
            new TaxSlab(1_200_000m, 15m),
            new TaxSlab(1_600_000m, 20m),
            new TaxSlab(2_000_000m, 25m),
            new TaxSlab(2_400_000m, 30m),
        });
}

/// <summary>An income-tax estimate.</summary>
/// <param name="FinancialYear">The financial year of the rules that were used.</param>
/// <param name="GrossIncome">The income before the standard deduction.</param>
/// <param name="StandardDeduction">The standard deduction that was applied.</param>
/// <param name="TaxableIncome">Gross income minus the standard deduction.</param>
/// <param name="TaxOnSlabs">The tax from the slabs, before any rebate.</param>
/// <param name="RebateAndRelief">The rebate, or the marginal relief just above the rebate limit.</param>
/// <param name="TaxAfterRebate">The slab tax minus the rebate or relief.</param>
/// <param name="Cess">Health and education cess on the tax after rebate.</param>
/// <param name="TotalTax">The tax after rebate plus cess.</param>
/// <param name="EffectiveRatePercent">Total tax as a percentage of gross income, to 2 places.</param>
/// <param name="Breakdown">The tax in each slab that the income reaches.</param>
public sealed record IncomeTaxResult(
    string FinancialYear,
    decimal GrossIncome,
    decimal StandardDeduction,
    decimal TaxableIncome,
    decimal TaxOnSlabs,
    decimal RebateAndRelief,
    decimal TaxAfterRebate,
    decimal Cess,
    decimal TotalTax,
    decimal EffectiveRatePercent,
    IReadOnlyList<SlabTax> Breakdown);