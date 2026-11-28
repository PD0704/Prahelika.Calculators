using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Prahelika.Calculators.Core;
using Prahelika.Calculators.Core.Dates;
using Prahelika.Calculators.Web.Models;

namespace Prahelika.Calculators.Web.Pages;

public class DatesModel(IDateCalculator calculator, TimeProvider clock) : CalculatorPageModel
{
    // India has no daylight saving time, so a fixed offset of 5 hours 30 minutes is enough.
    private static readonly TimeSpan IndiaOffset = TimeSpan.FromHours(5.5);

    [BindProperty(SupportsGet = true)]
    public DateTool Tool { get; set; } = DateTool.Age;

    [BindProperty]
    public DateOnly? FirstDate { get; set; }

    [BindProperty]
    public DateOnly? SecondDate { get; set; }

    [BindProperty]
    public int? Amount { get; set; }

    [BindProperty]
    public DateUnit Unit { get; set; } = DateUnit.Months;

    public string FirstLabel => Tool switch
    {
        DateTool.Age => "Date of birth",
        DateTool.Between => "From",
        _ => "Date"
    };

    public string SecondLabel => Tool switch
    {
        DateTool.Age => "Age on (leave empty for today)",
        _ => "To"
    };

    public string Description => Tool switch
    {
        DateTool.Age => "Your age in years, months and days. Someone born on 29 February turns a year older on 28 February in years that are not leap years.",
        DateTool.Between => "How long it is from one date to another, in years, months, days and in total days. The order of the dates does not matter.",
        DateTool.Add => "Add months or days to a date, or go back with a minus sign. A month that is too short moves to its last day, so 31 January plus one month is 28 or 29 February.",
        _ => string.Empty
    };

    // Today's date in India, whatever the server's own time zone is.
    private DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(IndiaOffset).DateTime);

    public void OnGet()
    {
    }

    public void OnPost()
    {
        if (!ModelState.IsValid)
        {
            Fail("Please enter valid dates (day, month and year) and a whole number of months or days.");
            return;
        }

        switch (Tool)
        {
            case DateTool.Age:
                ShowAge();
                break;
            case DateTool.Between:
                ShowBetween();
                break;
            case DateTool.Add:
                ShowAdd();
                break;
        }
    }

    private void ShowAge()
    {
        if (FirstDate is null)
        {
            Fail("Please enter the date of birth.");
            return;
        }

        DateOnly asOf = SecondDate ?? Today;

        if (ShowDifference(calculator.Age(FirstDate.Value, asOf)))
        {
            Note = $"Age on {LongDate(asOf)}.";
        }
    }

    private void ShowBetween()
    {
        if (FirstDate is null || SecondDate is null)
        {
            Fail("Please enter both dates.");
            return;
        }

        if (ShowDifference(calculator.Between(FirstDate.Value, SecondDate.Value)))
        {
            Note = "Counted from the earlier date to the later one.";
        }
    }

    private void ShowAdd()
    {
        if (FirstDate is null)
        {
            Fail("Please enter the date.");
            return;
        }

        if (Amount is null)
        {
            Fail("Please enter how many months or days to add. Use a minus sign to go back.");
            return;
        }

        var result = Unit == DateUnit.Months
            ? calculator.AddMonths(FirstDate.Value, Amount.Value)
            : calculator.AddDays(FirstDate.Value, Amount.Value);

        if (!result.IsSuccess)
        {
            Fail(string.IsNullOrWhiteSpace(result.Message) ? "That calculation could not be done." : result.Message);
            return;
        }

        Display(ResultView.From(LongDate(result.Value), money: false));
        Note = $"{Amount.Value} {Unit.ToString().ToLowerInvariant()} from {LongDate(FirstDate.Value)}.";
    }

    // Shows "26 years, 10 months, 12 days" with the total days under it. Returns false when it showed an error.
    private bool ShowDifference(CalcResult<DateDifference> result)
    {
        if (!result.IsSuccess)
        {
            Fail(string.IsNullOrWhiteSpace(result.Message) ? "That calculation could not be done." : result.Message);
            return false;
        }

        DateDifference difference = result.Value!;
        var view = ResultView.From(Describe(difference), money: false);
        view.Details.Add(new("Total days", difference.TotalDays.ToString(CultureInfo.InvariantCulture)));
        Display(view);
        return true;
    }

    private static string Describe(DateDifference difference) =>
        $"{Plural(difference.Years, "year")}, {Plural(difference.Months, "month")}, {Plural(difference.Days, "day")}";

    private static string Plural(int count, string unit) => $"{count} {unit}{(count == 1 ? string.Empty : "s")}";

    private static string LongDate(DateOnly date) => date.ToString("dddd, d MMMM yyyy", CultureInfo.InvariantCulture);
}