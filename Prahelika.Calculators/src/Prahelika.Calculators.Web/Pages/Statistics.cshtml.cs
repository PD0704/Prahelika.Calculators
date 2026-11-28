using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Prahelika.Calculators.Core.Statistics;
using Prahelika.Calculators.Web.Models;

namespace Prahelika.Calculators.Web.Pages;

public class StatisticsModel(IStatisticsCalculator calculator) : CalculatorPageModel
{
    [BindProperty]
    public string? Values { get; set; }

    public void OnGet()
    {
    }

    public void OnPost()
    {
        if (!NumberListParser.TryParse(Values, out decimal[] numbers, out string error))
        {
            Fail(error);
            return;
        }

        var result = calculator.Summarise(numbers);
        if (!result.IsSuccess)
        {
            Fail(string.IsNullOrWhiteSpace(result.Message) ? "That calculation could not be done." : result.Message);
            return;
        }

        Display(BuildView(result.Value!));
        Note = "Use the population standard deviation when your numbers are the whole group, "
             + "and the sample one when they are a sample taken from a bigger group.";
    }

    // The summary has a list inside it (the modes), so the page lays out the rows itself.
    private static ResultView BuildView(StatisticsSummary summary)
    {
        var view = new ResultView();

        void Add(string label, string value) => view.Details.Add(new(label, value));

        Add("Count", summary.Count.ToString(CultureInfo.InvariantCulture));
        Add("Sum", ResultView.Number(summary.Sum));
        Add("Minimum", ResultView.Number(summary.Minimum));
        Add("Maximum", ResultView.Number(summary.Maximum));
        Add("Mean", ResultView.Number(summary.Mean));
        Add("Median", ResultView.Number(summary.Median));
        Add("Mode", summary.Modes.Count == 0
            ? "None (no value repeats)"
            : string.Join(", ", summary.Modes.Select(ResultView.Number)));
        Add("Standard deviation (population)", ResultView.Number(summary.PopulationStandardDeviation));
        Add("Standard deviation (sample)", summary.SampleStandardDeviation is { } sample
            ? ResultView.Number(sample)
            : "Needs at least two numbers");

        return view;
    }
}