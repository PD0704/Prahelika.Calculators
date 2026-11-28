using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Prahelika.Calculators.Core.Dates;

namespace Prahelika.Calculators.Api.Tests;

public class DateEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public DateEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static async Task<T> ReadResultAsync<T>(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CalcResponse<T>>(JsonOptions);
        Assert.NotNull(body);
        return body.Result;
    }

    private async Task<T> GetResultAsync<T>(string url) => await ReadResultAsync<T>(await _client.GetAsync(url));

    [Fact]
    public async Task Age_OnAGivenDate_ReturnsYearsMonthsAndDays()
    {
        var age = await GetResultAsync<DateDifference>("/api/dates/age?birthDate=2000-01-15&asOf=2026-11-27");

        Assert.Equal(new DateDifference(26, 10, 12, 9813), age);
    }

    [Fact]
    public async Task Age_WithoutAsOf_UsesTodayFromTheClock()
    {
        // Replace the real clock with one that always says 27 November 2026.
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(new DateTimeOffset(2026, 11, 27, 10, 0, 0, TimeSpan.Zero)))));

        var response = await factory.CreateClient().GetAsync("/api/dates/age?birthDate=2000-01-15");

        Assert.Equal(new DateDifference(26, 10, 12, 9813), await ReadResultAsync<DateDifference>(response));
    }

    [Fact]
    public async Task Age_WithoutAsOf_UsesTheUtcDate()
    {
        // 26 November 20:00 UTC is already 27 November in India, but the server counts the UTC date.
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(new DateTimeOffset(2026, 11, 26, 20, 0, 0, TimeSpan.Zero)))));

        var response = await factory.CreateClient().GetAsync("/api/dates/age?birthDate=2000-01-15");

        Assert.Equal(new DateDifference(26, 10, 11, 9812), await ReadResultAsync<DateDifference>(response));
    }

    [Fact]
    public async Task Between_ReturnsTheGap_InEitherOrder()
    {
        var forwards = await GetResultAsync<DateDifference>("/api/dates/between?start=2024-01-01&end=2025-01-01");
        var backwards = await GetResultAsync<DateDifference>("/api/dates/between?start=2025-01-01&end=2024-01-01");

        Assert.Equal(new DateDifference(1, 0, 0, 366), forwards);
        Assert.Equal(forwards, backwards);
    }

    [Theory]
    [InlineData("2024-01-31", 1, "2024-02-29")]
    [InlineData("2025-01-31", 1, "2025-02-28")]
    [InlineData("2024-03-31", -1, "2024-02-29")]
    [InlineData("2026-11-27", 2, "2027-01-27")]
    public async Task AddMonths_MovesToTheLastDayOfAShorterMonth(string date, int months, string expected)
    {
        var result = await GetResultAsync<DateOnly>($"/api/dates/add-months?date={date}&months={months}");

        Assert.Equal(DateOnly.Parse(expected), result);
    }

    [Theory]
    [InlineData("2024-02-28", 2, "2024-03-01")]
    [InlineData("2023-02-28", 1, "2023-03-01")]
    [InlineData("2024-03-01", -1, "2024-02-29")]
    public async Task AddDays_CountsRealCalendarDays(string date, int days, string expected)
    {
        var result = await GetResultAsync<DateOnly>($"/api/dates/add-days?date={date}&days={days}");

        Assert.Equal(DateOnly.Parse(expected), result);
    }

    [Fact]
    public async Task Dates_AreWrittenAsPlainText()
    {
        var response = await _client.GetAsync("/api/dates/add-days?date=2024-02-28&days=1");

        Assert.Contains("\"result\":\"2024-02-29\"", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/api/dates/age?birthDate=2026-11-28&asOf=2026-11-27", "InvalidInput")]     // born after the date
    [InlineData("/api/dates/add-months?date=9999-12-31&months=1", "Overflow")]
    [InlineData("/api/dates/add-days?date=0001-01-01&days=-1", "Overflow")]
    public async Task InvalidDates_ReturnCalcProblem(string url, string expectedCode)
    {
        var response = await _client.GetAsync(url);

        await ProblemAssert.AssertCalcProblemAsync(response, expectedCode);
    }

    [Theory]
    [InlineData("/api/dates/age")]                                  // birthDate is missing
    [InlineData("/api/dates/age?birthDate=notadate")]
    [InlineData("/api/dates/age?birthDate=2000-02-30")]             // there is no 30 February
    [InlineData("/api/dates/between?start=2024-01-01")]             // end is missing
    [InlineData("/api/dates/add-months?date=2024-01-31&months=1.5")]
    public async Task BadOrMissingInput_ReturnsBadRequestProblem(string url)
    {
        var response = await _client.GetAsync(url);

        await ProblemAssert.AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }
}