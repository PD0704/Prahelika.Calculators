using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Prahelika.Calculators.Core.Programmer;

namespace Prahelika.Calculators.Api.Tests;

public class ProgrammerEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public ProgrammerEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<NumberConversion> GetResultAsync(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CalcResponse<NumberConversion>>(JsonOptions);
        Assert.NotNull(body);
        return body.Result;
    }

    [Theory]
    [InlineData("value=255", "255", "11111111", "377", "FF")]                          // decimal is the default base
    [InlineData("value=255&base=decimal", "255", "11111111", "377", "FF")]
    [InlineData("value=ff&base=hexadecimal", "255", "11111111", "377", "FF")]
    [InlineData("value=0xFF&base=hexadecimal", "255", "11111111", "377", "FF")]
    [InlineData("value=0b1100&base=binary", "12", "1100", "14", "C")]
    [InlineData("value=17&base=OCTAL", "15", "1111", "17", "F")]                       // letter case does not matter
    public async Task Convert_WritesTheNumberInEveryBase(
        string query, string decimalText, string binary, string octal, string hex)
    {
        var result = await GetResultAsync($"/api/programmer/convert?{query}");

        Assert.Equal(decimalText, result.Decimal);
        Assert.Equal(binary, result.Binary);
        Assert.Equal(octal, result.Octal);
        Assert.Equal(hex, result.Hexadecimal);
    }

    [Fact]
    public async Task Convert_ShowsNegativeNumbersAs64Bits()
    {
        var result = await GetResultAsync("/api/programmer/convert?value=-1");

        Assert.Equal("-1", result.Decimal);
        Assert.Equal(new string('1', 64), result.Binary);
        Assert.Equal("FFFFFFFFFFFFFFFF", result.Hexadecimal);
    }

    [Theory]
    [InlineData("left=12&operation=and&right=10", 8)]
    [InlineData("left=12&operation=or&right=10", 14)]
    [InlineData("left=12&operation=xor&right=10", 6)]
    [InlineData("left=5&operation=not", -6)]                      // not needs only one number
    [InlineData("left=1&operation=shiftLeft&right=4", 16)]
    [InlineData("left=256&operation=shiftRight&right=4", 16)]
    [InlineData("left=-16&operation=shiftRight&right=2", -4)]
    public async Task Bitwise_AppliesTheOperation(string query, long expected)
    {
        var result = await GetResultAsync($"/api/programmer/bitwise?{query}");

        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public async Task Bitwise_ReadsTheNumbersInTheChosenBase_AndShowsEveryBase()
    {
        var result = await GetResultAsync(
            "/api/programmer/bitwise?left=F0F0&operation=and&right=FF00&base=hexadecimal");

        Assert.Equal("F000", result.Hexadecimal);
        Assert.Equal("61440", result.Decimal);
        Assert.Equal("1111000000000000", result.Binary);
    }

    [Theory]
    [InlineData("/api/programmer/convert?value=12&base=ternary", "InvalidInput")]                  // an unknown base
    [InlineData("/api/programmer/convert?value=102&base=binary", "InvalidInput")]                  // 2 is not a binary digit
    [InlineData("/api/programmer/convert?value=-5&base=binary", "InvalidInput")]                   // negative numbers are 64 bits
    [InlineData("/api/programmer/convert?value=", "InvalidInput")]
    [InlineData("/api/programmer/convert?value=9223372036854775808", "Overflow")]
    [InlineData("/api/programmer/convert?value=10000000000000000&base=hexadecimal", "Overflow")]
    [InlineData("/api/programmer/bitwise?left=1&operation=nand&right=1", "InvalidInput")]          // an unknown operation
    [InlineData("/api/programmer/bitwise?left=abc&operation=and&right=1", "InvalidInput")]
    [InlineData("/api/programmer/bitwise?left=1&operation=and&right=xyz", "InvalidInput")]
    [InlineData("/api/programmer/bitwise?left=1&operation=shiftLeft&right=64", "InvalidInput")]    // shift by 0 to 63 only
    [InlineData("/api/programmer/bitwise?left=1&operation=shiftRight&right=-1", "InvalidInput")]
    [InlineData("/api/programmer/bitwise?left=9223372036854775808&operation=not", "Overflow")]
    public async Task InvalidInput_ReturnsCalcProblem(string url, string expectedCode)
    {
        var response = await _client.GetAsync(url);

        await ProblemAssert.AssertCalcProblemAsync(response, expectedCode);
    }

    [Theory]
    [InlineData("/api/programmer/convert")]                       // value is missing
    [InlineData("/api/programmer/bitwise?operation=and")]         // left is missing
    [InlineData("/api/programmer/bitwise?left=1")]                // operation is missing
    public async Task BadOrMissingInput_ReturnsBadRequestProblem(string url)
    {
        var response = await _client.GetAsync(url);

        await ProblemAssert.AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }
}