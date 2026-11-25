using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Prahelika.Calculators.Core.Finance;

namespace Prahelika.Calculators.Api.Tests;

public class LoanEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    // The API writes enums as names ("First"), so reading the response needs the same setting.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public LoanEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // Calls the API, checks for status 200, and returns the "result" part of the response.
    private async Task<T> GetResultAsync<T>(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CalcResponse<T>>(JsonOptions);
        Assert.NotNull(body);
        return body.Result;
    }

    // ---- Prepayment ----

    [Fact]
    public async Task Prepayment_ValidInput_ReturnsNewTenureAndInterestSaved()
    {
        var result = await GetResultAsync<PrepaymentResult>(
            "/api/loans/prepayment?loanAmount=100000&rate=12&months=12&prepaymentMonth=6&prepaymentAmount=20000");

        Assert.Equal(8884.88m, result.MonthlyPayment);
        Assert.Equal(12, result.OriginalMonths);
        Assert.Equal(10, result.NewMonths);
        Assert.Equal(2, result.MonthsSaved);
        Assert.Equal(20000m, result.PrepaymentApplied);
        Assert.Equal(6618.53m, result.OriginalTotalInterest);
        Assert.Equal(5543.41m, result.NewTotalInterest);
        Assert.Equal(1075.12m, result.InterestSaved);
    }

    [Fact]
    public async Task Prepayment_LongLoan_ReturnsMonthsAndInterestSaved()
    {
        var result = await GetResultAsync<PrepaymentResult>(
            "/api/loans/prepayment?loanAmount=1000000&rate=9&months=240&prepaymentMonth=60&prepaymentAmount=200000");

        Assert.Equal(8997.26m, result.MonthlyPayment);
        Assert.Equal(174, result.NewMonths);
        Assert.Equal(66, result.MonthsSaved);
        Assert.Equal(395575.67m, result.InterestSaved);
    }

    // ---- Compare ----

    [Fact]
    public async Task Compare_ValidInput_ReturnsBothLoansAndTheCheaperOne()
    {
        var result = await GetResultAsync<LoanComparison>(
            "/api/loans/compare?firstAmount=500000&firstRate=10&firstMonths=60&firstFee=5000"
            + "&secondAmount=500000&secondRate=10.5&secondMonths=60");

        Assert.Equal(10623.52m, result.First.MonthlyPayment);
        Assert.Equal(142411.20m, result.First.TotalCost);
        Assert.Equal(10746.95m, result.Second.MonthlyPayment);
        Assert.Equal(144817.00m, result.Second.TotalCost);
        Assert.Equal(LoanChoice.First, result.Cheaper);
        Assert.Equal(2405.80m, result.CostDifference);
    }

    [Fact]
    public async Task Compare_FeesAreOptional()
    {
        var result = await GetResultAsync<LoanComparison>(
            "/api/loans/compare?firstAmount=500000&firstRate=9&firstMonths=36"
            + "&secondAmount=500000&secondRate=9&secondMonths=60");

        Assert.Equal(0m, result.First.ProcessingFee);
        Assert.Equal(LoanChoice.First, result.Cheaper);
        Assert.Equal(50355.48m, result.CostDifference);
    }

    [Fact]
    public async Task Compare_IdenticalLoans_AreTheSame()
    {
        var result = await GetResultAsync<LoanComparison>(
            "/api/loans/compare?firstAmount=1000000&firstRate=8.5&firstMonths=240"
            + "&secondAmount=1000000&secondRate=8.5&secondMonths=240");

        Assert.Equal(LoanChoice.Same, result.Cheaper);
        Assert.Equal(0m, result.CostDifference);
    }

    [Fact]
    public async Task Compare_WritesTheCheaperLoanAsText()
    {
        var response = await _client.GetAsync(
            "/api/loans/compare?firstAmount=500000&firstRate=10&firstMonths=60"
            + "&secondAmount=500000&secondRate=10.5&secondMonths=60");

        string json = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"cheaper\":\"First\"", json);
    }

    // ---- How much can I borrow? ----

    [Theory]
    [InlineData("monthlyPayment=20000&rate=9&months=240", 2222899.08, 4800000, 2577100.92)]
    [InlineData("monthlyPayment=10000&rate=0&months=12", 120000, 120000, 0)]
    public async Task MaxAmount_ValidInput_ReturnsTheLargestLoan(
        string query, decimal expectedLoan, decimal expectedTotal, decimal expectedInterest)
    {
        var result = await GetResultAsync<BorrowingResult>($"/api/loans/max-amount?{query}");

        Assert.Equal(expectedLoan, result.MaxLoanAmount);
        Assert.Equal(expectedTotal, result.TotalPayment);
        Assert.Equal(expectedInterest, result.TotalInterest);
    }

    // ---- Calculation errors come back as problem details with a "code" ----

    [Theory]
    [InlineData("/api/loans/prepayment?loanAmount=0&rate=12&months=12&prepaymentMonth=6&prepaymentAmount=1000", "InvalidInput")]
    [InlineData("/api/loans/prepayment?loanAmount=100000&rate=12&months=12&prepaymentMonth=12&prepaymentAmount=1000", "InvalidInput")]
    [InlineData("/api/loans/prepayment?loanAmount=100000&rate=12&months=12&prepaymentMonth=0&prepaymentAmount=1000", "InvalidInput")]
    [InlineData("/api/loans/prepayment?loanAmount=100000&rate=12&months=12&prepaymentMonth=6&prepaymentAmount=0", "InvalidInput")]
    [InlineData("/api/loans/prepayment?loanAmount=79228162514264337593543950335&rate=1000000&months=12&prepaymentMonth=6&prepaymentAmount=1000", "Overflow")]
    [InlineData("/api/loans/compare?firstAmount=0&firstRate=10&firstMonths=60&secondAmount=500000&secondRate=10&secondMonths=60", "InvalidInput")]
    [InlineData("/api/loans/compare?firstAmount=500000&firstRate=10&firstMonths=60&secondAmount=500000&secondRate=10&secondMonths=0", "InvalidInput")]
    [InlineData("/api/loans/compare?firstAmount=500000&firstRate=10&firstMonths=60&firstFee=-1&secondAmount=500000&secondRate=10&secondMonths=60", "InvalidInput")]
    [InlineData("/api/loans/compare?firstAmount=500000&firstRate=10&firstMonths=60&firstFee=79228162514264337593543950335&secondAmount=500000&secondRate=10&secondMonths=60", "Overflow")]
    [InlineData("/api/loans/max-amount?monthlyPayment=0&rate=9&months=240", "InvalidInput")]
    [InlineData("/api/loans/max-amount?monthlyPayment=20000&rate=-1&months=240", "InvalidInput")]
    [InlineData("/api/loans/max-amount?monthlyPayment=20000&rate=9&months=1201", "InvalidInput")]
    [InlineData("/api/loans/max-amount?monthlyPayment=79228162514264337593543950335&rate=0&months=12", "Overflow")]
    public async Task InvalidInput_ReturnsCalcProblem(string url, string expectedCode)
    {
        var response = await _client.GetAsync(url);

        await ProblemAssert.AssertCalcProblemAsync(response, expectedCode);
    }

    // ---- Framework-level errors (missing or badly formatted values) ----

    [Theory]
    [InlineData("/api/loans/prepayment?loanAmount=abc&rate=12&months=12&prepaymentMonth=6&prepaymentAmount=1000")]  // not a number
    [InlineData("/api/loans/prepayment?loanAmount=100000&rate=12&months=12")]                                       // prepayment details missing
    [InlineData("/api/loans/compare")]                                                                              // everything is missing
    [InlineData("/api/loans/max-amount?monthlyPayment=1000&rate=9")]                                                // months is missing
    [InlineData("/api/loans/max-amount?monthlyPayment=1000&rate=9&months=12.5")]                                    // months must be whole
    public async Task BadOrMissingInput_ReturnsBadRequestProblem(string url)
    {
        var response = await _client.GetAsync(url);

        await ProblemAssert.AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    // ---- The API description lists the new routes ----

    [Fact]
    public async Task OpenApiDocument_ListsTheLoanEndpoints()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/loans/prepayment", json);
        Assert.Contains("/api/loans/compare", json);
        Assert.Contains("/api/loans/max-amount", json);
    }
}