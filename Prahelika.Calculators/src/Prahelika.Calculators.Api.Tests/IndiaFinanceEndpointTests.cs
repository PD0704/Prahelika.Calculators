using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Prahelika.Calculators.Core.Finance;

namespace Prahelika.Calculators.Api.Tests;

public class IndiaFinanceEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IndiaFinanceEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // Calls the API, checks for status 200, and returns the "result" part of the response.
    private async Task<T> GetResultAsync<T>(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CalcResponse<T>>();
        Assert.NotNull(body);
        return body.Result;
    }

    // ---- Fixed deposit ----

    [Theory]
    [InlineData("principal=100000&rate=7&months=12", 100000, 7185.90, 107185.90)]                          // default: quarterly
    [InlineData("principal=100000&rate=8&months=24&frequency=annually", 100000, 16640.00, 116640.00)]
    [InlineData("principal=100000&rate=8&months=24&frequency=MONTHLY", 100000, 17288.79, 117288.79)]       // not case-sensitive
    public async Task FixedDeposit_ValidInput_ReturnsInvestedInterestAndMaturity(
        string query, decimal expectedInvested, decimal expectedInterest, decimal expectedMaturity)
    {
        var result = await GetResultAsync<MaturityResult>($"/api/finance/fixed-deposit?{query}");

        Assert.Equal(expectedInvested, result.InvestedAmount);
        Assert.Equal(expectedInterest, result.InterestEarned);
        Assert.Equal(expectedMaturity, result.MaturityAmount);
    }

    // ---- Recurring deposit ----

    [Fact]
    public async Task RecurringDeposit_ValidInput_ReturnsInvestedInterestAndMaturity()
    {
        var result = await GetResultAsync<MaturityResult>(
            "/api/finance/recurring-deposit?monthlyDeposit=5000&rate=7.5&months=12");

        Assert.Equal(60000m, result.InvestedAmount);
        Assert.Equal(2478.46m, result.InterestEarned);
        Assert.Equal(62478.46m, result.MaturityAmount);
    }

    // ---- PPF ----

    [Fact]
    public async Task Ppf_ValidInput_ReturnsInvestedInterestAndMaturity()
    {
        var result = await GetResultAsync<MaturityResult>("/api/finance/ppf?yearlyDeposit=150000&rate=7.1&years=15");

        Assert.Equal(2250000m, result.InvestedAmount);
        Assert.Equal(1818209.22m, result.InterestEarned);
        Assert.Equal(4068209.22m, result.MaturityAmount);
    }

    // ---- GST split ----

    [Theory]
    [InlineData("amount=1000&rate=18", 1000, 90, 90, 180, 1180)]                       // default: exclusive
    [InlineData("amount=1000&rate=18&mode=exclusive", 1000, 90, 90, 180, 1180)]
    [InlineData("amount=1180&rate=18&mode=INCLUSIVE", 1000, 90, 90, 180, 1180)]        // not case-sensitive
    [InlineData("amount=1&rate=1", 1, 0.01, 0, 0.01, 1.01)]                            // one paisa cannot split in two
    public async Task GstSplit_ValidInput_ReturnsCgstAndSgst(
        string query, decimal net, decimal cgst, decimal sgst, decimal totalGst, decimal total)
    {
        var result = await GetResultAsync<GstSplitResult>($"/api/finance/gst-split?{query}");

        Assert.Equal(net, result.NetAmount);
        Assert.Equal(cgst, result.Cgst);
        Assert.Equal(sgst, result.Sgst);
        Assert.Equal(totalGst, result.TotalGst);
        Assert.Equal(total, result.TotalAmount);
    }

    // ---- Income tax ----

    [Fact]
    public async Task IncomeTax_Salaried_IsTheDefault_AndReturnsTheBreakdown()
    {
        var result = await GetResultAsync<IncomeTaxResult>("/api/finance/income-tax?income=1500000");

        Assert.Equal("2026-27", result.FinancialYear);
        Assert.Equal(75000m, result.StandardDeduction);
        Assert.Equal(1425000m, result.TaxableIncome);
        Assert.Equal(93750m, result.TaxOnSlabs);
        Assert.Equal(3750m, result.Cess);
        Assert.Equal(97500m, result.TotalTax);
        Assert.Equal(4, result.Breakdown.Count);
        Assert.Equal(new SlabTax(1200000m, 1600000m, 15m, 225000m, 33750m), result.Breakdown[3]);
    }

    [Theory]
    [InlineData("income=1275000", 1200000, 0)]                       // 12.75 lakh salaried: nothing to pay
    [InlineData("income=1280000", 1205000, 5200)]                    // marginal relief
    [InlineData("income=1200000&salaried=false", 1200000, 0)]        // no standard deduction
    [InlineData("income=1300000&salaried=false", 1300000, 78000)]
    public async Task IncomeTax_ValidInput_ReturnsTaxableIncomeAndTotalTax(string query, decimal expectedTaxable, decimal expectedTotal)
    {
        var result = await GetResultAsync<IncomeTaxResult>($"/api/finance/income-tax?{query}");

        Assert.Equal(expectedTaxable, result.TaxableIncome);
        Assert.Equal(expectedTotal, result.TotalTax);
    }

    // ---- Calculation errors come back as problem details with a "code" ----

    [Theory]
    [InlineData("/api/finance/fixed-deposit?principal=0&rate=7&months=12", "InvalidInput")]
    [InlineData("/api/finance/fixed-deposit?principal=1000&rate=7&months=1201", "InvalidInput")]
    [InlineData("/api/finance/fixed-deposit?principal=1000&rate=7&months=12&frequency=yearly", "InvalidInput")]
    [InlineData("/api/finance/fixed-deposit?principal=1000&rate=7&months=12&frequency=4", "InvalidInput")]   // numbers are rejected
    [InlineData("/api/finance/fixed-deposit?principal=79228162514264337593543950335&rate=10&months=12", "Overflow")]
    [InlineData("/api/finance/recurring-deposit?monthlyDeposit=0&rate=7&months=12", "InvalidInput")]
    [InlineData("/api/finance/recurring-deposit?monthlyDeposit=1000&rate=-1&months=12", "InvalidInput")]
    [InlineData("/api/finance/recurring-deposit?monthlyDeposit=79228162514264337593543950335&rate=10&months=12", "Overflow")]
    [InlineData("/api/finance/ppf?yearlyDeposit=499&rate=7.1&years=15", "InvalidInput")]
    [InlineData("/api/finance/ppf?yearlyDeposit=1000&rate=7.1&years=16", "InvalidInput")]   // terms come in blocks of 5
    [InlineData("/api/finance/ppf?yearlyDeposit=150000&rate=1000&years=50", "Overflow")]
    [InlineData("/api/finance/gst-split?amount=-1&rate=18", "InvalidInput")]
    [InlineData("/api/finance/gst-split?amount=1000&rate=18&mode=sideways", "InvalidInput")]
    [InlineData("/api/finance/gst-split?amount=1000&rate=18&mode=1", "InvalidInput")]
    [InlineData("/api/finance/income-tax?income=-1", "InvalidInput")]
    [InlineData("/api/finance/income-tax?income=5075001", "InvalidInput")]                  // taxable income above the limit
    public async Task InvalidInput_ReturnsCalcProblem(string url, string expectedCode)
    {
        var response = await _client.GetAsync(url);

        await ProblemAssert.AssertCalcProblemAsync(response, expectedCode);
    }

    // ---- Framework-level errors (missing or badly formatted values) ----

    [Theory]
    [InlineData("/api/finance/fixed-deposit?principal=abc&rate=7&months=12")]   // not a number
    [InlineData("/api/finance/ppf?yearlyDeposit=1000&rate=7.1&years=15.5")]     // years must be a whole number
    [InlineData("/api/finance/recurring-deposit?monthlyDeposit=1000&rate=7")]   // months is missing
    [InlineData("/api/finance/income-tax")]                                     // income is missing
    [InlineData("/api/finance/income-tax?income=1000000&salaried=maybe")]       // not true or false
    public async Task BadOrMissingInput_ReturnsBadRequestProblem(string url)
    {
        var response = await _client.GetAsync(url);

        await ProblemAssert.AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    // ---- The API description lists the new routes ----

    [Fact]
    public async Task OpenApiDocument_ListsTheNewEndpoints()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/finance/fixed-deposit", json);
        Assert.Contains("/api/finance/recurring-deposit", json);
        Assert.Contains("/api/finance/ppf", json);
        Assert.Contains("/api/finance/gst-split", json);
        Assert.Contains("/api/finance/income-tax", json);
    }
}