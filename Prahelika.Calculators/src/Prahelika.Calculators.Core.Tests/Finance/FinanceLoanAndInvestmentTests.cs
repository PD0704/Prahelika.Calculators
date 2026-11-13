using Prahelika.Calculators.Core.Finance;

namespace Prahelika.Calculators.Core.Tests.Finance;

public class FinanceLoanAndInvestmentTests
{
    private readonly FinanceCalculator _calculator = new();

    // Checks that a calculation succeeded and returns its value, so tests stay short.
    private static T Succeeded<T>(CalcResult<T> result)
    {
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        return result.Value!;
    }

    private static void AssertRow(
        AmortizationRow row, int month, decimal payment, decimal principal, decimal interest, decimal balance)
    {
        Assert.Equal(month, row.Month);
        Assert.Equal(payment, row.Payment);
        Assert.Equal(principal, row.PrincipalPaid);
        Assert.Equal(interest, row.InterestPaid);
        Assert.Equal(balance, row.RemainingBalance);
    }

    // ================= EMI =================
    // Known answers, e.g. a 100,000 loan at 12% for 12 months has an EMI of 8884.88.

    [Theory]
    [InlineData(100000, 12, 12, 8884.88, 106618.56, 6618.56)]
    [InlineData(1000000, 10, 12, 87915.89, 1054990.68, 54990.68)]
    [InlineData(500000, 8.5, 60, 10258.27, 615496.20, 115496.20)]
    [InlineData(2500000, 9, 240, 22493.15, 5398356.00, 2898356.00)]
    [InlineData(120000, 0, 12, 10000, 120000, 0)]
    public void Emi_ValidLoan_ReturnsPaymentTotalAndInterest(
        decimal loan, decimal rate, int months, decimal expectedEmi, decimal expectedTotal, decimal expectedInterest)
    {
        var value = Succeeded(_calculator.Emi(loan, rate, months));

        Assert.Equal(expectedEmi, value.MonthlyPayment);
        Assert.Equal(expectedTotal, value.TotalPayment);
        Assert.Equal(expectedInterest, value.TotalInterest);
    }

