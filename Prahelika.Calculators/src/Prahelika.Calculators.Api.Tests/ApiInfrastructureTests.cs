using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;

namespace Prahelika.Calculators.Api.Tests;

public class ApiInfrastructureTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiInfrastructureTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OpenApiDocument_IsServed_AndListsEndpoints()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/basic/add", json);
        Assert.Contains("/api/scientific/sin", json);
    }

    [Fact]
    public async Task Root_RedirectsToSwagger()
    {
        // By default the test client follows redirects. Turn that off so we can see the redirect itself.
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/swagger", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task UnknownRoute_ReturnsNotFoundProblem()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/doesnotexist");

        await ProblemAssert.AssertProblemAsync(response, HttpStatusCode.NotFound);
    }
}