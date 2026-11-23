using Prahelika.Calculators.Core.Finance;

namespace Prahelika.Calculators.Core.Tests.Finance;

public class LoanPlannerTests
{
    private readonly LoanPlanner _planner = new();

    // loan, rate, months, prepayment month, lump sum,
    // then: EMI, new months, months saved, lump sum applied, original interest, new interest, interest saved
    [Theory]
    [InlineData(1000000, 9, 240, 60, 200000, 8997.26, 174, 66, 200000, 1159342.12, 763766.45, 395575.67)]
    [InlineData(100000, 12, 12, 6, 20000, 8884.88, 10, 2, 20000, 6618.53, 5543.41, 1075.12)]
    [InlineData(500000, 8.5, 60, 12, 50000, 10258.27, 54, 6, 50000, 115495.89, 96744.51, 18751.38)]
    [InlineData(120000, 0, 12, 6, 30000, 10000, 9, 3, 30000, 0, 0, 0)]                      // no interest
    [InlineData(100000, 12, 12, 11, 1000, 8884.88, 12, 0, 1000, 6618.53, 6608.53, 10)]      // too small to save a month
    [InlineData(100000, 12, 12, 3, 1000000, 8884.88, 3, 9, 76108.02, 6618.53, 2762.66, 3855.87)] // closes the loan
    public void Prepay_Matches_Known_Values(
        double loan, double rate, int months, int prepaymentMonth, double lumpSum,
        double emi, int newMonths, int monthsSaved, double applied,
        double originalInterest, double newInterest, double interestSaved)
    {
        var result = _planner.Prepay((decimal)loan, (decimal)rate, months, prepaymentMonth, (decimal)lumpSum);

        Assert.True(result.IsSuccess);
        var value = result.Value!;
        Assert.Equal((decimal)emi, value.MonthlyPayment);
        Assert.Equal(months, value.OriginalMonths);
        Assert.Equal(newMonths, value.NewMonths);
        Assert.Equal(monthsSaved, value.MonthsSaved);
        Assert.Equal((decimal)applied, value.PrepaymentApplied);
        Assert.Equal((decimal)originalInterest, value.OriginalTotalInterest);
        Assert.Equal((decimal)newInterest, value.NewTotalInterest);
        Assert.Equal((decimal)interestSaved, value.InterestSaved);
    }

    [Fact]
    public void The_Original_Interest_Is_The_Sum_Of_The_Amortization_Schedule_Interest()
    {
        var schedule = new FinanceCalculator().AmortizationSchedule(1000000m, 9m, 240);

        var result = _planner.Prepay(1000000m, 9m, 240, 60, 200000m);

        Assert.Equal(schedule.Value!.Sum(row => row.InterestPaid), result.Value!.OriginalTotalInterest);
    }

    [Fact]
    public void A_Bigger_Prepayment_Never_Saves_Less()
    {
        decimal previousSaved = 0m;
        int previousMonths = 240;

        foreach (decimal lumpSum in new[] { 10000m, 50000m, 100000m, 300000m, 600000m })
        {
            var value = _planner.Prepay(1000000m, 9m, 240, 24, lumpSum).Value!;

            Assert.True(value.InterestSaved >= previousSaved);
            Assert.True(value.NewMonths <= previousMonths);
            Assert.Equal(value.OriginalTotalInterest - value.NewTotalInterest, value.InterestSaved);
            Assert.Equal(value.OriginalMonths - value.NewMonths, value.MonthsSaved);

            previousSaved = value.InterestSaved;
            previousMonths = value.NewMonths;
        }
    }

    [Fact]
    public void An_Earlier_Prepayment_Saves_More_Interest()
    {
        var early = _planner.Prepay(1000000m, 9m, 240, 12, 200000m).Value!;
        var late = _planner.Prepay(1000000m, 9m, 240, 120, 200000m).Value!;

        Assert.True(early.InterestSaved > late.InterestSaved);
    }

    [Theory]
    [InlineData(0, 12, 12, 6, 1000)]          // no loan
    [InlineData(-1, 12, 12, 6, 1000)]         // negative loan
    [InlineData(100000, -1, 12, 6, 1000)]     // negative rate
    [InlineData(100000, 12, 0, 6, 1000)]      // too few months
    [InlineData(100000, 12, 1201, 6, 1000)]   // too many months
    [InlineData(100000, 12, 12, 0, 1000)]     // prepayment before the first EMI
    [InlineData(100000, 12, 12, 12, 1000)]    // prepayment at the last EMI: nothing left to prepay
    [InlineData(100000, 12, 12, 13, 1000)]    // after the loan has ended
    [InlineData(100000, 12, 1, 1, 1000)]      // a one-month loan has nothing to prepay
    [InlineData(100000, 12, 12, 6, 0)]        // no lump sum
    [InlineData(100000, 12, 12, 6, -5)]       // negative lump sum
    public void Prepay_Rejects_Invalid_Input(double loan, double rate, int months, int prepaymentMonth, double lumpSum)
    {
        var result = _planner.Prepay((decimal)loan, (decimal)rate, months, prepaymentMonth, (decimal)lumpSum);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Fact]
    public void Prepay_Reports_Overflow_For_Huge_Loans()
    {
        var result = _planner.Prepay(79228162514264337593543950335m, 1000000m, 12, 6, 1000m);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.Overflow, result.Error);
    }

    [Fact]
    public void The_Decimal_Places_Are_Kept()
    {
        Assert.Equal(3, new LoanPlanner(3).DecimalPlaces);
    }

    [Fact]
    public void A_Negative_Number_Of_Decimal_Places_Is_A_Programming_Error()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LoanPlanner(-1));
    }
}