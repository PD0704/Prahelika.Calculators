namespace Prahelika.Calculators.Core.Statistics;

/// <summary>Everything about a list of numbers in one result.</summary>
/// <param name="Count">How many numbers there are.</param>
/// <param name="Sum">The total. It is not rounded.</param>
/// <param name="Minimum">The smallest number.</param>
/// <param name="Maximum">The largest number.</param>
/// <param name="Mean">The average.</param>
/// <param name="Median">The middle value.</param>
/// <param name="Modes">The most frequent values, smallest first. Empty when no value repeats.</param>
/// <param name="PopulationStandardDeviation">The standard deviation of the whole group (divides by n).</param>
/// <param name="SampleStandardDeviation">The standard deviation of a sample (divides by n - 1). Null when there is only one number.</param>
public sealed record StatisticsSummary(
    int Count,
    decimal Sum,
    decimal Minimum,
    decimal Maximum,
    decimal Mean,
    decimal Median,
    IReadOnlyList<decimal> Modes,
    decimal PopulationStandardDeviation,
    decimal? SampleStandardDeviation);