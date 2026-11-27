using System.Globalization;
using Prahelika.Calculators.Core.Statistics;

namespace Prahelika.Calculators.Core.Tests.Statistics;

public class StatisticsCalculatorTests
{
    private readonly StatisticsCalculator _calculator = new();

    // "1,2,2" becomes { 1, 2, 2 }. An empty text becomes an empty list.
    private static decimal[] Numbers(string text) =>
        text.Length == 0
            ? Array.Empty<decimal>()
            : text.Split(',').Select(part => decimal.Parse(part, CultureInfo.InvariantCulture)).ToArray();

    private static decimal Dec(string text) => decimal.Parse(text, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("2,4,4,4,5,5,7,9", "5")]
    [InlineData("1,2,3,4", "2.5")]
    [InlineData("-5,5", "0")]
    [InlineData("1,2,2", "1.67")]      // 1.6666... rounded to 2 places
    [InlineData("7", "7")]
    public void Mean_Is_The_Average(string input, string expected)
    {
        var result = _calculator.Mean(Numbers(input));

        Assert.True(result.IsSuccess);
        Assert.Equal(Dec(expected), result.Value);
    }

    [Theory]
    [InlineData("9,1,5", "5")]               // the list is sorted first
    [InlineData("1,2,3,4", "2.5")]           // even count: average of the middle two
    [InlineData("2,4,4,4,5,5,7,9", "4.5")]
    [InlineData("3,1,2,2.5", "2.25")]
    [InlineData("7", "7")]
    public void Median_Is_The_Middle_Value(string input, string expected)
    {
        var result = _calculator.Median(Numbers(input));

        Assert.True(result.IsSuccess);
        Assert.Equal(Dec(expected), result.Value);
    }

    [Fact]
    public void Median_Does_Not_Change_The_List_It_Is_Given()
    {
        decimal[] values = { 3m, 1m, 2m };

        _calculator.Median(values);

        Assert.Equal(new[] { 3m, 1m, 2m }, values);
    }

    [Theory]
    [InlineData("1,2,2,3", "2")]
    [InlineData("2,4,4,4,5,5,7,9", "4")]
    [InlineData("1,2,2,3,3", "2,3")]         // a tie returns every tied value
    [InlineData("3,1,3,1", "1,3")]           // smallest first
    [InlineData("1,2,3,4", "")]              // nothing repeats: no mode
    [InlineData("5", "")]
    public void Mode_Returns_The_Most_Frequent_Values(string input, string expected)
    {
        var result = _calculator.Mode(Numbers(input));

        Assert.True(result.IsSuccess);
        Assert.Equal(Numbers(expected), result.Value!.ToArray());
    }

    [Theory]
    [InlineData("2,4,4,4,5,5,7,9", "2")]
    [InlineData("1,2,3,4", "1.12")]
    [InlineData("1,2,2,3,3", "0.75")]
    [InlineData("-5,5", "5")]
    [InlineData("10,20,30", "8.16")]         // 8.16497 rounded down: the third decimal is 4
    [InlineData("5", "0")]                   // one number never varies
    public void Population_Standard_Deviation_Divides_By_N(string input, string expected)
    {
        var result = _calculator.PopulationStandardDeviation(Numbers(input));

        Assert.True(result.IsSuccess);
        Assert.Equal(Dec(expected), result.Value);
    }

    [Theory]
    [InlineData("2,4,4,4,5,5,7,9", "2.14")]
    [InlineData("1,2,3,4", "1.29")]
    [InlineData("1,2,2,3,3", "0.84")]
    [InlineData("-5,5", "7.07")]
    [InlineData("10,20,30", "10")]
    public void Sample_Standard_Deviation_Divides_By_N_Minus_One(string input, string expected)
    {
        var result = _calculator.SampleStandardDeviation(Numbers(input));

        Assert.True(result.IsSuccess);
        Assert.Equal(Dec(expected), result.Value);
    }

    [Fact]
    public void Sample_Standard_Deviation_Needs_At_Least_Two_Numbers()
    {
        var result = _calculator.SampleStandardDeviation(Numbers("5"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Fact]
    public void More_Decimal_Places_Give_More_Digits()
    {
        var precise = new StatisticsCalculator(4);

        Assert.Equal(Dec("1.6667"), precise.Mean(Numbers("1,2,2")).Value);
        Assert.Equal(Dec("8.1650"), precise.PopulationStandardDeviation(Numbers("10,20,30")).Value);
    }

    [Fact]
    public void Zero_Decimal_Places_Round_To_Whole_Numbers()
    {
        var whole = new StatisticsCalculator(0);

        Assert.Equal(2m, whole.Mean(Numbers("1,2,2")).Value);
        Assert.Equal(3m, whole.Median(Numbers("2,3")).Value);   // 2.5 rounds away from zero
    }

    [Fact]
    public void Every_Calculation_Fails_On_An_Empty_List()
    {
        var empty = Array.Empty<decimal>();

        var results = new[]
        {
            _calculator.Mean(empty).Error,
            _calculator.Median(empty).Error,
            _calculator.Mode(empty).Error,
            _calculator.PopulationStandardDeviation(empty).Error,
            _calculator.SampleStandardDeviation(empty).Error,
            _calculator.Summarise(empty).Error,
        };

        Assert.All(results, error => Assert.Equal(CalcError.InvalidInput, error));
    }

    [Fact]
    public void Numbers_Too_Large_To_Add_Return_An_Overflow_Error()
    {
        var huge = new[] { decimal.MaxValue, decimal.MaxValue };

        Assert.Equal(CalcError.Overflow, _calculator.Mean(huge).Error);
        Assert.Equal(CalcError.Overflow, _calculator.Median(huge).Error);
        Assert.Equal(CalcError.Overflow, _calculator.Summarise(huge).Error);
    }

    [Fact]
    public void Standard_Deviation_Of_Huge_Numbers_Returns_An_Overflow_Error()
    {
        var huge = new[] { decimal.MaxValue, -decimal.MaxValue };   // the mean is 0, but squaring them overflows

        var result = _calculator.PopulationStandardDeviation(huge);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.Overflow, result.Error);
    }

    [Fact]
    public void A_Missing_List_Is_A_Programmer_Error()
    {
        Assert.Throws<ArgumentNullException>(() => _calculator.Mean(null!));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Invalid_Decimal_Places_Throw(int places)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new StatisticsCalculator(places));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void Decimal_Places_From_0_To_10_Are_Accepted(int places)
    {
        Assert.Equal(places, new StatisticsCalculator(places).DecimalPlaces);
    }

    [Fact]
    public void Summarise_Returns_Everything_At_Once()
    {
        var result = _calculator.Summarise(Numbers("2,4,4,4,5,5,7,9"));

        Assert.True(result.IsSuccess);
        var summary = result.Value!;
        Assert.Equal(8, summary.Count);
        Assert.Equal(40m, summary.Sum);
        Assert.Equal(2m, summary.Minimum);
        Assert.Equal(9m, summary.Maximum);
        Assert.Equal(5m, summary.Mean);
        Assert.Equal(4.5m, summary.Median);
        Assert.Equal(new[] { 4m }, summary.Modes.ToArray());
        Assert.Equal(2m, summary.PopulationStandardDeviation);
        Assert.Equal(2.14m, summary.SampleStandardDeviation);
    }

    [Fact]
    public void Summarise_Of_One_Number_Has_No_Sample_Standard_Deviation()
    {
        var result = _calculator.Summarise(Numbers("5"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Count);
        Assert.Equal(0m, result.Value.PopulationStandardDeviation);
        Assert.Null(result.Value.SampleStandardDeviation);
    }
}