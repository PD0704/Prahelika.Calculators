using Prahelika.Calculators.Core.Finance;

namespace Prahelika.Calculators.Core.Tests.Finance;

public class IncomeTaxCalculatorTests
{
    private readonly IncomeTaxCalculator _calculator = new();

    // ---------- Whole estimates, from gross income ----------

    [Theory]
    [InlineData(0, true, 0, 0, 0, 0)]
    [InlineData(475000, true, 400000, 0, 0, 0)]            // 4.75 lakh salaried: nothing to pay
    [InlineData(800000, true, 725000, 16250, 16250, 0)]    // the rebate wipes out the tax
    [InlineData(1275000, true, 1200000, 60000, 60000, 0)]  // 12.75 lakh salaried: still nothing
    [InlineData(1280000, true, 1205000, 60750, 55750, 5200)] // marginal relief just above the limit
    [InlineData(1500000, true, 1425000, 93750, 0, 97500)]
    [InlineData(2500000, true, 2425000, 307500, 0, 319800)]
    [InlineData(1200000, false, 1200000, 60000, 60000, 0)] // not salaried: no standard deduction
    [InlineData(75000, true, 0, 0, 0, 0)]
    [InlineData(50000, true, 0, 0, 0, 0)]                  // the deduction cannot exceed the income
    public void Estimate_Matches_Known_Values(
        double gross, bool salaried, double taxable, double taxOnSlabs, double rebate, double totalTax)
    {
        var result = _calculator.EstimateNewRegime((decimal)gross, salaried);

        Assert.True(result.IsSuccess);
        Assert.Equal((decimal)taxable, result.Value!.TaxableIncome);
        Assert.Equal((decimal)taxOnSlabs, result.Value.TaxOnSlabs);
        Assert.Equal((decimal)rebate, result.Value.RebateAndRelief);
        Assert.Equal((decimal)totalTax, result.Value.TotalTax);
    }

    // ---------- Slab edges, using income that is not salaried so gross = taxable ----------

    [Theory]
    [InlineData(400000, 0, 0, 0)]
    [InlineData(800000, 20000, 20000, 0)]
    [InlineData(1200000, 60000, 60000, 0)]       // the last rupee that gets the full rebate
    [InlineData(1205000, 60750, 55750, 5200)]    // marginal relief
    [InlineData(1270000, 70500, 500, 72800)]     // relief nearly used up
    [InlineData(1275000, 71250, 0, 74100)]       // no relief any more
    [InlineData(1600000, 120000, 0, 124800)]
    [InlineData(2000000, 200000, 0, 208000)]
    [InlineData(2400000, 300000, 0, 312000)]
    [InlineData(2500000, 330000, 0, 343200)]
    [InlineData(5000000, 1080000, 0, 1123200)]   // the largest income covered
    public void Slab_Edges_Match_Hand_Checked_Values(double taxable, double taxOnSlabs, double rebate, double totalTax)
    {
        var result = _calculator.EstimateNewRegime((decimal)taxable, isSalaried: false);

        Assert.True(result.IsSuccess);
        Assert.Equal((decimal)taxOnSlabs, result.Value!.TaxOnSlabs);
        Assert.Equal((decimal)rebate, result.Value.RebateAndRelief);
        Assert.Equal((decimal)totalTax, result.Value.TotalTax);
    }

    [Fact]
    public void Estimate_Reports_Every_Part_Of_The_Result()
    {
        var result = _calculator.EstimateNewRegime(1500000m);

        Assert.Equal("2026-27", result.Value!.FinancialYear);
        Assert.Equal(1500000m, result.Value.GrossIncome);
        Assert.Equal(75000m, result.Value.StandardDeduction);
        Assert.Equal(93750m, result.Value.TaxAfterRebate);
        Assert.Equal(3750m, result.Value.Cess);
        Assert.Equal(97500m, result.Value.TotalTax);
        Assert.Equal(6.5m, result.Value.EffectiveRatePercent);
    }

    [Fact]
    public void The_Effective_Rate_Is_Rounded_To_Two_Places()
    {
        var result = _calculator.EstimateNewRegime(2500000m);

        Assert.Equal(12.79m, result.Value!.EffectiveRatePercent);   // 319800 / 2500000
    }

    // ---------- Breakdown ----------

    [Fact]
    public void Breakdown_Lists_The_Slabs_The_Income_Reaches()
    {
        var result = _calculator.EstimateNewRegime(1500000m);   // taxable 14,25,000

        var rows = result.Value!.Breakdown;
        Assert.Equal(4, rows.Count);
        Assert.Equal(new SlabTax(1200000m, 1600000m, 15m, 225000m, 33750m), rows[3]);
    }

