using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Text.RegularExpressions;


namespace Prahelika.Calculators.Web.Tests;

public class WebPageTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    // https avoids the template's http-to-https redirect, and we do not follow redirects
    // silently, so a redirect shows up as a clear failure instead of an empty page.
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost")
    });

    // Razor Pages protects forms with a hidden token. Load the page, read the token,
    // and send it back with the form, like a browser does.
    private async Task<string> PostFormAsync(string url, Dictionary<string, string> fields)
    {
        var pageResponse = await _client.GetAsync(url);
        Assert.True(pageResponse.IsSuccessStatusCode,
            $"GET {url} returned {(int)pageResponse.StatusCode}, redirect to: {pageResponse.Headers.Location}");
        string page = await pageResponse.Content.ReadAsStringAsync();
        var match = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "The page has no anti-forgery token.");

        fields["__RequestVerificationToken"] = match.Groups[1].Value;
        var response = await _client.PostAsync(url, new FormUrlEncodedContent(fields));
        Assert.True(response.IsSuccessStatusCode,
    $"POST to {url} returned {(int)response.StatusCode}, redirect to: {response.Headers.Location}");
        return await response.Content.ReadAsStringAsync();
    }

    [Theory]
    [InlineData("/", "Prahelika Calculators")]
    [InlineData("/Basic", "Basic calculator")]
    [InlineData("/Scientific", "Scientific calculator")]
    [InlineData("/Finance", "Finance calculators")]
    public async Task Pages_Open_And_Show_Their_Title(string url, string expectedText)
    {
        var response = await _client.GetAsync(url);

        response.EnsureSuccessStatusCode();
        Assert.Contains(expectedText, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Basic_Divide_Shows_The_Result()
    {
        string html = await PostFormAsync("/Basic",
            new() { ["Operation"] = "Divide", ["A"] = "7", ["B"] = "2" });

        Assert.Contains("3.5", html);
        Assert.DoesNotContain("alert-danger", html);
    }

    [Fact]
    public async Task Basic_Divide_By_Zero_Shows_An_Error_Box()
    {
        string html = await PostFormAsync("/Basic",
            new() { ["Operation"] = "Divide", ["A"] = "10", ["B"] = "0" });

        Assert.Contains("alert-danger", html);
    }

    [Fact]
    public async Task Basic_Empty_Box_Asks_For_Both_Numbers()
    {
        string html = await PostFormAsync("/Basic",
            new() { ["Operation"] = "Add", ["A"] = "5", ["B"] = "" });

        Assert.Contains("Please enter both numbers.", html);
    }

    [Fact]
    public async Task Scientific_Sin_In_Degrees_Reports_The_Unit()
    {
        string html = await PostFormAsync("/Scientific",
            new() { ["Operation"] = "Sin", ["X"] = "90", ["Unit"] = "Degrees" });

        Assert.Contains("treated as degrees", html);
        Assert.DoesNotContain("alert-danger", html);
    }

    [Fact]
    public async Task Scientific_Tan_Of_90_Degrees_Is_An_Error()
    {
        string html = await PostFormAsync("/Scientific",
            new() { ["Operation"] = "Tan", ["X"] = "90", ["Unit"] = "Degrees" });

        Assert.Contains("alert-danger", html);
    }

    [Fact]
    public async Task Scientific_Nth_Root_Needs_A_Whole_Number()
    {
        string html = await PostFormAsync("/Scientific",
            new() { ["Operation"] = "NthRoot", ["X"] = "27", ["Y"] = "2.5" });

        Assert.Contains("must be a whole number", html);
    }

    [Fact]
    public async Task Finance_Emi_Shows_The_Monthly_Payment()
    {
        string html = await PostFormAsync("/Finance?calculation=Emi",
            new() { ["Calculation"] = "Emi", ["Amount"] = "100000", ["Rate"] = "12", ["Months"] = "12" });

        Assert.Contains("8,884.88", html);
    }

    [Fact]
    public async Task Finance_Missing_Fields_Are_Listed()
    {
        string html = await PostFormAsync("/Finance?calculation=Emi",
            new() { ["Calculation"] = "Emi", ["Amount"] = "100000" });

        Assert.Contains("Please fill in", html);
    }
}