using Prahelika.Calculators.Core.Finance;

namespace Prahelika.Calculators.Core.Tests.Finance;

public class SavingsCalculatorTests
{
    private readonly SavingsCalculator _calculator = new();

    // ---------- Fixed deposit ----------

    [Theory]
    [InlineData(100000, 7, 12, CompoundingFrequency.Quarterly, 107185.90, 7185.90)]
    [InlineData(100000, 8, 24, CompoundingFrequency.Annually, 116640.00, 16640.00)]
    [InlineData(100000, 8, 24, CompoundingFrequency.Monthly, 117288.79, 17288.79)]
    [InlineData(50000, 6.5, 5, CompoundingFrequency.Quarterly, 51361.49, 1361.49)]   // part of a quarter
    [InlineData(100000, 0, 12, CompoundingFrequency.Quarterly, 100000.00, 0)]        // no interest
    public void FixedDeposit_Matches_Known_Values(
        double principal, double rate, int months, CompoundingFrequency frequency, double maturity, double interest)
    {
        var result = _calculator.FixedDeposit((decimal)principal, (decimal)rate, months, frequency);

        Assert.True(result.IsSuccess);
        Assert.Equal((decimal)principal, result.Value!.InvestedAmount);
        Assert.Equal((decimal)interest, result.Value.InterestEarned);
        Assert.Equal((decimal)maturity, result.Value.MaturityAmount);
    }

    [Fact]
    public void FixedDeposit_Compounds_Quarterly_By_Default()
    {
        var result = _calculator.FixedDeposit(100000m, 7m, 12);

        Assert.True(result.IsSuccess);
        Assert.Equal(107185.90m, result.Value!.MaturityAmount);
    }

    [Theory]
    [InlineData(0, 7, 12)]        // nothing deposited
    [InlineData(-1, 7, 12)]       // negative amount
    [InlineData(1000, -1, 12)]    // negative rate
    [InlineData(1000, 7, 0)]      // too few months
    [InlineData(1000, 7, 1201)]   // too many months
    public void FixedDeposit_Rejects_Invalid_Input(double principal, double rate, int months)
    {
        var result = _calculator.FixedDeposit((decimal)principal, (decimal)rate, months);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Fact]
    public void FixedDeposit_Rejects_An_Unknown_Frequency()
    {
        var result = _calculator.FixedDeposit(1000m, 7m, 12, (CompoundingFrequency)7);

        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Fact]
    public void FixedDeposit_Reports_Overflow_For_Huge_Amounts()
    {
        var result = _calculator.FixedDeposit(decimal.MaxValue, 10m, 12);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.Overflow, result.Error);
    }

    // ---------- Recurring deposit ----------

    [Theory]
    [InlineData(5000, 7.5, 12, 60000, 2478.46, 62478.46)]
    [InlineData(5000, 7.5, 60, 300000, 64448.61, 364448.61)]
    [InlineData(2000, 6.5, 10, 20000, 602.32, 20602.32)]
    [InlineData(1000, 0, 12, 12000, 0, 12000)]
    public void RecurringDeposit_Matches_Known_Values(
        double monthly, double rate, int months, double invested, double interest, double maturity)
    {
        var result = _calculator.RecurringDeposit((decimal)monthly, (decimal)rate, months);

        Assert.True(result.IsSuccess);
        Assert.Equal((decimal)invested, result.Value!.InvestedAmount);
        Assert.Equal((decimal)interest, result.Value.InterestEarned);
        Assert.Equal((decimal)maturity, result.Value.MaturityAmount);
    }

    [Theory]
    [InlineData(0, 7, 12)]
    [InlineData(-5, 7, 12)]
    [InlineData(1000, -1, 12)]
    [InlineData(1000, 7, 0)]
    [InlineData(1000, 7, 1201)]
    public void RecurringDeposit_Rejects_Invalid_Input(double monthly, double rate, int months)
    {
        var result = _calculator.RecurringDeposit((decimal)monthly, (decimal)rate, months);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Fact]
    public void RecurringDeposit_Reports_Overflow_For_Huge_Amounts()
    {
        var result = _calculator.RecurringDeposit(decimal.MaxValue, 10m, 12);

        Assert.Equal(CalcError.Overflow, result.Error);
    }

    // ---------- PPF ----------

    [Theory]
    [InlineData(150000, 7.1, 15, 2250000, 1818209.22, 4068209.22)]
    [InlineData(50000, 7.1, 20, 1000000, 1219429.39, 2219429.39)]
    [InlineData(12000, 7.1, 15, 180000, 145456.74, 325456.74)]
    [InlineData(1000, 0, 15, 15000, 0, 15000)]
    public void Ppf_Matches_Known_Values(
        double yearly, double rate, int years, double invested, double interest, double maturity)
    {
        var result = _calculator.Ppf((decimal)yearly, (decimal)rate, years);

        Assert.True(result.IsSuccess);
        Assert.Equal((decimal)invested, result.Value!.InvestedAmount);
        Assert.Equal((decimal)interest, result.Value.InterestEarned);
        Assert.Equal((decimal)maturity, result.Value.MaturityAmount);
    }

    [Theory]
    [InlineData(499, 7.1, 15)]       // below the minimum yearly deposit
    [InlineData(150001, 7.1, 15)]    // above the yearly limit
    [InlineData(1000, -1, 15)]       // negative rate
    [InlineData(1000, 7.1, 14)]      // shorter than the 15-year term
    [InlineData(1000, 7.1, 16)]      // longer terms come in blocks of 5
    [InlineData(1000, 7.1, 55)]      // above the 50-year limit
    public void Ppf_Rejects_Invalid_Input(double yearly, double rate, int years)
    {
        var result = _calculator.Ppf((decimal)yearly, (decimal)rate, years);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(20)]
    [InlineData(50)]
    public void Ppf_Accepts_The_Allowed_Terms(int years)
    {
        Assert.True(_calculator.Ppf(1000m, 7.1m, years).IsSuccess);
    }

    [Fact]
    public void Ppf_Reports_Overflow_For_Extreme_Rates()
    {
        var result = _calculator.Ppf(150000m, 1000m, 50);

        Assert.Equal(CalcError.Overflow, result.Error);
    }

    // ---------- Rounding and construction ----------

    [Fact]
    public void Amounts_Are_Rounded_To_The_Chosen_Decimal_Places()
    {
        var whole = new SavingsCalculator(0);

        var result = whole.FixedDeposit(100000m, 7m, 12);

        Assert.Equal(107186m, result.Value!.MaturityAmount);
        Assert.Equal(7186m, result.Value.InterestEarned);
    }

    [Fact]
    public void A_Negative_Number_Of_Decimal_Places_Is_A_Programming_Error()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SavingsCalculator(-1));
    }
}