namespace Prahelika.Calculators.Core.Statistics;

/// <summary>Default implementation of <see cref="IStatisticsCalculator"/> using <see cref="decimal"/> arithmetic.</summary>
public sealed class StatisticsCalculator : IStatisticsCalculator
{
    private const string EmptyMessage = "Enter at least one number.";

    /// <param name="decimalPlaces">How many decimal places results are rounded to (0 to 10).</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="decimalPlaces"/> is not between 0 and 10.</exception>
    public StatisticsCalculator(int decimalPlaces = 2)
    {
        if (decimalPlaces is < 0 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces), decimalPlaces, "Decimal places must be between 0 and 10.");
        }

        DecimalPlaces = decimalPlaces;
    }

    /// <inheritdoc />
    public int DecimalPlaces { get; }

    /// <inheritdoc />
    public CalcResult<decimal> Mean(IReadOnlyList<decimal> values) =>
        Compute(values, 1, EmptyMessage, () => Round(MeanOf(values)));

    /// <inheritdoc />
    public CalcResult<decimal> Median(IReadOnlyList<decimal> values) =>
        Compute(values, 1, EmptyMessage, () => Round(MedianOf(values)));

    /// <inheritdoc />
    public CalcResult<IReadOnlyList<decimal>> Mode(IReadOnlyList<decimal> values) =>
        Compute(values, 1, EmptyMessage, () => ModesOf(values));

    /// <inheritdoc />
    public CalcResult<decimal> PopulationStandardDeviation(IReadOnlyList<decimal> values) =>
        Compute(values, 1, EmptyMessage, () => Round(StandardDeviationOf(values, isSample: false)));

    /// <inheritdoc />
    public CalcResult<decimal> SampleStandardDeviation(IReadOnlyList<decimal> values) =>
        Compute(values, 2, "The sample standard deviation needs at least two numbers.",
            () => Round(StandardDeviationOf(values, isSample: true)));

    /// <inheritdoc />
    public CalcResult<StatisticsSummary> Summarise(IReadOnlyList<decimal> values) =>
        Compute(values, 1, EmptyMessage, () => new StatisticsSummary(
            Count: values.Count,
            Sum: values.Sum(),
            Minimum: values.Min(),
            Maximum: values.Max(),
            Mean: Round(MeanOf(values)),
            Median: Round(MedianOf(values)),
            Modes: ModesOf(values),
            PopulationStandardDeviation: Round(StandardDeviationOf(values, isSample: false)),
            SampleStandardDeviation: values.Count >= 2 ? Round(StandardDeviationOf(values, isSample: true)) : null));

    // Checks the list, runs the calculation, and turns a .NET overflow into a failed result.
    private static CalcResult<T> Compute<T>(IReadOnlyList<decimal> values, int minimumCount, string tooFewMessage, Func<T> operation)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count < minimumCount)
        {
            return CalcResult<T>.Failure(CalcError.InvalidInput, tooFewMessage);
        }

        try
        {
            return CalcResult<T>.Success(operation());
        }
        catch (OverflowException)
        {
            return CalcResult<T>.Failure(CalcError.Overflow, "The numbers are too large to calculate with.");
        }
    }

    private decimal Round(decimal value) => Math.Round(value, DecimalPlaces, MidpointRounding.AwayFromZero);

    // The helpers below do not round. Rounding happens once, at the end, so errors do not pile up.
    private static decimal MeanOf(IReadOnlyList<decimal> values) => values.Sum() / values.Count;

    private static decimal MedianOf(IReadOnlyList<decimal> values)
    {
        decimal[] sorted = values.OrderBy(value => value).ToArray();   // a sorted copy: the caller's list stays as it was
        int middle = sorted.Length / 2;

        return sorted.Length % 2 == 1
            ? sorted[middle]
            : (sorted[middle - 1] + sorted[middle]) / 2m;
    }

    private static IReadOnlyList<decimal> ModesOf(IReadOnlyList<decimal> values)
    {
        var groups = values.GroupBy(value => value).ToList();
        int highest = groups.Max(group => group.Count());

        if (highest == 1)
        {
            return Array.Empty<decimal>();   // nothing repeats, so there is no mode
        }

        return groups.Where(group => group.Count() == highest)
                     .Select(group => group.Key)
                     .OrderBy(value => value)
                     .ToArray();
    }

    private static decimal StandardDeviationOf(IReadOnlyList<decimal> values, bool isSample)
    {
        decimal mean = MeanOf(values);
        decimal sumOfSquares = values.Sum(value => (value - mean) * (value - mean));
        decimal variance = sumOfSquares / (isSample ? values.Count - 1 : values.Count);

        // decimal has no square root, so use double for this one step.
        return (decimal)Math.Sqrt((double)variance);
    }
}