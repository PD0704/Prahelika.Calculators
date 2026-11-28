using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Prahelika.Calculators.Api.Tests;

public class UnitEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public UnitEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<decimal> ConvertAsync(string query)
    {
        var response = await _client.GetAsync($"/api/units/convert?{query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CalcResponse<decimal>>(JsonOptions);
        Assert.NotNull(body);
        return body.Result;
    }

    [Theory]
    [InlineData("value=1&from=mile&to=kilometre", 1.609344)]
    [InlineData("value=100&from=celsius&to=fahrenheit", 212)]
    [InlineData("value=37&from=celsius&to=fahrenheit", 98.6)]
    [InlineData("value=1&from=acre&to=hectare", 0.404686)]
    [InlineData("value=1&from=usGallon&to=litre", 3.785412)]
    [InlineData("value=-40&from=fahrenheit&to=celsius", -40)]
    [InlineData("value=1&from=MILE&to=Kilometre", 1.609344)]       // letter case does not matter
    public async Task Convert_ReturnsTheConvertedValue(string query, decimal expected)
    {
        Assert.Equal(expected, await ConvertAsync(query));
    }

    [Fact]
    public async Task UnitList_GroupsTheUnitsByCategory()
    {
        var response = await _client.GetAsync("/api/units");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<List<UnitCategoryInfo>>(JsonOptions);
        Assert.NotNull(categories);

        Assert.Equal(new[] { "Length", "Weight", "Temperature", "Area", "Volume" }, categories.Select(c => c.Category));
        Assert.Contains("Mile", categories.Single(c => c.Category == "Length").Units);
        Assert.Equal(new[] { "Celsius", "Fahrenheit", "Kelvin" }, categories.Single(c => c.Category == "Temperature").Units);
    }

    [Theory]
    [InlineData("/api/units/convert?value=1&from=parsec&to=metre", "InvalidInput")]          // an unknown unit
    [InlineData("/api/units/convert?value=1&from=metre&to=5", "InvalidInput")]               // numbers are not unit names
    [InlineData("/api/units/convert?value=1&from=metre&to=kilogram", "InvalidInput")]        // different categories
    [InlineData("/api/units/convert?value=-300&from=celsius&to=fahrenheit", "InvalidInput")] // below absolute zero
    [InlineData("/api/units/convert?value=79228162514264337593543950335&from=kilometre&to=millimetre", "Overflow")]
    public async Task InvalidConversions_ReturnCalcProblem(string url, string expectedCode)
    {
        var response = await _client.GetAsync(url);

        await ProblemAssert.AssertCalcProblemAsync(response, expectedCode);
    }

    [Theory]
    [InlineData("/api/units/convert?value=1&from=metre")]              // to is missing
    [InlineData("/api/units/convert?value=abc&from=metre&to=foot")]    // not a number
    public async Task BadOrMissingInput_ReturnsBadRequestProblem(string url)
    {
        var response = await _client.GetAsync(url);

        await ProblemAssert.AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }
}