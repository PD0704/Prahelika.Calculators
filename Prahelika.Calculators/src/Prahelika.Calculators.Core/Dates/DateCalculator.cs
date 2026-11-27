namespace Prahelika.Calculators.Core.Dates;

/// <summary>Default implementation of <see cref="IDateCalculator"/>, using <see cref="DateOnly"/>.</summary>
public sealed class DateCalculator : IDateCalculator
{
    private const int FirstYear = 1;
    private const int LastYear = 9999;

    /// <inheritdoc />
    public CalcResult<DateDifference> Age(DateOnly birthDate, DateOnly asOfDate)
    {
        if (birthDate > asOfDate)
        {
            return CalcResult<DateDifference>.Failure(
                CalcError.InvalidInput, "The birth date cannot be after the date you are measuring to.");
        }

        return CalcResult<DateDifference>.Success(Measure(birthDate, asOfDate));
    }

    /// <inheritdoc />
    public CalcResult<DateDifference> Between(DateOnly start, DateOnly end)
    {
        DateOnly earlier = start <= end ? start : end;
        DateOnly later = start <= end ? end : start;

        return CalcResult<DateDifference>.Success(Measure(earlier, later));
    }

    /// <inheritdoc />
    public CalcResult<DateOnly> AddMonths(DateOnly date, int months)
    {
        // Check the target year first, using long so a huge number of months cannot wrap around.
        long monthNumber = date.Year * 12L + (date.Month - 1) + months;
        long year = monthNumber >= 0 ? monthNumber / 12 : -1;

        if (year < FirstYear || year > LastYear)
        {
            return OutOfRange();
        }

        return CalcResult<DateOnly>.Success(date.AddMonths(months));
    }

    /// <inheritdoc />
    public CalcResult<DateOnly> AddDays(DateOnly date, int days)
    {
        try
        {
            return CalcResult<DateOnly>.Success(date.AddDays(days));
        }
        catch (ArgumentOutOfRangeException)
        {
            return OutOfRange();
        }
    }

    // earlier must not be after later. The whole months are the most months that can be added to the
    // earlier date without passing the later one. What is left over is the days.
    private static DateDifference Measure(DateOnly earlier, DateOnly later)
    {
        // Start from the difference in calendar months. It can be one too many, so step back once if it overshoots.
        int months = (later.Year - earlier.Year) * 12 + later.Month - earlier.Month;
        if (earlier.AddMonths(months) > later)
        {
            months--;
        }

        DateOnly anchor = earlier.AddMonths(months);

        return new DateDifference(
            Years: months / 12,
            Months: months % 12,
            Days: later.DayNumber - anchor.DayNumber,
            TotalDays: later.DayNumber - earlier.DayNumber);
    }

    private static CalcResult<DateOnly> OutOfRange() =>
        CalcResult<DateOnly>.Failure(CalcError.Overflow, "The result is outside the years 1 to 9999.");
}