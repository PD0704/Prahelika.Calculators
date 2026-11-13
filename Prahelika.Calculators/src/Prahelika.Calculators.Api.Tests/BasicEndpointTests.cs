using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Json;

namespace Prahelika.Calculators.Api.Tests;

public class BasicEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    // xUnit creates one factory (one in-memory copy of the API) and shares it with every test in this class.
    public BasicEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("add", 2, 3, 5)]
    [InlineData("subtract", 10, 4, 6)]
    [InlineData("multiply", 3, 4, 12)]
    [InlineData("divide", 7, 2, 3.5)]
    [InlineData("modulo", 10, 3, 1)]
    [InlineData("add", 0.1, 0.2, 0.3)]
    public async Task Operation_ValidInput_ReturnsResult(string operation, decimal a, decimal b, decimal expected)
    {
        // Invariant culture: numbers in the URL must always use "." as the decimal point.
        var url = FormattableString.Invariant($"/api/basic/{operation}?a={a}&b={b}");

        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CalcResponse<decimal>>();
        Assert.NotNull(body);
        Assert.Equal(expected, body.Result);
    }

    [Fact]
    public async Task Percentage_ValidInput_ReturnsResult()
    {
        var response = await _client.GetAsync("/api/basic/percentage?value=150&percent=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CalcResponse<decimal>>();
        Assert.NotNull(body);
        Assert.Equal(30m, body.Result);
    }

    [Theory]
    [InlineData("divide")]
    [InlineData("modulo")]
    public async Task DivideOrModulo_ByZero_ReturnsDivisionByZeroProblem(string operation)
    {
        var response = await _client.GetAsync($"/api/basic/{operation}?a=10&b=0");

        await ProblemAssert.AssertCalcProblemAsync(response, "DivisionByZero");
    }

    [Fact]
    public async Task Add_ResultTooLarge_ReturnsOverflowProblem()
    {
        const string max = "79228162514264337593543950335";   // decimal.MaxValue

        var response = await _client.GetAsync($"/api/basic/add?a={max}&b={max}");

        await ProblemAssert.AssertCalcProblemAsync(response, "Overflow");
    }

    [Theory]
    [InlineData("/api/basic/add?a=abc&b=3")]   // not a number
    [InlineData("/api/basic/add?a=2")]         // b is missing
    [InlineData("/api/basic/add")]             // both are missing
    public async Task BadOrMissingInput_ReturnsBadRequestProblem(string url)
    {
        var response = await _client.GetAsync(url);

        await ProblemAssert.AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }
}