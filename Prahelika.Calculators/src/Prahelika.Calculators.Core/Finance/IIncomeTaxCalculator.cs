namespace Prahelika.Calculators.Core.Finance;

/// <summary>Income-tax estimates for individuals, using <see cref="decimal"/> arithmetic.</summary>
public interface IIncomeTaxCalculator
{
    /// <summary>The tax rules this calculator uses.</summary>
    IncomeTaxRules Rules { get; }

    /// <summary>The number of decimal places that tax amounts are rounded to.</summary>
    int DecimalPlaces { get; }

    /// <summary>
    /// Estimates the tax for one year under the new regime: standard deduction (if salaried), slab tax,
    /// rebate with marginal relief, and cess. It is an estimate: surcharge, other income types and
    /// rounding to the nearest ten rupees are not included.
    /// The income cannot be negative, and the taxable income cannot be above <see cref="IncomeTaxRules.MaxSupportedIncome"/>.
    /// </summary>
    CalcResult<IncomeTaxResult> EstimateNewRegime(decimal annualIncome, bool isSalaried = true);
}