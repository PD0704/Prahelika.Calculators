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

    // ---------- Comparing two loans ----------

    [Fact]
    public void CompareLoans_Shows_Both_Loans_And_The_Cheaper_One()
    {
        var first = new LoanOffer(500000m, 10m, 60, ProcessingFee: 5000m);
        var second = new LoanOffer(500000m, 10.5m, 60);

        var result = _planner.CompareLoans(first, second);

        Assert.True(result.IsSuccess);
        Assert.Equal(new LoanOfferSummary(10623.52m, 637411.20m, 137411.20m, 5000m, 142411.20m), result.Value!.First);
        Assert.Equal(new LoanOfferSummary(10746.95m, 644817.00m, 144817.00m, 0m, 144817.00m), result.Value.Second);
        Assert.Equal(LoanChoice.First, result.Value.Cheaper);
        Assert.Equal(2405.80m, result.Value.CostDifference);
    }

    [Fact]
    public void A_Shorter_Loan_Has_A_Bigger_EMI_But_Costs_Less_Overall()
    {
        var shorter = new LoanOffer(500000m, 9m, 36);
        var longer = new LoanOffer(500000m, 9m, 60);

        var result = _planner.CompareLoans(shorter, longer);

        Assert.Equal(15899.87m, result.Value!.First.MonthlyPayment);
        Assert.Equal(10379.18m, result.Value.Second.MonthlyPayment);
        Assert.Equal(72395.32m, result.Value.First.TotalInterest);
        Assert.Equal(122750.80m, result.Value.Second.TotalInterest);
        Assert.Equal(LoanChoice.First, result.Value.Cheaper);
        Assert.Equal(50355.48m, result.Value.CostDifference);
    }

    [Fact]
    public void A_Lower_Rate_Does_Not_Win_When_The_Fee_Is_High_Enough()
    {
        var higherRate = new LoanOffer(1000000m, 9m, 240);
        var lowerRateWithFee = new LoanOffer(1000000m, 8.75m, 240, ProcessingFee: 50000m);

        var result = _planner.CompareLoans(higherRate, lowerRateWithFee);

        Assert.Equal(1159342.40m, result.Value!.First.TotalCost);
        Assert.Equal(1170906.40m, result.Value.Second.TotalCost);   // 1120906.40 interest + 50000 fee
        Assert.Equal(LoanChoice.First, result.Value.Cheaper);
        Assert.Equal(11564.00m, result.Value.CostDifference);
    }

    [Fact]
    public void The_Lower_Rate_Wins_When_The_Fee_Is_Small()
    {
        var higherRate = new LoanOffer(1000000m, 9m, 240);
        var lowerRateWithFee = new LoanOffer(1000000m, 8.75m, 240, ProcessingFee: 15000m);

        var result = _planner.CompareLoans(higherRate, lowerRateWithFee);

        Assert.Equal(LoanChoice.Second, result.Value!.Cheaper);
        Assert.Equal(23436.00m, result.Value.CostDifference);
    }

    [Fact]
    public void Identical_Loans_Are_The_Same()
    {
        var loan = new LoanOffer(1000000m, 8.5m, 240);

        var result = _planner.CompareLoans(loan, loan);

        Assert.Equal(LoanChoice.Same, result.Value!.Cheaper);
        Assert.Equal(0m, result.Value.CostDifference);
        Assert.Equal(8678.23m, result.Value.First.MonthlyPayment);
    }

    [Fact]
    public void A_Loan_Without_Interest_Costs_Nothing_Extra()
    {
        var result = _planner.CompareLoans(new LoanOffer(200000m, 0m, 10), new LoanOffer(200000m, 5m, 10));

        Assert.Equal(0m, result.Value!.First.TotalInterest);
        Assert.Equal(20000m, result.Value.First.MonthlyPayment);
        Assert.Equal(LoanChoice.First, result.Value.Cheaper);
        Assert.Equal(4611.90m, result.Value.CostDifference);
    }

    [Fact]
    public void Swapping_The_Loans_Swaps_The_Answer()
    {
        var a = new LoanOffer(500000m, 10m, 60, 5000m);
        var b = new LoanOffer(500000m, 10.5m, 60);

        var forward = _planner.CompareLoans(a, b).Value!;
        var backward = _planner.CompareLoans(b, a).Value!;

        Assert.Equal(LoanChoice.First, forward.Cheaper);
        Assert.Equal(LoanChoice.Second, backward.Cheaper);
        Assert.Equal(forward.CostDifference, backward.CostDifference);
        Assert.Equal(forward.First, backward.Second);
    }

    [Fact]
    public void The_Cost_Difference_Is_Never_Negative()
    {
        var result = _planner.CompareLoans(new LoanOffer(500000m, 12m, 60), new LoanOffer(500000m, 8m, 60));

        Assert.Equal(LoanChoice.Second, result.Value!.Cheaper);
        Assert.True(result.Value.CostDifference > 0m);
    }

    [Theory]
    [InlineData(0, 10, 60, 0, "First loan")]       // no loan amount
    [InlineData(500000, -1, 60, 0, "First loan")]  // negative rate
    [InlineData(500000, 10, 0, 0, "First loan")]   // too few months
    [InlineData(500000, 10, 1201, 0, "First loan")] // too many months
    [InlineData(500000, 10, 60, -1, "First loan")] // negative fee
    public void CompareLoans_Rejects_An_Invalid_First_Loan(
        double loan, double rate, int months, double fee, string expectedMessageStart)
    {
        var invalid = new LoanOffer((decimal)loan, (decimal)rate, months, (decimal)fee);

        var result = _planner.CompareLoans(invalid, new LoanOffer(500000m, 10m, 60));

        Assert.Equal(CalcError.InvalidInput, result.Error);
        Assert.Contains(expectedMessageStart, result.Message);
    }

    [Fact]
    public void CompareLoans_Says_Which_Loan_Is_Invalid_When_It_Is_The_Second()
    {
        var result = _planner.CompareLoans(new LoanOffer(500000m, 10m, 60), new LoanOffer(500000m, 10m, 0));

        Assert.Equal(CalcError.InvalidInput, result.Error);
        Assert.Contains("Second loan", result.Message);
    }

    [Fact]
    public void CompareLoans_Reports_Overflow_For_An_Enormous_Fee()
    {
        var huge = new LoanOffer(500000m, 10m, 60, ProcessingFee: decimal.MaxValue);

        var result = _planner.CompareLoans(huge, new LoanOffer(500000m, 10m, 60));

        Assert.Equal(CalcError.Overflow, result.Error);
    }

    [Fact]
    public void CompareLoans_Needs_Both_Loans()
    {
        var loan = new LoanOffer(500000m, 10m, 60);

        Assert.Throws<ArgumentNullException>(() => _planner.CompareLoans(null!, loan));
        Assert.Throws<ArgumentNullException>(() => _planner.CompareLoans(loan, null!));
    }

    // ---------- How much can I borrow? ----------

    [Theory]
    [InlineData(20000, 9, 240, 2222899.08, 4800000, 2577100.92)]
    [InlineData(8997.26, 9, 240, 1000000.04, 2159342.40, 1159342.36)]
    [InlineData(8884.88, 12, 12, 100000.01, 106618.56, 6618.55)]
    [InlineData(5000, 7.5, 60, 249526.54, 300000, 50473.46)]
    [InlineData(50000, 8.5, 360, 6502682.17, 18000000, 11497317.83)]
    [InlineData(250000, 10, 6, 1457204.29, 1500000, 42795.71)]
    [InlineData(10000, 12, 1, 9900.99, 10000, 99.01)]          // a one-month loan
    [InlineData(1, 24, 1200, 49.99, 1200, 1150.01)]            // a very long loan
    [InlineData(10000, 0, 12, 120000, 120000, 0)]              // no interest
    public void MaxLoanAmount_Matches_Known_Values(
        double payment, double rate, int months, double maxLoan, double totalPayment, double totalInterest)
    {
        var result = _planner.MaxLoanAmount((decimal)payment, (decimal)rate, months);

        Assert.True(result.IsSuccess);
        Assert.Equal((decimal)payment, result.Value!.MonthlyPayment);
        Assert.Equal((decimal)maxLoan, result.Value.MaxLoanAmount);
        Assert.Equal((decimal)totalPayment, result.Value.TotalPayment);
        Assert.Equal((decimal)totalInterest, result.Value.TotalInterest);
    }

    [Fact]
    public void MaxLoanAmount_Round_Trips_With_The_Emi_Calculator()
    {
        var finance = new FinanceCalculator();

        foreach (decimal payment in new[] { 1000m, 8884.88m, 12345.67m, 50000m })
            foreach (decimal rate in new[] { 0m, 6.5m, 9m, 12m, 18m })
                foreach (int months in new[] { 1, 12, 60, 240, 360 })
                {
                    var borrowing = _planner.MaxLoanAmount(payment, rate, months).Value!;
                    var emi = finance.Emi(borrowing.MaxLoanAmount, rate, months).Value!;

                    // Rounding down means the EMI never goes above the payment, and it is within a paisa of it.
                    Assert.True(emi.MonthlyPayment <= payment,
                        $"EMI {emi.MonthlyPayment} is above {payment} (rate {rate}, months {months}).");
                    Assert.True(payment - emi.MonthlyPayment <= 0.01m,
                        $"EMI {emi.MonthlyPayment} is too far below {payment} (rate {rate}, months {months}).");
                }
    }

    [Fact]
    public void A_Longer_Term_Allows_A_Bigger_Loan()
    {
        decimal shorter = _planner.MaxLoanAmount(20000m, 9m, 120).Value!.MaxLoanAmount;
        decimal longer = _planner.MaxLoanAmount(20000m, 9m, 240).Value!.MaxLoanAmount;

        Assert.True(longer > shorter);
    }

    [Fact]
    public void A_Higher_Rate_Allows_A_Smaller_Loan()
    {
        decimal cheap = _planner.MaxLoanAmount(20000m, 8m, 240).Value!.MaxLoanAmount;
        decimal dear = _planner.MaxLoanAmount(20000m, 12m, 240).Value!.MaxLoanAmount;

        Assert.True(cheap > dear);
    }

    [Fact]
    public void MaxLoanAmount_Is_Rounded_Down_To_The_Chosen_Decimal_Places()
    {
        var whole = new LoanPlanner(0);

        var result = whole.MaxLoanAmount(20000m, 9m, 240);

        Assert.Equal(2222899m, result.Value!.MaxLoanAmount);     // 2222899.08 rounded down
        Assert.Equal(2577101m, result.Value.TotalInterest);
    }

    [Theory]
    [InlineData(0, 9, 240)]       // nothing affordable
    [InlineData(-100, 9, 240)]    // negative payment
    [InlineData(20000, -1, 240)]  // negative rate
    [InlineData(20000, 9, 0)]     // too few months
    [InlineData(20000, 9, 1201)]  // too many months
    public void MaxLoanAmount_Rejects_Invalid_Input(double payment, double rate, int months)
    {
        var result = _planner.MaxLoanAmount((decimal)payment, (decimal)rate, months);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void MaxLoanAmount_Reports_Overflow_For_Huge_Payments(double rate)
    {
        var result = _planner.MaxLoanAmount(decimal.MaxValue, (decimal)rate, 12);

        Assert.Equal(CalcError.Overflow, result.Error);
    }
}