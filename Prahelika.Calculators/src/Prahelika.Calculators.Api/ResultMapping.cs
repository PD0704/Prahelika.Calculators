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
        Results.BadRequest(new ErrorResponse(result.Error.ToString(), result.Message ?? "The calculation failed."));

    /// <summary>Documents a 200 response with a number and a 400 response with an error.</summary>
    public static RouteHandlerBuilder ProducesCalculation<T>(this RouteHandlerBuilder builder) =>
        builder.Produces<CalcResponse<T>>().Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

    /// <summary>Documents a 200 response with a number and its angle unit, and a 400 response with an error.</summary>
    public static RouteHandlerBuilder ProducesAngleCalculation(this RouteHandlerBuilder builder) =>
        builder.Produces<AngleCalcResponse>().Produces<ErrorResponse>(StatusCodes.Status400BadRequest);
}