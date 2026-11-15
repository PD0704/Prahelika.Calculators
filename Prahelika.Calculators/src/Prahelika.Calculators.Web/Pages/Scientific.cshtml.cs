using Microsoft.AspNetCore.Mvc;
using Prahelika.Calculators.Core.Scientific;
using Prahelika.Calculators.Web.Models;

namespace Prahelika.Calculators.Web.Pages;

public class ScientificModel(IScientificCalculator calculator) : CalculatorPageModel
{
    [BindProperty]
    public ScientificOperation Operation { get; set; } = ScientificOperation.Power;

    [BindProperty]
    public double? X { get; set; }

    [BindProperty]
    public double? Y { get; set; }

    [BindProperty]
    public AngleUnit Unit { get; set; } = AngleUnit.Radians;

    public void OnGet()
    {
    }

    public void OnPost()
    {
        if (!ModelState.IsValid)
        {
            Fail("Please enter valid numbers.");
            return;
        }

        if (X is null)
        {
            Fail("Please enter the value (X).");
            return;
        }

        bool needsY = Operation is ScientificOperation.Power
            or ScientificOperation.NthRoot
            or ScientificOperation.Log;

        if (needsY && Y is null)
        {
            Fail("This operation also needs the second value (Y).");
            return;
        }

        double x = X.Value;
        double y = Y ?? 0;
        string unitName = Unit.ToString().ToLowerInvariant();
        // Each request gets a calculator that uses the unit the user picked (same approach as the API).
        var trig = new ScientificCalculator(Unit);

        switch (Operation)
        {
            case ScientificOperation.Power:
                Show(calculator.Power(x, y));
                break;
            case ScientificOperation.SquareRoot:
                Show(calculator.SquareRoot(x));
                break;
            case ScientificOperation.NthRoot:
                if (y % 1 != 0 || Math.Abs(y) > int.MaxValue)
                {
                    Fail("The root (Y) must be a whole number, for example 3 for a cube root.");
                    break;
                }
                Show(calculator.NthRoot(x, (int)y));
                break;
            case ScientificOperation.NaturalLog:
                Show(calculator.NaturalLog(x));
                break;
            case ScientificOperation.Log10:
                Show(calculator.Log10(x));
                break;
            case ScientificOperation.Log:
                Show(calculator.Log(x, y));
                break;
            case ScientificOperation.Sin:
                Show(trig.Sin(x));
                Note = $"The angle was treated as {unitName}.";
                break;
            case ScientificOperation.Cos:
                Show(trig.Cos(x));
                Note = $"The angle was treated as {unitName}.";
                break;
            case ScientificOperation.Tan:
                Show(trig.Tan(x));
                Note = $"The angle was treated as {unitName}.";
                break;
            case ScientificOperation.Asin:
                Show(trig.Asin(x));
                Note = $"The result is an angle in {unitName}.";
                break;
            case ScientificOperation.Acos:
                Show(trig.Acos(x));
                Note = $"The result is an angle in {unitName}.";
                break;
            case ScientificOperation.Atan:
                Show(trig.Atan(x));
                Note = $"The result is an angle in {unitName}.";
                break;
            case ScientificOperation.Factorial:
                if (x % 1 != 0 || Math.Abs(x) > int.MaxValue)
                {
                    Fail("Factorial needs a whole number.");
                    break;
                }
                Show(calculator.Factorial((int)x));
                break;
        }
    }
}