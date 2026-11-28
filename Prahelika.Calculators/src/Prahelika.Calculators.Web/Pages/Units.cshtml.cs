using Microsoft.AspNetCore.Mvc;
using Prahelika.Calculators.Core.Units;
using Prahelika.Calculators.Web.Models;

namespace Prahelika.Calculators.Web.Pages;

public class UnitsModel(IUnitConverter converter) : CalculatorPageModel
{
    [BindProperty(SupportsGet = true)]
    public UnitCategory Category { get; set; } = UnitCategory.Length;

    [BindProperty]
    public decimal? Value { get; set; }

    [BindProperty]
    public MeasurementUnit? From { get; set; }

    [BindProperty]
    public MeasurementUnit? To { get; set; }

    public IReadOnlyList<MeasurementUnit> UnitsInCategory => converter.UnitsIn(Category);

    public string Description => Category switch
    {
        UnitCategory.Temperature => "Celsius, Fahrenheit and Kelvin. Temperatures below absolute zero (-273.15 degrees Celsius) are not possible.",
        UnitCategory.Area => "Square units, hectares and acres.",
        UnitCategory.Volume => "Litres, cubic metres, and US fluid ounces and gallons.",
        UnitCategory.Weight => "Grams, kilograms, tonnes, ounces and pounds.",
        _ => "Metric and imperial lengths, from millimetres to miles."
    };

    public void OnGet()
    {
        // Start with the first two units of the category chosen in the tabs.
        From = UnitsInCategory[0];
        To = UnitsInCategory[1];
    }

    public void OnPost()
    {
        if (!ModelState.IsValid)
        {
            Fail("Please enter a valid number.");
            return;
        }

        if (Value is null)
        {
            Fail("Please enter the value to convert.");
            return;
        }

        if (From is null || To is null)
        {
            Fail("Please choose both units.");
            return;
        }

        var result = converter.Convert(Value.Value, From.Value, To.Value);
        Show(result);

        if (result.IsSuccess)
        {
            Note = $"{ResultView.Number(Value.Value)} {UnitNames.For(From.Value)} = "
                 + $"{ResultView.Number(result.Value)} {UnitNames.For(To.Value)}";
        }
    }
}