    [Fact]
    public void The_Last_Slab_Has_No_Upper_Limit()
    {
        var result = _calculator.EstimateNewRegime(2500000m);   // taxable 24,25,000

        var rows = result.Value!.Breakdown;
        Assert.Equal(7, rows.Count);
        Assert.Equal(new SlabTax(2400000m, null, 30m, 25000m, 7500m), rows[^1]);
    }

    [Fact]
    public void Breakdown_Taxes_Add_Up_To_The_Slab_Tax()
    {
        var result = _calculator.EstimateNewRegime(1999999m);

        Assert.Equal(result.Value!.TaxOnSlabs, result.Value.Breakdown.Sum(row => row.Tax));
    }

    // ---------- Invalid input ----------

    [Fact]
    public void A_Negative_Income_Is_Invalid()
    {
        var result = _calculator.EstimateNewRegime(-1m);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Theory]
    [InlineData(5000001, false)]   // taxable income just over the limit
    [InlineData(5075001, true)]    // salaried: 75,000 is deducted first
    public void Taxable_Income_Above_The_Limit_Is_Invalid(double gross, bool salaried)
    {
        var result = _calculator.EstimateNewRegime((decimal)gross, salaried);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Fact]
    public void Taxable_Income_Exactly_At_The_Limit_Is_Accepted()
    {
        Assert.True(_calculator.EstimateNewRegime(5075000m).IsSuccess);
    }

    // ---------- Rules are data ----------

    [Fact]
    public void The_Slab_Table_Can_Be_Replaced()
    {
        var rules = new IncomeTaxRules("Test", 0m, 0m, 0m, 0m, 10_000_000m, new[] { new TaxSlab(0m, 10m) });
        var calculator = new IncomeTaxCalculator(rules);

        var result = calculator.EstimateNewRegime(100000m, isSalaried: false);

        Assert.Equal(10000m, result.Value!.TotalTax);
        Assert.Equal("Test", result.Value.FinancialYear);
    }

    [Fact]
    public void The_Default_Rules_Are_The_Current_New_Regime()
    {
        Assert.Same(IncomeTaxRules.NewRegime202627, _calculator.Rules);
    }

    public static TheoryData<IncomeTaxRules> BrokenRules()
    {
        var current = IncomeTaxRules.NewRegime202627;

        return new TheoryData<IncomeTaxRules>
        {
            current with { Slabs = Array.Empty<TaxSlab>() },                                   // no slabs
            current with { Slabs = new[] { new TaxSlab(1000m, 5m) } },                         // does not start at 0
            current with { Slabs = new[] { new TaxSlab(0m, 5m), new TaxSlab(500m, 10m), new TaxSlab(500m, 20m) } }, // not ascending
            current with { Slabs = new[] { new TaxSlab(0m, -1m) } },                           // negative rate
            current with { Slabs = new[] { new TaxSlab(0m, 101m) } },                          // rate above 100
            current with { CessPercent = -1m },
        };
    }

    [Theory]
    [MemberData(nameof(BrokenRules))]
    public void Broken_Rules_Are_A_Programming_Error(IncomeTaxRules rules)
    {
        Assert.Throws<ArgumentException>(() => new IncomeTaxCalculator(rules));
    }

    // ---------- Rounding and construction ----------

    [Fact]
    public void Tax_Is_Rounded_To_Two_Places_By_Default()
    {
        var result = _calculator.EstimateNewRegime(1300001m, isSalaried: false);

        Assert.Equal(75000.15m, result.Value!.TaxOnSlabs);
        Assert.Equal(3000.01m, result.Value.Cess);
        Assert.Equal(78000.16m, result.Value.TotalTax);
    }

    [Fact]
    public void Tax_Is_Rounded_To_The_Chosen_Decimal_Places()
    {
        var whole = new IncomeTaxCalculator(decimalPlaces: 0);

        var result = whole.EstimateNewRegime(1300001m, isSalaried: false);

        Assert.Equal(75000m, result.Value!.TaxOnSlabs);
        Assert.Equal(3000m, result.Value.Cess);
        Assert.Equal(78000m, result.Value.TotalTax);
    }

    [Fact]
    public void A_Negative_Number_Of_Decimal_Places_Is_A_Programming_Error()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IncomeTaxCalculator(decimalPlaces: -1));
    }
}