    [Fact]
    public void Emi_LongestAllowedLoan_Succeeds()
    {
        var result = _calculator.Emi(1000m, 10m, 1200);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(0, 10, 12)]        // loan must be above zero
    [InlineData(-1000, 10, 12)]
    [InlineData(1000, -1, 12)]     // rate cannot be negative
    [InlineData(1000, 10, 0)]      // months must be 1 to 1200
    [InlineData(1000, 10, -5)]
    [InlineData(1000, 10, 1201)]
    public void Emi_InvalidInput_ReturnsInvalidInput(decimal loan, decimal rate, int months)
    {
        var result = _calculator.Emi(loan, rate, months);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Fact]
    public void Emi_ResultTooLarge_ReturnsOverflow()
    {
        var result = _calculator.Emi(decimal.MaxValue, 1_000_000m, 12);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.Overflow, result.Error);
    }

    // ================= Amortisation schedule =================

    [Fact]
    public void AmortizationSchedule_KnownLoan_RowsMatch()
    {
        var rows = Succeeded(_calculator.AmortizationSchedule(100000m, 12m, 12));

        Assert.Equal(12, rows.Count);
        AssertRow(rows[0], 1, 8884.88m, 7884.88m, 1000.00m, 92115.12m);
        AssertRow(rows[1], 2, 8884.88m, 7963.73m, 921.15m, 84151.39m);
        // The last payment is a little smaller: it absorbs the rounding of all the earlier months.
        AssertRow(rows[11], 12, 8884.85m, 8796.88m, 87.97m, 0m);
    }

    [Fact]
    public void AmortizationSchedule_ZeroRate_SplitsEvenlyAndLastPaymentTakesTheRemainder()
    {
        var rows = Succeeded(_calculator.AmortizationSchedule(100000m, 0m, 3));

        AssertRow(rows[0], 1, 33333.33m, 33333.33m, 0m, 66666.67m);
        AssertRow(rows[1], 2, 33333.33m, 33333.33m, 0m, 33333.34m);
        AssertRow(rows[2], 3, 33333.34m, 33333.34m, 0m, 0m);
    }

    [Theory]
    [InlineData(100000, 12, 12)]
    [InlineData(500000, 8.5, 60)]
    [InlineData(2500000, 9, 240)]
    [InlineData(50000, 18, 36)]
    [InlineData(100000, 0, 3)]
    public void AmortizationSchedule_AnyLoan_RepaysExactlyTheLoanAmount(decimal loan, decimal rate, int months)
    {
        var rows = Succeeded(_calculator.AmortizationSchedule(loan, rate, months));
        var monthlyPayment = Succeeded(_calculator.Emi(loan, rate, months)).MonthlyPayment;

        Assert.Equal(months, rows.Count);
        Assert.Equal(loan, rows.Sum(row => row.PrincipalPaid));
        Assert.Equal(0m, rows[^1].RemainingBalance);

        for (int i = 0; i < rows.Count; i++)
        {
            Assert.Equal(i + 1, rows[i].Month);
            Assert.Equal(rows[i].PrincipalPaid + rows[i].InterestPaid, rows[i].Payment);
            Assert.True(rows[i].RemainingBalance >= 0m);

            if (i < rows.Count - 1)
            {
                Assert.Equal(monthlyPayment, rows[i].Payment);
            }
        }
    }

    [Fact]
    public void AmortizationSchedule_LongestAllowedLoan_HasOneRowPerMonth()
    {
        var rows = Succeeded(_calculator.AmortizationSchedule(1000m, 10m, 1200));

        Assert.Equal(1200, rows.Count);
    }

    [Theory]
    [InlineData(0, 10, 12)]
    [InlineData(-1000, 10, 12)]
    [InlineData(1000, -1, 12)]
    [InlineData(1000, 10, 0)]
    [InlineData(1000, 10, 1201)]
    public void AmortizationSchedule_InvalidInput_ReturnsInvalidInput(decimal loan, decimal rate, int months)
    {
        var result = _calculator.AmortizationSchedule(loan, rate, months);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    // ================= SIP =================

    [Theory]
    [InlineData(5000, 12, 120, 600000, 561695.38, 1161695.38)]
    [InlineData(1000, 12, 12, 12000, 809.33, 12809.33)]
    [InlineData(10000, 15, 60, 600000, 296816.89, 896816.89)]
    [InlineData(500, 12, 24, 12000, 1621.60, 13621.60)]
    [InlineData(1000, 0, 12, 12000, 0, 12000)]
    public void SipFutureValue_ValidInput_ReturnsInvestedReturnsAndTotal(
        decimal monthly, decimal rate, int months, decimal expectedInvested, decimal expectedReturns, decimal expectedFutureValue)
    {
        var value = Succeeded(_calculator.SipFutureValue(monthly, rate, months));

        Assert.Equal(expectedInvested, value.InvestedAmount);
        Assert.Equal(expectedReturns, value.EstimatedReturns);
        Assert.Equal(expectedFutureValue, value.FutureValue);
    }

    [Theory]
    [InlineData(0, 12, 12)]
    [InlineData(-500, 12, 12)]
    [InlineData(500, -1, 12)]
    [InlineData(500, 12, 0)]
    [InlineData(500, 12, 1201)]
    public void SipFutureValue_InvalidInput_ReturnsInvalidInput(decimal monthly, decimal rate, int months)
    {
        var result = _calculator.SipFutureValue(monthly, rate, months);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Fact]
    public void SipFutureValue_ResultTooLarge_ReturnsOverflow()
    {
        // 100% a year for 100 years is a number far beyond what decimal can hold.
        var result = _calculator.SipFutureValue(1000m, 100m, 1200);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.Overflow, result.Error);
    }

    // ================= ROI =================

    [Theory]
    [InlineData(1000, 1250, 250, 25)]
    [InlineData(2000, 1500, -500, -25)]       // a loss
    [InlineData(3000, 3100, 100, 3.33)]       // 3.3333...% rounded
    [InlineData(1000, 0, -1000, -100)]        // lost everything
    [InlineData(1000, 1000, 0, 0)]
    public void ReturnOnInvestment_ValidInput_ReturnsGainAndPercent(
        decimal invested, decimal finalValue, decimal expectedGain, decimal expectedPercent)
    {
        var value = Succeeded(_calculator.ReturnOnInvestment(invested, finalValue));

        Assert.Equal(expectedGain, value.Gain);
        Assert.Equal(expectedPercent, value.RoiPercent);
    }

    [Theory]
    [InlineData(0, 100)]       // cannot divide by an investment of zero
    [InlineData(-100, 100)]
    [InlineData(1000, -1)]
    public void ReturnOnInvestment_InvalidInput_ReturnsInvalidInput(decimal invested, decimal finalValue)
    {
        var result = _calculator.ReturnOnInvestment(invested, finalValue);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    // ================= GST =================

    [Theory]
    [InlineData(1000, 18, GstMode.Exclusive, 1000, 180, 1180)]
    [InlineData(250.50, 12, GstMode.Exclusive, 250.50, 30.06, 280.56)]
    [InlineData(500, 0, GstMode.Exclusive, 500, 0, 500)]
    [InlineData(1180, 18, GstMode.Inclusive, 1000, 180, 1180)]
    [InlineData(1000, 18, GstMode.Inclusive, 847.46, 152.54, 1000)]
    [InlineData(105, 5, GstMode.Inclusive, 100, 5, 105)]
    public void Gst_ValidInput_ReturnsNetGstAndTotal(
        decimal amount, decimal rate, GstMode mode, decimal expectedNet, decimal expectedGst, decimal expectedTotal)
    {
        var value = Succeeded(_calculator.Gst(amount, rate, mode));

        Assert.Equal(expectedNet, value.NetAmount);
        Assert.Equal(expectedGst, value.GstAmount);
        Assert.Equal(expectedTotal, value.TotalAmount);
    }

    [Fact]
    public void Gst_DefaultMode_IsExclusive()
    {
        var value = Succeeded(_calculator.Gst(1000m, 18m));

        Assert.Equal(180m, value.GstAmount);
        Assert.Equal(1180m, value.TotalAmount);
    }

    [Fact]
    public void Gst_ExactMidpoint_RoundsAwayFromZero()
    {
        // 999.99 x 5% = 49.9995 exactly, which rounds up to 50.00.
        var value = Succeeded(_calculator.Gst(999.99m, 5m, GstMode.Exclusive));

        Assert.Equal(50.00m, value.GstAmount);
        Assert.Equal(1049.99m, value.TotalAmount);
    }

    [Theory]
    [InlineData(-1, 18)]
    [InlineData(1000, -5)]
    public void Gst_NegativeInput_ReturnsInvalidInput(decimal amount, decimal rate)
    {
        var result = _calculator.Gst(amount, rate);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Fact]
    public void Gst_UndefinedMode_ReturnsInvalidInput()
    {
        var result = _calculator.Gst(1000m, 18m, (GstMode)99);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }
}