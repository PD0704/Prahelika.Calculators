using Prahelika.Calculators.Core.Dates;

namespace Prahelika.Calculators.Api.Endpoints;

public static class DateEndpoints
{
    public static IEndpointRouteBuilder MapDateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dates").WithTags("Dates");

        // IDateCalculator and TimeProvider (the clock) are supplied by dependency injection.
        // Dates are written as year-month-day, for example 2026-11-27.

        group.MapGet("/age",
                (DateOnly birthDate, IDateCalculator calc, TimeProvider clock, DateOnly? asOf = null) =>
                    calc.Age(birthDate, asOf ?? Today(clock)).ToHttpResult())
            .WithSummary("Age in years, months and days. asOf is the date to measure to; without it, today's date in UTC is used (which can be a day behind India early in the morning).")
            .ProducesCalculation<DateDifference>();

        group.MapGet("/between",
                (DateOnly start, DateOnly end, IDateCalculator calc) =>
                    calc.Between(start, end).ToHttpResult())
            .WithSummary("The gap between two dates in years, months, days and total days. The order of the dates does not matter.")
            .ProducesCalculation<DateDifference>();

        group.MapGet("/add-months",
                (DateOnly date, int months, IDateCalculator calc) =>
                    calc.AddMonths(date, months).ToHttpResult())
            .WithSummary("Adds months to a date (a negative number goes back). A month that is too short moves to its last day: 31 January plus 1 month is 28 or 29 February.")
            .ProducesCalculation<DateOnly>();

        group.MapGet("/add-days",
                (DateOnly date, int days, IDateCalculator calc) =>
                    calc.AddDays(date, days).ToHttpResult())
            .WithSummary("Adds days to a date (a negative number goes back).")
            .ProducesCalculation<DateOnly>();

        return app;
    }

    private static DateOnly Today(TimeProvider clock) => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
}