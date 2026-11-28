using Microsoft.AspNetCore.Mvc;
using Prahelika.Calculators.Core;
using Prahelika.Calculators.Core.Programmer;

namespace Prahelika.Calculators.Api.Endpoints;

public static class ProgrammerEndpoints
{
    private const string BaseMessage = "The base must be one of: binary, octal, decimal, hexadecimal.";

    public static IEndpointRouteBuilder MapProgrammerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/programmer").WithTags("Programmer");

        // IProgrammerCalculator is supplied by dependency injection (registered in Program.cs).
        // Numbers are 64-bit. In binary, octal and hexadecimal a negative number is written as its 64 bits (two's complement).
        // The "value" in the answer can be larger than a JavaScript number holds exactly,
        // so read the text fields (decimal, binary, octal, hexadecimal) for very large numbers.

        group.MapGet("/convert",
                (string value, IProgrammerCalculator calc, [FromQuery(Name = "base")] string numberBase = "decimal") =>
                {
                    if (!EnumParser.TryParseName(numberBase, out NumberBase from))
                    {
                        return ResultMapping.ToProblem(CalcError.InvalidInput, BaseMessage);
                    }

                    return calc.ConvertBase(value, from).ToHttpResult();
                })
            .WithSummary("Writes a number in binary, octal, decimal and hexadecimal. 'base' is the base of the number you send (default: decimal). The prefixes 0b, 0o and 0x are optional.")
            .ProducesCalculation<NumberConversion>();

        group.MapGet("/bitwise",
                (string left, string operation, IProgrammerCalculator calc,
                 string? right = null, [FromQuery(Name = "base")] string numberBase = "decimal") =>
                {
                    if (!EnumParser.TryParseName(numberBase, out NumberBase from))
                    {
                        return ResultMapping.ToProblem(CalcError.InvalidInput, BaseMessage);
                    }

                    if (!EnumParser.TryParseName(operation, out BitwiseOperation parsedOperation))
                    {
                        return ResultMapping.ToProblem(
                            CalcError.InvalidInput,
                            "The operation must be one of: and, or, xor, not, shiftLeft, shiftRight.");
                    }

                    var leftNumber = calc.Parse(left, from);
                    if (!leftNumber.IsSuccess)
                    {
                        return leftNumber.ToErrorResult();
                    }

                    var rightNumber = calc.Parse(right ?? "0", from);
                    if (!rightNumber.IsSuccess)
                    {
                        return rightNumber.ToErrorResult();
                    }

                    return calc.Bitwise(leftNumber.Value, parsedOperation, rightNumber.Value).ToHttpResult();
                })
            .WithSummary("Bitwise and, or, xor, not, shiftLeft or shiftRight on 64-bit numbers. 'base' is the base of left and right (default: decimal). 'right' is not needed for 'not'. A shift amount must be 0 to 63.")
            .ProducesCalculation<NumberConversion>();

        return app;
    }
}