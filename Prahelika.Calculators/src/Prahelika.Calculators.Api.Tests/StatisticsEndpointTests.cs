using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Prahelika.Calculators.Core.Statistics;

namespace Prahelika.Calculators.Api.Tests;

public class StatisticsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public StatisticsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<T> GetResultAsync<T>(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CalcResponse<T>>(JsonOptions);
        Assert.NotNull(body);
        return body.Result;
    }

    [Fact]
    public async Task Summary_ReturnsEverythingAboutTheNumbers()
    {
        var summary = await GetResultAsync<StatisticsSummary>("/api/statistics/summary?values=2,4,4,4,5,5,7,9");

        Assert.Equal(8, summary.Count);
        Assert.Equal(40m, summary.Sum);
        Assert.Equal(2m, summary.Minimum);
        Assert.Equal(9m, summary.Maximum);
        Assert.Equal(5m, summary.Mean);
        Assert.Equal(4.5m, summary.Median);
        Assert.Equal(new[] { 4m }, summary.Modes.ToArray());
        Assert.Equal(2m, summary.PopulationStandardDeviation);
        Assert.Equal(2.14m, summary.SampleStandardDeviation);
    }

    [Fact]
    public async Task Summary_OfOneNumber_HasNoSampleStandardDeviation()
    {
        var summary = await GetResultAsync<StatisticsSummary>("/api/statistics/summary?values=5");

        Assert.Equal(1, summary.Count);
        Assert.Null(summary.SampleStandardDeviation);
    }

    [Theory]
    [InlineData("1,2,2", 1.67)]
    [InlineData("2,4,4,4,5,5,7,9", 5)]
    [InlineData("1,2", 1.5)]
    [InlineData("-5,5.5", 0.25)]          // negative numbers and decimal points
    public async Task Mean_ReturnsTheAverage(string values, decimal expected)
    {
        Assert.Equal(expected, await GetResultAsync<decimal>($"/api/statistics/mean?values={values}"));
    }

    [Theory]
    [InlineData("9,1,5", 5)]
    [InlineData("1,2,3,4", 2.5)]
    public async Task Median_ReturnsTheMiddleValue(string values, decimal expected)
    {
        Assert.Equal(expected, await GetResultAsync<decimal>($"/api/statistics/median?values={values}"));
    }

    [Fact]
    public async Task Mode_ReturnsEveryTiedValue()
    {
        var modes = await GetResultAsync<decimal[]>("/api/statistics/mode?values=1,2,2,3,3");

        Assert.Equal(new[] { 2m, 3m }, modes);
    }

    [Fact]
    public async Task Mode_WhenNothingRepeats_ReturnsAnEmptyList()
    {
        var modes = await GetResultAsync<decimal[]>("/api/statistics/mode?values=1,2,3");

        Assert.Empty(modes);
    }

    [Theory]
    [InlineData("values=2,4,4,4,5,5,7,9", 2)]                 // population is the default
    [InlineData("values=2,4,4,4,5,5,7,9&sample=false", 2)]
    [InlineData("values=2,4,4,4,5,5,7,9&sample=true", 2.14)]
    [InlineData("values=5", 0)]
    public async Task StandardDeviation_CanBePopulationOrSample(string query, decimal expected)
    {
        Assert.Equal(expected, await GetResultAsync<decimal>($"/api/statistics/standard-deviation?{query}"));
    }

    [Theory]
    [InlineData("/api/statistics/mean?values=", "InvalidInput")]                     // no numbers
    [InlineData("/api/statistics/mean?values=1,abc", "InvalidInput")]                // not a number
    [InlineData("/api/statistics/mean?values=1,,2", "InvalidInput")]                 // an empty entry
    [InlineData("/api/statistics/mean?values=1,2,", "InvalidInput")]                 // a trailing comma
    [InlineData("/api/statistics/median?values=1;2", "InvalidInput")]                // the wrong separator
    [InlineData("/api/statistics/summary?values=1e3", "InvalidInput")]               // scientific notation is not accepted
    [InlineData("/api/statistics/standard-deviation?values=5&sample=true", "InvalidInput")]   // a sample needs two numbers
    [InlineData("/api/statistics/mean?values=79228162514264337593543950335,79228162514264337593543950335", "Overflow")]
    public async Task InvalidNumbers_ReturnCalcProblem(string url, string expectedCode)
    {
        var response = await _client.GetAsync(url);

        await ProblemAssert.AssertCalcProblemAsync(response, expectedCode);
    }

    [Theory]
    [InlineData("/api/statistics/mean")]                                          // values is missing
    [InlineData("/api/statistics/standard-deviation?values=1,2&sample=maybe")]    // not true or false
    public async Task BadOrMissingInput_ReturnsBadRequestProblem(string url)
    {
        var response = await _client.GetAsync(url);

        await ProblemAssert.AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }
}