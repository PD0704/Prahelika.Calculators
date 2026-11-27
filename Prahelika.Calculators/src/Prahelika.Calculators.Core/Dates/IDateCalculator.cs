namespace Prahelika.Calculators.Core.Dates;

/// <summary>Age, the gap between two dates, and adding months or days to a date.</summary>
public interface IDateCalculator
{
    /// <summary>
    /// A person's age on a given date, in years, months and days. Pass today's date as <paramref name="asOfDate"/>
    /// for their age now. A birth date after <paramref name="asOfDate"/> is invalid.
    /// Months are counted by adding them to the birth date, and a month that is too short for the day
    /// moves to its last day (31 January plus one month is 28 February). So someone born on 29 February
    /// turns a year older on 28 February in years that are not leap years.
    /// </summary>
    CalcResult<DateDifference> Age(DateOnly birthDate, DateOnly asOfDate);

    /// <summary>
    /// The gap between two dates, in years, months, days and total days, counted the same way as <see cref="Age"/>.
    /// The order of the dates does not matter: the gap is always from the earlier date to the later one.
    /// </summary>
    CalcResult<DateDifference> Between(DateOnly start, DateOnly end);

    /// <summary>
    /// Adds months to a date (a negative number goes back). A month that is too short for the day
    /// moves to its last day: 31 January plus one month is 28 February, or 29 in a leap year.
    /// Fails with <see cref="CalcError.Overflow"/> if the result is outside the years 1 to 9999.
    /// </summary>
    CalcResult<DateOnly> AddMonths(DateOnly date, int months);

    /// <summary>
    /// Adds days to a date (a negative number goes back).
    /// Fails with <see cref="CalcError.Overflow"/> if the result is outside the years 1 to 9999.
    /// </summary>
    CalcResult<DateOnly> AddDays(DateOnly date, int days);
}