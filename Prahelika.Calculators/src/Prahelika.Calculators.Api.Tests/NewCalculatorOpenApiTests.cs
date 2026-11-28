using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Prahelika.Calculators.Api.Tests;

public class NewCalculatorOpenApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public NewCalculatorOpenApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/statistics/summary")]
    [InlineData("/api/statistics/mean")]
    [InlineData("/api/statistics/median")]
    [InlineData("/api/statistics/mode")]
    [InlineData("/api/statistics/standard-deviation")]
    [InlineData("/api/units")]
    [InlineData("/api/units/convert")]
    [InlineData("/api/dates/age")]
    [InlineData("/api/dates/between")]
    [InlineData("/api/dates/add-months")]
    [InlineData("/api/dates/add-days")]
    [InlineData("/api/programmer/convert")]
    [InlineData("/api/programmer/bitwise")]
    public async Task OpenApiDocument_ListsTheRoute(string route)
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains($"\"{route}\"", await response.Content.ReadAsStringAsync());
    }
}