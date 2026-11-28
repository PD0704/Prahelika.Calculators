using Prahelika.Calculators.Web.Models;

namespace Prahelika.Calculators.Web.Tests;

public class ResultViewHistoryTests
{
    [Fact]
    public void A_Single_Value_Is_The_History_Text()
    {
        Assert.Equal("3.5", ResultView.From(3.5m, money: false).HistoryText);
    }

    [Fact]
    public void A_Record_Gives_Its_First_Two_Rows()
    {
        // Amounts under a lakh are grouped the same way everywhere, so this does not depend on the server's culture data.
        var view = ResultView.From(new { MonthlyPayment = 8884.88m, TotalPayment = 95000.50m, TotalInterest = 6618.53m }, money: true);

        Assert.Equal("Monthly Payment: 8,884.88; Total Payment: 95,000.50", view.HistoryText);
    }

    [Fact]
    public void A_Table_Gives_Its_Row_Count()
    {
        var rows = new[] { new { Month = 1 }, new { Month = 2 }, new { Month = 3 } };

        Assert.Equal("Table with 3 rows", ResultView.From(rows, money: false).HistoryText);
    }

    [Fact]
    public void A_Table_Of_One_Row_Is_Singular()
    {
        var rows = new[] { new { Month = 1 } };

        Assert.Equal("Table with 1 row", ResultView.From(rows, money: false).HistoryText);
    }

    [Fact]
    public void Nothing_Gives_An_Empty_Text()
    {
        Assert.Equal(string.Empty, ResultView.From(null, money: false).HistoryText);
    }
}