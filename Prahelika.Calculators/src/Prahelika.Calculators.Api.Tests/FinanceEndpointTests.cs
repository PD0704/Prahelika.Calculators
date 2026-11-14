using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Prahelika.Calculators.Core.Finance;

namespace Prahelika.Calculators.Api.Tests;

public class FinanceEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FinanceEndpointTests(WebApplicationFactory<Program> factory)
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

    // ---- Interest ----

    [Fact]
    public async Task SimpleInterest_ValidInput_ReturnsInterestAndTotal()
    {
        var result = await GetResultAsync<InterestResult>("/api/finance/simple-interest?principal=1000&rate=5&years=3");

        Assert.Equal(1000m, result.Principal);
        Assert.Equal(150m, result.Interest);
        Assert.Equal(1150m, result.TotalAmount);
    }

    [Theory]
    [InlineData("principal=1000&rate=5&years=10", 628.89, 1628.89)]                              // default: annually
    [InlineData("principal=1000&rate=12&years=1&frequency=monthly", 126.83, 1126.83)]
    [InlineData("principal=1000&rate=12&years=1&frequency=MONTHLY", 126.83, 1126.83)]            // not case-sensitive
    [InlineData("principal=1000&rate=5&years=1&frequency=semiAnnually", 50.63, 1050.63)]         // exact midpoint rounds up
    [InlineData("principal=5000&rate=8&years=2&frequency=quarterly", 858.30, 5858.30)]
    public async Task CompoundInterest_ValidInput_ReturnsInterestAndTotal(string query, decimal expectedInterest, decimal expectedTotal)
    {
        var result = await GetResultAsync<InterestResult>($"/api/finance/compound-interest?{query}");

        Assert.Equal(expectedInterest, result.Interest);
        Assert.Equal(expectedTotal, result.TotalAmount);
    }

    // ---- Loans ----

    [Fact]
    public async Task Emi_ValidLoan_ReturnsPaymentTotalAndInterest()
    {
        var result = await GetResultAsync<LoanSummary>("/api/finance/emi?loanAmount=100000&rate=12&months=12");

        Assert.Equal(8884.88m, result.MonthlyPayment);
        Assert.Equal(106618.56m, result.TotalPayment);
        Assert.Equal(6618.56m, result.TotalInterest);
    }

    [Fact]
    public async Task Amortization_ValidLoan_ReturnsOneRowPerMonthEndingAtZero()
    {
        var rows = await GetResultAsync<IReadOnlyList<AmortizationRow>>(
            "/api/finance/amortization?loanAmount=100000&rate=12&months=12");

        Assert.Equal(12, rows.Count);

        Assert.Equal(1, rows[0].Month);
        Assert.Equal(8884.88m, rows[0].Payment);
        Assert.Equal(7884.88m, rows[0].PrincipalPaid);
        Assert.Equal(1000.00m, rows[0].InterestPaid);
        Assert.Equal(92115.12m, rows[0].RemainingBalance);

        Assert.Equal(0m, rows[^1].RemainingBalance);
    }

    // ---- Investments ----

    [Fact]
    public async Task Sip_ValidInput_ReturnsInvestedReturnsAndFutureValue()
    {
        var result = await GetResultAsync<SipResult>("/api/finance/sip?monthlyInvestment=5000&rate=12&months=120");

        Assert.Equal(600000m, result.InvestedAmount);
        Assert.Equal(561695.38m, result.EstimatedReturns);
        Assert.Equal(1161695.38m, result.FutureValue);
    }

    [Fact]
    public async Task Roi_ValidInput_ReturnsGainAndPercent()
    {
        var result = await GetResultAsync<RoiResult>("/api/finance/roi?amountInvested=1000&finalValue=1250");

        Assert.Equal(250m, result.Gain);
        Assert.Equal(25m, result.RoiPercent);
    }

    // ---- GST ----

    [Theory]
    [InlineData("amount=1000&rate=18", 1000, 180, 1180)]                       // default: exclusive
    [InlineData("amount=1000&rate=18&mode=exclusive", 1000, 180, 1180)]
    [InlineData("amount=1180&rate=18&mode=inclusive", 1000, 180, 1180)]
    [InlineData("amount=1000&rate=18&mode=INCLUSIVE", 847.46, 152.54, 1000)]   // not case-sensitive
    public async Task Gst_ValidInput_ReturnsNetGstAndTotal(string query, decimal expectedNet, decimal expectedGst, decimal expectedTotal)
    {
        var result = await GetResultAsync<GstResult>($"/api/finance/gst?{query}");

        Assert.Equal(expectedNet, result.NetAmount);
        Assert.Equal(expectedGst, result.GstAmount);
        Assert.Equal(expectedTotal, result.TotalAmount);
    }

    // ---- Calculation errors come back as problem details with a "code" ----

    [Theory]
    [InlineData("/api/finance/simple-interest?principal=-1&rate=5&years=1", "InvalidInput")]
    [InlineData("/api/finance/compound-interest?principal=1000&rate=5&years=1&frequency=yearly", "InvalidInput")]
    [InlineData("/api/finance/compound-interest?principal=1000&rate=5&years=1&frequency=12", "InvalidInput")]   // numbers are rejected
    [InlineData("/api/finance/emi?loanAmount=0&rate=12&months=12", "InvalidInput")]
    [InlineData("/api/finance/emi?loanAmount=1000&rate=12&months=1201", "InvalidInput")]
    [InlineData("/api/finance/amortization?loanAmount=1000&rate=-1&months=12", "InvalidInput")]
    [InlineData("/api/finance/sip?monthlyInvestment=500&rate=12&months=0", "InvalidInput")]
    [InlineData("/api/finance/sip?monthlyInvestment=1000&rate=100&months=1200", "Overflow")]
    [InlineData("/api/finance/roi?amountInvested=0&finalValue=100", "InvalidInput")]
    [InlineData("/api/finance/gst?amount=1000&rate=18&mode=sideways", "InvalidInput")]
    [InlineData("/api/finance/gst?amount=1000&rate=18&mode=1", "InvalidInput")]
    [InlineData("/api/finance/emi?loanAmount=79228162514264337593543950335&rate=1000000&months=12", "Overflow")]
    public async Task InvalidInput_ReturnsCalcProblem(string url, string expectedCode)
    {
        var response = await _client.GetAsync(url);

        await ProblemAssert.AssertCalcProblemAsync(response, expectedCode);
    }

    // ---- Framework-level errors (missing or badly formatted values) ----

    [Theory]
    [InlineData("/api/finance/emi?loanAmount=abc&rate=12&months=12")]    // not a number
    [InlineData("/api/finance/emi?loanAmount=1000&rate=12&months=12.5")] // months must be a whole number
    [InlineData("/api/finance/emi?loanAmount=1000&rate=12")]             // months is missing
    [InlineData("/api/finance/gst")]                                     // everything is missing
    public async Task BadOrMissingInput_ReturnsBadRequestProblem(string url)
    {
        var response = await _client.GetAsync(url);

        await ProblemAssert.AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }
}