using Microsoft.AspNetCore.Mvc;
using Prahelika.Calculators.Core;
using Prahelika.Calculators.Core.Units;

namespace Prahelika.Calculators.Api.Endpoints;

public static class UnitEndpoints
{
    public static IEndpointRouteBuilder MapUnitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/units").WithTags("Units");

        // IUnitConverter is supplied by dependency injection (registered in Program.cs).

        group.MapGet("/",
                (IUnitConverter converter) => Results.Ok(
                    Enum.GetValues<UnitCategory>()
                        .Select(category => new UnitCategoryInfo(
                            category.ToString(),
                            converter.UnitsIn(category).Select(unit => unit.ToString()).ToArray()))
                        .ToList()))
            .WithSummary("All the units you can convert between, grouped by category (Length, Weight, Temperature, Area, Volume).")
            .Produces<List<UnitCategoryInfo>>();

        group.MapGet("/convert",
                (decimal value, [FromQuery(Name = "from")] string fromUnit, [FromQuery(Name = "to")] string toUnit, IUnitConverter converter) =>
                {
                    if (!EnumParser.TryParseName(fromUnit, out MeasurementUnit from)
                        || !EnumParser.TryParseName(toUnit, out MeasurementUnit to))
                    {
                        return ResultMapping.ToProblem(
                            CalcError.InvalidInput,
                            "Unknown unit. Call GET /api/units to see the unit names, for example mile, kilometre, celsius, usGallon.");
                    }

                    return converter.Convert(value, from, to).ToHttpResult();
                })
            .WithSummary("Converts a value from one unit to another, for example value=1&from=mile&to=kilometre. Both units must be in the same category. Results have up to 6 decimal places.")
            .ProducesCalculation<decimal>();

        return app;
    }
}