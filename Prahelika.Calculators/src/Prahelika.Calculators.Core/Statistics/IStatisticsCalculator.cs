namespace Prahelika.Calculators.Core.Statistics;

/// <summary>Statistics for a list of numbers, using <see cref="decimal"/> arithmetic.</summary>
public interface IStatisticsCalculator
{
    /// <summary>The number of decimal places that results are rounded to.</summary>
    int DecimalPlaces { get; }

    /// <summary>The average. The list must have at least one number.</summary>
    CalcResult<decimal> Mean(IReadOnlyList<decimal> values);

    /// <summary>
    /// The middle value of the sorted list. With an even count it is the average of the two middle values.
    /// The list you pass in is not changed.
    /// </summary>
    CalcResult<decimal> Median(IReadOnlyList<decimal> values);

    /// <summary>
    /// The most frequent value or values, smallest first. All tied values are returned.
    /// The result is empty when no value appears more than once.
    /// </summary>
    CalcResult<IReadOnlyList<decimal>> Mode(IReadOnlyList<decimal> values);

    /// <summary>The standard deviation of a whole group: the square root of the average squared distance from the mean.</summary>
    CalcResult<decimal> PopulationStandardDeviation(IReadOnlyList<decimal> values);

    /// <summary>The standard deviation of a sample (divides by n - 1). Needs at least two numbers.</summary>
    CalcResult<decimal> SampleStandardDeviation(IReadOnlyList<decimal> values);

    /// <summary>Count, sum, smallest, largest, mean, median, mode and both standard deviations in one result.</summary>
    CalcResult<StatisticsSummary> Summarise(IReadOnlyList<decimal> values);
}