using Prahelika.Calculators.Core.Finance;

namespace Prahelika.Calculators.Core.Tests.Finance;

public class GstSplitCalculatorTests
{
    private readonly GstSplitCalculator _calculator = new();

    [Theory]
    [InlineData(1000, 18, GstMode.Exclusive, 1000, 90, 90, 180, 1180)]
    [InlineData(1180, 18, GstMode.Inclusive, 1000, 90, 90, 180, 1180)]
    [InlineData(2500, 5, GstMode.Exclusive, 2500, 62.5, 62.5, 125, 2625)]
    [InlineData(1, 1, GstMode.Exclusive, 1, 0.01, 0, 0.01, 1.01)]     // one paisa cannot split in two
    [InlineData(1.01, 1, GstMode.Inclusive, 1, 0.01, 0, 0.01, 1.01)]
    public void GstSplit_Matches_Known_Values(
        double amount, double rate, GstMode mode,
        double net, double cgst, double sgst, double totalGst, double total)
    {
        var result = _calculator.GstSplit((decimal)amount, (decimal)rate, mode);

        Assert.True(result.IsSuccess);
        Assert.Equal((decimal)net, result.Value!.NetAmount);
        Assert.Equal((decimal)cgst, result.Value.Cgst);
        Assert.Equal((decimal)sgst, result.Value.Sgst);
        Assert.Equal((decimal)totalGst, result.Value.TotalGst);
        Assert.Equal((decimal)total, result.Value.TotalAmount);
    }

    [Fact]
    public void GstSplit_Is_Exclusive_By_Default()
    {
        var result = _calculator.GstSplit(1000m, 18m);

        Assert.Equal(1180m, result.Value!.TotalAmount);
    }

    [Theory]
    [InlineData(2, GstMode.Exclusive)]
    [InlineData(2, GstMode.Inclusive)]
    [InlineData(0, GstMode.Exclusive)]
    [InlineData(0, GstMode.Inclusive)]
    public void GstSplit_Halves_Always_Add_Up(int decimalPlaces, GstMode mode)
    {
        var calculator = new GstSplitCalculator(decimalPlaces);

        foreach (decimal amount in new[] { 0.01m, 1m, 99.99m, 123.45m, 1005m, 98765.43m })
            foreach (decimal rate in new[] { 0.25m, 3m, 5m, 12m, 18m, 28m })
            {
                var result = calculator.GstSplit(amount, rate, mode);

                Assert.True(result.IsSuccess);
                Assert.Equal(result.Value!.TotalGst, result.Value.Cgst + result.Value.Sgst);
                Assert.Equal(result.Value.TotalAmount, result.Value.NetAmount + result.Value.TotalGst);
            }
    }

    [Theory]
    [InlineData(-1, 18, GstMode.Exclusive)]
    [InlineData(1000, -5, GstMode.Exclusive)]
    [InlineData(-1, 18, GstMode.Inclusive)]
    [InlineData(1000, -5, GstMode.Inclusive)]
    public void GstSplit_Fails_Exactly_When_Gst_Fails(double amount, double rate, GstMode mode)
    {
        var expected = new FinanceCalculator().Gst((decimal)amount, (decimal)rate, mode);

        var actual = _calculator.GstSplit((decimal)amount, (decimal)rate, mode);

        Assert.Equal(expected.IsSuccess, actual.IsSuccess);
        Assert.Equal(expected.Error, actual.Error);
    }

    [Fact]
    public void Amounts_Are_Rounded_To_The_Chosen_Decimal_Places()
    {
        var whole = new GstSplitCalculator(0);

        var result = whole.GstSplit(1005m, 18m);   // GST 180.9 rounds to 181; CGST 90.5 rounds to 91

        Assert.Equal(181m, result.Value!.TotalGst);
        Assert.Equal(91m, result.Value.Cgst);
        Assert.Equal(90m, result.Value.Sgst);
    }

    [Fact]
    public void A_Negative_Number_Of_Decimal_Places_Is_A_Programming_Error()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GstSplitCalculator(-1));
    }
}