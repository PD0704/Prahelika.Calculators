using System.Globalization;
using Prahelika.Calculators.Core;
using Prahelika.Calculators.Core.Statistics;

namespace Prahelika.Calculators.Api.Endpoints;

public static class StatisticsEndpoints
{
    public static IEndpointRouteBuilder MapStatisticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/statistics").WithTags("Statistics");

        // IStatisticsCalculator is supplied by dependency injection (registered in Program.cs).
        // The numbers are sent as one comma-separated list: values=2,4,4,4,5,5,7,9

        group.MapGet("/summary",
                (string values, IStatisticsCalculator calc) => WithNumbers(values, calc.Summarise))
            .WithSummary("Count, sum, smallest, largest, mean, median, mode and both standard deviations for a list of numbers. Send them as values=2,4,4,4,5,5,7,9.")
            .ProducesCalculation<StatisticsSummary>();

        group.MapGet("/mean",
                (string values, IStatisticsCalculator calc) => WithNumbers(values, calc.Mean))
            .WithSummary("The average of a list of numbers.")
            .ProducesCalculation<decimal>();

        group.MapGet("/median",
                (string values, IStatisticsCalculator calc) => WithNumbers(values, calc.Median))
            .WithSummary("The middle value. With an even count, the average of the two middle values.")
            .ProducesCalculation<decimal>();

        group.MapGet("/mode",
                (string values, IStatisticsCalculator calc) => WithNumbers(values, calc.Mode))
            .WithSummary("The most frequent value or values, smallest first. The list is empty when no value repeats.")
            .ProducesCalculation<IReadOnlyList<decimal>>();

        group.MapGet("/standard-deviation",
                (string values, IStatisticsCalculator calc, bool sample = false) =>
                    WithNumbers(values, numbers => sample
                        ? calc.SampleStandardDeviation(numbers)
                        : calc.PopulationStandardDeviation(numbers)))
            .WithSummary("Standard deviation. Population (the default) divides by n. Use sample=true for a sample (divides by n - 1, needs two numbers).")
            .ProducesCalculation<decimal>();

        return app;
    }

    // Reads the comma-separated list, then runs the calculation on it.
    private static IResult WithNumbers<T>(string text, Func<IReadOnlyList<decimal>, CalcResult<T>> calculate)
    {
        if (!TryParseNumbers(text, out decimal[] numbers, out string error))
        {
            return ResultMapping.ToProblem(CalcError.InvalidInput, error);
        }

        return calculate(numbers).ToHttpResult();
    }

    private static bool TryParseNumbers(string text, out decimal[] numbers, out string error)
    {
        numbers = [];
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Enter at least one number, separated by commas: values=2,4,4,4,5,5,7,9.";
            return false;
        }

        var parsed = new List<decimal>();

        foreach (string part in text.Split(','))
        {
            // Invariant culture, so 1.5 means one and a half whatever the server's language is.
            if (!decimal.TryParse(part.Trim(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out decimal number))
            {
                error = $"'{part.Trim()}' is not a number. Separate the numbers with commas: values=2,4,4,4,5,5,7,9.";
                return false;
            }

            parsed.Add(number);
        }

        numbers = parsed.ToArray();
        return true;
    }
}