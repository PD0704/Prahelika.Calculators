using System.Net;
using System.Text.Json;

namespace Prahelika.Calculators.Api.Tests;

internal static class ProblemAssert
{
    /// <summary>Checks the response is a standard problem-details response with the given status.</summary>
    public static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.Equal((int)expectedStatus, document.RootElement.GetProperty("status").GetInt32());
        Assert.True(document.RootElement.TryGetProperty("title", out _));
    }

    /// <summary>Checks a 400 problem response caused by a calculation error, including its "code" field.</summary>
    public static async Task AssertCalcProblemAsync(HttpResponseMessage response, string expectedCode)
    {
        await AssertProblemAsync(response, HttpStatusCode.BadRequest);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.Equal(expectedCode, document.RootElement.GetProperty("code").GetString());
    }
}