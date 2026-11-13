using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Json;

namespace Prahelika.Calculators.Api.Tests;

public class ScientificEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const int Precision = 10;   // compare doubles to 10 decimal places

    private readonly HttpClient _client;

    public ScientificEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("power?value=2&exponent=10", 1024)]
    [InlineData("sqrt?value=144", 12)]
    [InlineData("nthroot?value=27&degree=3", 3)]
    [InlineData("ln?value=1", 0)]
    [InlineData("log10?value=1000", 3)]
    [InlineData("log?value=8&logBase=2", 3)]
    [InlineData("factorial?n=5", 120)]
    public async Task Function_ValidInput_ReturnsResult(string pathAndQuery, double expected)
    {
        var response = await _client.GetAsync($"/api/scientific/{pathAndQuery}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CalcResponse<double>>();
        Assert.NotNull(body);
        Assert.Equal(expected, body.Result, Precision);
    }

    [Fact]
    public async Task Constants_ReturnsPiAndE()
    {
        var response = await _client.GetAsync("/api/scientific/constants");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ConstantsResponse>();
        Assert.NotNull(body);
        Assert.Equal(Math.PI, body.Pi, Precision);
        Assert.Equal(Math.E, body.E, Precision);
    }

    [Theory]
    [InlineData("sin?angle=90&unit=degrees", 1, "Degrees")]
    [InlineData("sin?angle=90&unit=DEGREES", 1, "Degrees")]    // unit is not case-sensitive
    [InlineData("sin?angle=0", 0, "Radians")]                  // radians is the default
    [InlineData("cos?angle=0", 1, "Radians")]
    [InlineData("tan?angle=45&unit=degrees", 1, "Degrees")]
    [InlineData("asin?value=1&unit=degrees", 90, "Degrees")]
    [InlineData("acos?value=1&unit=degrees", 0, "Degrees")]
    [InlineData("atan?value=1&unit=degrees", 45, "Degrees")]
    public async Task Trig_ValidInput_ReturnsResultAndUnit(string pathAndQuery, double expected, string expectedUnit)
    {
        var response = await _client.GetAsync($"/api/scientific/{pathAndQuery}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AngleCalcResponse>();
        Assert.NotNull(body);
        Assert.Equal(expected, body.Result, Precision);
        Assert.Equal(expectedUnit, body.AngleUnit);
    }

    [Theory]
    [InlineData("sqrt?value=-4", "InvalidInput")]
    [InlineData("log10?value=0", "InvalidInput")]
    [InlineData("power?value=0&exponent=-1", "DivisionByZero")]
    [InlineData("factorial?n=171", "Overflow")]
    [InlineData("factorial?n=-1", "InvalidInput")]
    [InlineData("tan?angle=90&unit=degrees", "InvalidInput")]
    [InlineData("asin?value=2", "InvalidInput")]
    [InlineData("sin?angle=90&unit=gradians", "InvalidInput")]    // unknown unit name
    [InlineData("sin?angle=90&unit=5", "InvalidInput")]           // a number is not a unit name
    public async Task InvalidInput_ReturnsCalcProblem(string pathAndQuery, string expectedCode)
    {
        var response = await _client.GetAsync($"/api/scientific/{pathAndQuery}");

        await ProblemAssert.AssertCalcProblemAsync(response, expectedCode);
    }
}