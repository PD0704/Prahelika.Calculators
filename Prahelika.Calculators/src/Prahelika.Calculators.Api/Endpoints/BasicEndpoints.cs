using Prahelika.Calculators.Core.Basic;

namespace Prahelika.Calculators.Api.Endpoints;

public static class BasicEndpoints
{
    public static IEndpointRouteBuilder MapBasicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/basic").WithTags("Basic");

        // IBasicCalculator is supplied by dependency injection (registered in Program.cs).
        group.MapGet("/add", (decimal a, decimal b, IBasicCalculator calc) => calc.Add(a, b).ToHttpResult())
            .WithSummary("Adds two numbers (a + b).")
            .ProducesCalculation<decimal>();

        group.MapGet("/subtract", (decimal a, decimal b, IBasicCalculator calc) => calc.Subtract(a, b).ToHttpResult())
            .WithSummary("Subtracts b from a (a - b).")
            .ProducesCalculation<decimal>();

        group.MapGet("/multiply", (decimal a, decimal b, IBasicCalculator calc) => calc.Multiply(a, b).ToHttpResult())
            .WithSummary("Multiplies two numbers (a * b).")
            .ProducesCalculation<decimal>();

        group.MapGet("/divide", (decimal a, decimal b, IBasicCalculator calc) => calc.Divide(a, b).ToHttpResult())
            .WithSummary("Divides a by b. Fails when b is 0.")
            .ProducesCalculation<decimal>();

        group.MapGet("/modulo", (decimal a, decimal b, IBasicCalculator calc) => calc.Modulo(a, b).ToHttpResult())
            .WithSummary("Remainder of a divided by b. Fails when b is 0.")
            .ProducesCalculation<decimal>();

        group.MapGet("/percentage", (decimal value, decimal percent, IBasicCalculator calc) => calc.Percentage(value, percent).ToHttpResult())
            .WithSummary("The given percentage of a value, e.g. 20% of 150 is 30.")
            .ProducesCalculation<decimal>();

        return app;
    }
}