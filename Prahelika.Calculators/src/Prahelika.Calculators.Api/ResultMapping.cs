using Prahelika.Calculators.Core;

namespace Prahelika.Calculators.Api;

/// <summary>Turns a <see cref="CalcResult{T}"/> into an HTTP response, and documents the responses for Swagger.</summary>
public static class ResultMapping
{
    public static IResult ToHttpResult<T>(this CalcResult<T> result) =>
        result.IsSuccess
            ? Results.Ok(new CalcResponse<T>(result.Value!))
            : result.ToErrorResult();

    public static IResult ToErrorResult<T>(this CalcResult<T> result) =>
        ToProblem(result.Error, result.Message ?? "The calculation failed.");

    /// <summary>Builds a standard problem-details response (status 400) for a calculation error.</summary>
    public static IResult ToProblem(CalcError error, string message)
    {
        var title = error switch
        {
            CalcError.DivisionByZero => "Division by zero",
            CalcError.InvalidInput => "Invalid input",
            CalcError.Overflow => "Result out of range",
            _ => "Calculation failed"
        };

        return Results.Problem(
            title: title,
            detail: message,
            statusCode: StatusCodes.Status400BadRequest,
            extensions: new Dictionary<string, object?> { ["code"] = error.ToString() });
    }

    /// <summary>Documents a 200 response with a number and a 400 problem response.</summary>
    public static RouteHandlerBuilder ProducesCalculation<T>(this RouteHandlerBuilder builder) =>
        builder.Produces<CalcResponse<T>>().ProducesProblem(StatusCodes.Status400BadRequest);

    /// <summary>Documents a 200 response with a number and its angle unit, and a 400 problem response.</summary>
    public static RouteHandlerBuilder ProducesAngleCalculation(this RouteHandlerBuilder builder) =>
        builder.Produces<AngleCalcResponse>().ProducesProblem(StatusCodes.Status400BadRequest);
}