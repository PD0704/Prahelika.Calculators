using Prahelika.Calculators.Core;
using Prahelika.Calculators.Core.Scientific;

namespace Prahelika.Calculators.Api.Endpoints;

public static class ScientificEndpoints
{
    public static IEndpointRouteBuilder MapScientificEndpoints(this IEndpointRouteBuilder app)
    {
        // Used by the functions that involve no angles, so the angle unit does not matter here.
        var calculator = new ScientificCalculator();

        var group = app.MapGroup("/api/scientific").WithTags("Scientific");

        // ---- Functions that return a plain number ----
        group.MapGet("/power", (double value, double exponent) => calculator.Power(value, exponent).ToHttpResult())
            .WithSummary("Raises value to the given exponent.")
            .ProducesCalculation<double>();

        group.MapGet("/sqrt", (double value) => calculator.SquareRoot(value).ToHttpResult())
            .WithSummary("Square root. Fails for negative numbers.")
            .ProducesCalculation<double>();

        group.MapGet("/nthroot", (double value, int degree) => calculator.NthRoot(value, degree).ToHttpResult())
            .WithSummary("Root of the given degree, e.g. degree 3 is the cube root.")
            .ProducesCalculation<double>();

        group.MapGet("/ln", (double value) => calculator.NaturalLog(value).ToHttpResult())
            .WithSummary("Natural logarithm (base e).")
            .ProducesCalculation<double>();

        group.MapGet("/log10", (double value) => calculator.Log10(value).ToHttpResult())
            .WithSummary("Base-10 logarithm.")
            .ProducesCalculation<double>();

        group.MapGet("/log", (double value, double logBase) => calculator.Log(value, logBase).ToHttpResult())
            .WithSummary("Logarithm of value in the given base.")
            .ProducesCalculation<double>();

        group.MapGet("/factorial", (int n) => calculator.Factorial(n).ToHttpResult())
            .WithSummary("n factorial. Works for n from 0 to 170.")
            .ProducesCalculation<double>();

        // ---- Constants ----
        group.MapGet("/constants", () => Results.Ok(new ConstantsResponse(ScientificConstants.Pi, ScientificConstants.E)))
            .WithSummary("Returns the constants pi and e.")
            .Produces<ConstantsResponse>();

        // ---- Trigonometry: the response also tells the caller which unit was used ----
        group.MapGet("/sin", (double angle, string unit = "radians") => Trig(unit, c => c.Sin(angle)))
            .WithSummary("Sine of an angle. The unit is 'radians' (default) or 'degrees'.")
            .ProducesAngleCalculation();

        group.MapGet("/cos", (double angle, string unit = "radians") => Trig(unit, c => c.Cos(angle)))
            .WithSummary("Cosine of an angle. The unit is 'radians' (default) or 'degrees'.")
            .ProducesAngleCalculation();

        group.MapGet("/tan", (double angle, string unit = "radians") => Trig(unit, c => c.Tan(angle)))
            .WithSummary("Tangent of an angle. The unit is 'radians' (default) or 'degrees'.")
            .ProducesAngleCalculation();

        group.MapGet("/asin", (double value, string unit = "radians") => Trig(unit, c => c.Asin(value)))
            .WithSummary("Inverse sine. The result is returned in the given unit.")
            .ProducesAngleCalculation();

        group.MapGet("/acos", (double value, string unit = "radians") => Trig(unit, c => c.Acos(value)))
            .WithSummary("Inverse cosine. The result is returned in the given unit.")
            .ProducesAngleCalculation();

        group.MapGet("/atan", (double value, string unit = "radians") => Trig(unit, c => c.Atan(value)))
            .WithSummary("Inverse tangent. The result is returned in the given unit.")
            .ProducesAngleCalculation();

        return app;
    }

    // A calculator is cheap to create, so each request gets one that uses the unit it asked for.
    private static IResult Trig(string unit, Func<IScientificCalculator, CalcResult<double>> operation)
    {
        if (!TryParseUnit(unit, out var angleUnit))
        {
            return Results.BadRequest(new ErrorResponse(nameof(CalcError.InvalidInput), "The unit must be 'radians' or 'degrees'."));
        }

        var result = operation(new ScientificCalculator(angleUnit));

        return result.IsSuccess
            ? Results.Ok(new AngleCalcResponse(result.Value, angleUnit.ToString()))
            : result.ToErrorResult();
    }

    private static bool TryParseUnit(string unit, out AngleUnit angleUnit) =>
        Enum.TryParse(unit, ignoreCase: true, out angleUnit) && Enum.IsDefined(angleUnit);
}