using System.Globalization;
using Prahelika.Calculators.Core.Dates;

namespace Prahelika.Calculators.Core.Tests.Dates;

public class DateCalculatorTests
{
    private readonly DateCalculator _calculator = new();

    // Dates cannot be written in [InlineData], so they are written as "2024-02-29" text.
    private static DateOnly D(string text) => DateOnly.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("2000-01-15", "2026-11-27", 26, 10, 12, 9813)]
    [InlineData("1990-05-20", "2026-05-20", 36, 0, 0, 13149)]     // exactly on the birthday
    [InlineData("1990-05-20", "2026-05-19", 35, 11, 29, 13148)]   // the day before the birthday
    [InlineData("2000-02-29", "2001-02-28", 1, 0, 0, 365)]        // a leap-day birth turns 1 on 28 February
    [InlineData("2000-02-29", "2001-02-27", 0, 11, 29, 364)]
    [InlineData("2000-02-29", "2004-02-29", 4, 0, 0, 1461)]
    [InlineData("2025-01-31", "2025-02-28", 0, 1, 0, 28)]         // month end: 31 Jan + 1 month is 28 Feb
    [InlineData("2025-01-31", "2025-02-27", 0, 0, 27, 27)]
    [InlineData("2024-01-31", "2024-03-01", 0, 1, 1, 30)]
    [InlineData("2026-11-27", "2026-11-27", 0, 0, 0, 0)]          // the same day
    [InlineData("2024-01-01", "2024-12-31", 0, 11, 30, 365)]
    [InlineData("2023-01-01", "2024-01-01", 1, 0, 0, 365)]        // a normal year has 365 days
    [InlineData("2024-01-01", "2025-01-01", 1, 0, 0, 366)]        // a leap year has 366
    [InlineData("2024-02-28", "2024-03-01", 0, 0, 2, 2)]          // 29 February exists
    [InlineData("2023-02-28", "2023-03-01", 0, 0, 1, 1)]          // it does not
    [InlineData("2025-12-31", "2026-01-01", 0, 0, 1, 1)]          // across a year end
    public void Age_And_Between_Split_A_Gap_Into_Years_Months_And_Days(
        string start, string end, int years, int months, int days, int totalDays)
    {
        var expected = new DateDifference(years, months, days, totalDays);

        var age = _calculator.Age(D(start), D(end));
        var between = _calculator.Between(D(start), D(end));

        Assert.True(age.IsSuccess);
        Assert.Equal(expected, age.Value);
        Assert.True(between.IsSuccess);
        Assert.Equal(expected, between.Value);
    }

    [Fact]
    public void Between_Gives_The_Same_Answer_In_Either_Order()
    {
        var forwards = _calculator.Between(D("2000-01-15"), D("2026-11-27"));
        var backwards = _calculator.Between(D("2026-11-27"), D("2000-01-15"));

        Assert.True(backwards.IsSuccess);
        Assert.Equal(forwards.Value, backwards.Value);
    }

    [Fact]
    public void Age_Before_The_Birth_Date_Is_Invalid()
    {
        var result = _calculator.Age(D("2026-11-28"), D("2026-11-27"));

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    // Rebuilding the end date from the answer proves the split is consistent, for thousands of start and end dates
    // (leap years, month ends and year ends included).
    [Fact]
    public void Adding_The_Answer_Back_To_The_Start_Gives_The_End_Date()
    {
        var first = new DateOnly(2023, 1, 1);

        for (int startOffset = 0; startOffset < 1100; startOffset += 3)
        {
            DateOnly start = first.AddDays(startOffset);

            for (int gap = 0; gap < 1200; gap += 17)
            {
                DateOnly end = start.AddDays(gap);

                DateDifference difference = _calculator.Between(start, end).Value!;

                DateOnly rebuilt = start.AddMonths(difference.Years * 12 + difference.Months).AddDays(difference.Days);
                Assert.True(rebuilt == end, $"{start} to {end} gave {difference}, which rebuilds to {rebuilt}.");
                Assert.InRange(difference.Months, 0, 11);
                Assert.InRange(difference.Days, 0, 30);
                Assert.Equal(gap, difference.TotalDays);
            }
        }
    }

    [Theory]
    [InlineData("2024-01-31", 1, "2024-02-29")]    // a leap year
    [InlineData("2025-01-31", 1, "2025-02-28")]    // not a leap year
    [InlineData("2024-03-31", -1, "2024-02-29")]
    [InlineData("2024-02-29", 12, "2025-02-28")]
    [InlineData("2024-02-29", 48, "2028-02-29")]
    [InlineData("2024-11-30", 3, "2025-02-28")]
    [InlineData("2026-11-27", 1, "2026-12-27")]
    [InlineData("2026-11-27", 2, "2027-01-27")]    // across a year end
    [InlineData("2026-01-31", -2, "2025-11-30")]
    [InlineData("2024-05-31", -3, "2024-02-29")]
    [InlineData("2026-11-27", 0, "2026-11-27")]
    public void Add_Months_Moves_To_The_Last_Day_Of_A_Shorter_Month(string date, int months, string expected)
    {
        var result = _calculator.AddMonths(D(date), months);

        Assert.True(result.IsSuccess);
        Assert.Equal(D(expected), result.Value);
    }

    [Theory]
    [InlineData("2024-02-28", 2, "2024-03-01")]
    [InlineData("2023-02-28", 1, "2023-03-01")]
    [InlineData("2024-03-01", -1, "2024-02-29")]
    [InlineData("2026-12-31", 1, "2027-01-01")]
    [InlineData("2026-11-27", 30, "2026-12-27")]
    [InlineData("2026-11-27", 0, "2026-11-27")]
    public void Add_Days_Counts_Real_Calendar_Days(string date, int days, string expected)
    {
        var result = _calculator.AddDays(D(date), days);

        Assert.True(result.IsSuccess);
        Assert.Equal(D(expected), result.Value);
    }

    [Fact]
    public void Add_Months_Outside_The_Supported_Years_Returns_An_Overflow_Error()
    {
        Assert.Equal(CalcError.Overflow, _calculator.AddMonths(DateOnly.MaxValue, 1).Error);
        Assert.Equal(CalcError.Overflow, _calculator.AddMonths(DateOnly.MinValue, -1).Error);
        Assert.Equal(CalcError.Overflow, _calculator.AddMonths(D("2026-11-27"), 200000).Error);
        Assert.Equal(CalcError.Overflow, _calculator.AddMonths(D("2026-11-27"), int.MaxValue).Error);
        Assert.Equal(CalcError.Overflow, _calculator.AddMonths(D("2026-11-27"), int.MinValue).Error);
    }

    [Fact]
    public void Add_Days_Outside_The_Supported_Years_Returns_An_Overflow_Error()
    {
        Assert.Equal(CalcError.Overflow, _calculator.AddDays(DateOnly.MaxValue, 1).Error);
        Assert.Equal(CalcError.Overflow, _calculator.AddDays(DateOnly.MinValue, -1).Error);
        Assert.Equal(CalcError.Overflow, _calculator.AddDays(D("2026-11-27"), int.MaxValue).Error);
    }

    [Fact]
    public void The_Largest_And_Smallest_Dates_Work_As_Long_As_The_Result_Stays_In_Range()
    {
        Assert.Equal(DateOnly.MaxValue.DayNumber - DateOnly.MinValue.DayNumber,
            _calculator.Between(DateOnly.MinValue, DateOnly.MaxValue).Value!.TotalDays);
        Assert.Equal(DateOnly.MaxValue, _calculator.AddDays(DateOnly.MaxValue, 0).Value);
    }
}