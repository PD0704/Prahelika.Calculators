using System.Globalization;
using Prahelika.Calculators.Core.Finance;
using Prahelika.Calculators.Web.Models;

namespace Prahelika.Calculators.Web.Tests;

public class LoanScheduleCsvTests
{
    private static readonly AmortizationRow[] TwoMonths =
    {
        new(1, 8884.88m, 7884.88m, 1000m, 92115.12m),
        new(2, 8884.88m, 7963.73m, 921.15m, 84151.39m),
    };

    [Fact]
    public void Build_Writes_A_Header_And_One_Line_Per_Month()
    {
        string csv = LoanScheduleCsv.Build(TwoMonths);

        Assert.Equal(
            "Month,Payment,Principal,Interest,Balance\r\n"
            + "1,8884.88,7884.88,1000.00,92115.12\r\n"
            + "2,8884.88,7963.73,921.15,84151.39\r\n",
            csv);
    }

    [Fact]
    public void Build_Always_Writes_Two_Decimals()
    {
        var rows = new[] { new AmortizationRow(1, 100m, 100m, 0m, 0m) };

        Assert.Contains("1,100.00,100.00,0.00,0.00", LoanScheduleCsv.Build(rows));
    }

    [Fact]
    public void Build_With_No_Months_Has_Only_The_Header()
    {
        Assert.Equal("Month,Payment,Principal,Interest,Balance\r\n", LoanScheduleCsv.Build(Array.Empty<AmortizationRow>()));
    }

    [Fact]
    public void Build_Uses_Dots_Whatever_The_Computer_Language_Is()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");   // German writes 7884,88

            string csv = LoanScheduleCsv.Build(TwoMonths);

            Assert.Contains("7884.88", csv);
            Assert.DoesNotContain("7884,88", csv);
        }
        catch (CultureNotFoundException)
        {
            // A machine without culture data cannot run this check. The code uses the invariant culture either way.
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void ToBytes_Starts_With_The_UTF8_Marker_That_Excel_Looks_For()
    {
        byte[] bytes = LoanScheduleCsv.ToBytes("Month\r\n");

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
        Assert.Equal("Month\r\n", System.Text.Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3));
    }
}