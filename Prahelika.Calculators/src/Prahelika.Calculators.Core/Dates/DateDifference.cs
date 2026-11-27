namespace Prahelika.Calculators.Core.Dates;

/// <summary>The gap between two dates, as whole years, months and days, and as a total number of days.</summary>
/// <param name="Years">Whole years.</param>
/// <param name="Months">Whole months left over after the years (0 to 11).</param>
/// <param name="Days">Days left over after the months (0 to 30).</param>
/// <param name="TotalDays">All the days between the two dates.</param>
public sealed record DateDifference(int Years, int Months, int Days, int TotalDays);