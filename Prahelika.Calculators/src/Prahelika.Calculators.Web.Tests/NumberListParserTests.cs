using Prahelika.Calculators.Web.Models;

namespace Prahelika.Calculators.Web.Tests;

public class NumberListParserTests
{
    [Theory]
    [InlineData("2,4,4", new[] { 2.0, 4.0, 4.0 })]
    [InlineData("2 4 4", new[] { 2.0, 4.0, 4.0 })]
    [InlineData("2;4;4", new[] { 2.0, 4.0, 4.0 })]
    [InlineData("2\n4\r\n4", new[] { 2.0, 4.0, 4.0 })]
    [InlineData("  2 ,  4,\t4  ", new[] { 2.0, 4.0, 4.0 })]
    [InlineData("1,,2,", new[] { 1.0, 2.0 })]               // empty entries are skipped
    [InlineData("-5 5.5", new[] { -5.0, 5.5 })]              // negative numbers and decimal points
    [InlineData("7", new[] { 7.0 })]
    public void Reads_Numbers_Separated_By_Commas_Spaces_Or_New_Lines(string text, double[] expected)
    {
        bool ok = NumberListParser.TryParse(text, out decimal[] numbers, out string error);

        Assert.True(ok, error);
        Assert.Equal(expected.Select(x => (decimal)x), numbers);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" , ; ")]
    public void Asks_For_At_Least_One_Number(string? text)
    {
        bool ok = NumberListParser.TryParse(text, out decimal[] numbers, out string error);

        Assert.False(ok);
        Assert.Empty(numbers);
        Assert.Contains("at least one number", error);
    }

    [Theory]
    [InlineData("1, abc", "abc")]
    [InlineData("1 2 1.5.2", "1.5.2")]
    [InlineData("1e3", "1e3")]            // scientific notation is not accepted
    [InlineData("12abc", "12abc")]
    [InlineData("1 - 2", "-")]
    public void Names_The_Entry_That_Is_Not_A_Number(string text, string badEntry)
    {
        bool ok = NumberListParser.TryParse(text, out decimal[] numbers, out string error);

        Assert.False(ok);
        Assert.Empty(numbers);
        Assert.Contains(badEntry, error);
        Assert.Contains("is not a number", error);
    }

    [Fact]
    public void Uses_A_Dot_As_The_Decimal_Point_Whatever_The_Computer_Language_Is()
    {
        var original = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("de-DE");

            bool ok = NumberListParser.TryParse("1.5 2.25", out decimal[] numbers, out _);

            Assert.True(ok);
            Assert.Equal(new[] { 1.5m, 2.25m }, numbers);
        }
        catch (System.Globalization.CultureNotFoundException)
        {
            // A machine without culture data cannot run this check. The code uses the invariant culture either way.
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = original;
        }
    }
}