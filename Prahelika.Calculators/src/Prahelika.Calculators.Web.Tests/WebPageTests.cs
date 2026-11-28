using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;


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
    // and send it back with the form, like a browser does. The form can be sent to a
    // different address than the one the token was read from (for example, a named handler).
    private async Task<HttpResponseMessage> PostFormResponseAsync(
        string url, Dictionary<string, string> fields, string? postUrl = null, HttpClient? client = null)
    {
        client ??= _client;

        var pageResponse = await client.GetAsync(url);
        Assert.True(pageResponse.IsSuccessStatusCode,
            $"GET {url} returned {(int)pageResponse.StatusCode}, redirect to: {pageResponse.Headers.Location}");
        string page = await pageResponse.Content.ReadAsStringAsync();
        var match = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "The page has no anti-forgery token.");

        fields["__RequestVerificationToken"] = match.Groups[1].Value;
        var response = await client.PostAsync(postUrl ?? url, new FormUrlEncodedContent(fields));
        Assert.True(response.IsSuccessStatusCode,
            $"POST to {postUrl ?? url} returned {(int)response.StatusCode}, redirect to: {response.Headers.Location}");
        return response;
    }

    private async Task<string> PostFormAsync(string url, Dictionary<string, string> fields, HttpClient? client = null)
    {
        var response = await PostFormResponseAsync(url, fields, client: client);
        return await response.Content.ReadAsStringAsync();
    }

    // A client for a copy of the website whose clock always says the given time.
    private HttpClient ClientWithClock(DateTimeOffset now) =>
        factory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => services.AddSingleton<TimeProvider>(new FixedTimeProvider(now))))
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });

    // Finds the value shown next to a label in the result box, for example the "Median" row.
    private static string DetailValue(string html, string label)
    {
        var match = Regex.Match(
            html,
            $"<dt class=\"col-sm-6\">{Regex.Escape(label)}</dt>\\s*<dd class=\"col-sm-6 mb-1\">(.*?)</dd>",
            RegexOptions.Singleline);
        Assert.True(match.Success, $"The result has no row called '{label}'.");
        return match.Groups[1].Value.Trim();
    }

    [Theory]
    [InlineData("/", "Prahelika Calculators")]
    [InlineData("/Basic", "Basic calculator")]
    [InlineData("/Scientific", "Scientific calculator")]
    [InlineData("/Finance", "Finance calculators")]
    [InlineData("/India", "India finance calculators")]
    [InlineData("/Loans", "Loan tools")]
    [InlineData("/Statistics", "Statistics calculator")]
    [InlineData("/Units", "Unit converter")]
    [InlineData("/Dates", "Date calculator")]
    [InlineData("/Programmer", "Programmer calculator")]

    public async Task Pages_Open_And_Show_Their_Title(string url, string expectedText)
    {
        var response = await _client.GetAsync(url);

        response.EnsureSuccessStatusCode();
        Assert.Contains(expectedText, await response.Content.ReadAsStringAsync());
    }

    #region Basic
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
    public async Task Basic_Percentage_Is_B_Percent_Of_A()
    {
        string html = await PostFormAsync("/Basic",
            new() { ["Operation"] = "Percentage", ["A"] = "150", ["B"] = "20" });

        Assert.Contains(">30</p>", html);
    }
    #endregion

    #region Scientific
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
    #endregion

    #region Finance
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

    [Fact]
    public async Task Finance_Loan_Schedule_Includes_The_Chart_Data()
    {
        string html = await PostFormAsync("/Finance?calculation=Amortization",
            new() { ["Calculation"] = "Amortization", ["Amount"] = "100000", ["Rate"] = "12", ["Months"] = "12" });

        // The two charts, and the scripts that draw them.
        Assert.Contains("id=\"balance-chart\"", html);
        Assert.Contains("id=\"split-chart\"", html);
        Assert.Matches("<script src=\"[^\"]*chart\\.umd[^\"]*\"", html);   // the Chart.js library
        Assert.Matches("<script src=\"[^\"]*loan-chart[^\"]*\"", html);    // the script that draws the charts

        // The numbers behind them are in the page as JSON.
        var match = Regex.Match(
            html, "<script id=\"loan-chart-data\" type=\"application/json\">(.*?)</script>", RegexOptions.Singleline);
        Assert.True(match.Success, "The chart data was not found in the page.");

        using var document = JsonDocument.Parse(match.Groups[1].Value);
        var data = document.RootElement;

        Assert.Equal(12, data.GetProperty("months").GetArrayLength());
        Assert.Equal(1, data.GetProperty("months")[0].GetInt32());
        Assert.Equal(12, data.GetProperty("months")[11].GetInt32());
        Assert.Equal(12, data.GetProperty("balance").GetArrayLength());
        Assert.Equal(12, data.GetProperty("principal").GetArrayLength());
        Assert.Equal(12, data.GetProperty("interest").GetArrayLength());

        Assert.Equal(1000m, data.GetProperty("interest")[0].GetDecimal());       // 1% of 100,000
        Assert.Equal(92115.12m, data.GetProperty("balance")[0].GetDecimal());    // 100,000 - (8,884.88 - 1,000)
        Assert.Equal(0m, data.GetProperty("balance")[11].GetDecimal());          // fully repaid
    }

    [Fact]
    public async Task Finance_Emi_Has_No_Chart()
    {
        string html = await PostFormAsync("/Finance?calculation=Emi",
            new() { ["Calculation"] = "Emi", ["Amount"] = "100000", ["Rate"] = "12", ["Months"] = "12" });

        Assert.DoesNotContain("loan-chart-data", html);
        Assert.DoesNotContain("chart.umd.min.js", html);
    }

    [Fact]
    public async Task Finance_Loan_Schedule_With_Bad_Input_Has_No_Chart()
    {
        string html = await PostFormAsync("/Finance?calculation=Amortization",
            new() { ["Calculation"] = "Amortization", ["Amount"] = "0", ["Rate"] = "12", ["Months"] = "12" });

        Assert.Contains("alert-danger", html);
        Assert.DoesNotContain("loan-chart-data", html);
    }

    [Fact]
    public async Task The_Chart_Library_File_Is_Served()
    {
        var response = await _client.GetAsync("/lib/chart.js/dist/chart.umd.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_Chart_Script_Is_Served()
    {
        var response = await _client.GetAsync("/js/loan-chart.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Finance_Loan_Schedule_Has_A_Download_Button_But_Emi_Does_Not()
    {
        var schedule = await _client.GetStringAsync("/Finance?calculation=Amortization");
        var emi = await _client.GetStringAsync("/Finance?calculation=Emi");

        Assert.Contains("Download CSV", schedule);
        Assert.DoesNotContain("Download CSV", emi);
    }

    [Fact]
    public async Task Finance_Loan_Schedule_Downloads_As_A_CSV_File()
    {
        var response = await PostFormResponseAsync(
            "/Finance?calculation=Amortization",
            new() { ["Calculation"] = "Amortization", ["Amount"] = "100000", ["Rate"] = "12", ["Months"] = "12" },
            postUrl: "/Finance?calculation=Amortization&handler=Csv");

        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("loan-schedule.csv", response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));

        string csv = await response.Content.ReadAsStringAsync();
        var lines = csv.TrimStart('\uFEFF').Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("Month,Payment,Principal,Interest,Balance", lines[0]);
        Assert.Equal(13, lines.Length);                                      // the header and 12 months
        Assert.Equal("1,8884.88,7884.88,1000.00,92115.12", lines[1]);
        Assert.EndsWith(",0.00", lines[12]);                                 // fully repaid
    }

    [Fact]
    public async Task Finance_Csv_With_Bad_Input_Shows_The_Error_Instead_Of_A_File()
    {
        var response = await PostFormResponseAsync(
            "/Finance?calculation=Amortization",
            new() { ["Calculation"] = "Amortization", ["Amount"] = "0", ["Rate"] = "12", ["Months"] = "12" },
            postUrl: "/Finance?calculation=Amortization&handler=Csv");

        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("alert-danger", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Finance_Csv_With_Missing_Fields_Asks_For_Them()
    {
        var response = await PostFormResponseAsync(
            "/Finance?calculation=Amortization",
            new() { ["Calculation"] = "Amortization", ["Amount"] = "100000" },
            postUrl: "/Finance?calculation=Amortization&handler=Csv");

        Assert.Contains("Please fill in", await response.Content.ReadAsStringAsync());
    }
    #endregion

    #region India
    [Fact]
    public async Task India_Fixed_Deposit_Shows_The_Maturity_Amount()
    {
        string html = await PostFormAsync("/India?calculation=FixedDeposit",
            new()
            {
                ["Calculation"] = "FixedDeposit",
                ["Amount"] = "100000",
                ["Rate"] = "7",
                ["Months"] = "12",
                ["Frequency"] = "Quarterly"
            });

        Assert.Contains("185.90", html);          // 1,07,185.90 (the digit grouping depends on the server)
        Assert.DoesNotContain("alert-danger", html);
    }

    [Fact]
    public async Task India_Ppf_Rejects_A_Term_That_Is_Not_Allowed()
    {
        string html = await PostFormAsync("/India?calculation=Ppf",
            new() { ["Calculation"] = "Ppf", ["Amount"] = "50000", ["Rate"] = "7.1", ["Years"] = "16" });

        Assert.Contains("alert-danger", html);
    }

    [Fact]
    public async Task India_Gst_Split_Shows_Cgst_And_Sgst()
    {
        string html = await PostFormAsync("/India?calculation=GstSplit",
            new() { ["Calculation"] = "GstSplit", ["Amount"] = "1000", ["Rate"] = "18", ["Mode"] = "Exclusive" });

        Assert.Contains("CGST", html);
        Assert.Contains("SGST", html);
        Assert.Contains("90.00", html);
    }

    [Fact]
    public async Task India_Income_Tax_Shows_The_Total_And_The_Slab_Breakdown()
    {
        string html = await PostFormAsync("/India?calculation=IncomeTax",
            new() { ["Calculation"] = "IncomeTax", ["Amount"] = "1500000", ["Salaried"] = "true" });

        Assert.Contains("97,500.00", html);        // total tax
        Assert.Contains("Income In Slab", html);   // the breakdown table
        Assert.Contains("33,750.00", html);        // tax in the 15% slab
        Assert.DoesNotContain("alert-danger", html);
    }

    [Fact]
    public async Task India_Income_Tax_Without_The_Salaried_Box_Has_No_Standard_Deduction()
    {
        string html = await PostFormAsync("/India?calculation=IncomeTax",
            new() { ["Calculation"] = "IncomeTax", ["Amount"] = "1300000", ["Salaried"] = "false" });

        Assert.Contains("78,000.00", html);
    }

    [Fact]
    public async Task India_Missing_Fields_Are_Listed()
    {
        string html = await PostFormAsync("/India?calculation=Ppf",
            new() { ["Calculation"] = "Ppf", ["Amount"] = "50000" });

        Assert.Contains("Please fill in", html);
    }
    #endregion

    #region Loans
    [Fact]
    public async Task Loans_Prepayment_Shows_The_Interest_Saved()
    {
        string html = await PostFormAsync("/Loans?tool=Prepayment",
            new()
            {
                ["Tool"] = "Prepayment",
                ["Amount"] = "100000",
                ["Rate"] = "12",
                ["Months"] = "12",
                ["PrepaymentMonth"] = "6",
                ["PrepaymentAmount"] = "20000"
            });

        Assert.Contains("1,075.12", html);
        Assert.DoesNotContain("alert-danger", html);
    }

    [Fact]
    public async Task Loans_Prepayment_At_The_Last_Month_Is_An_Error()
    {
        string html = await PostFormAsync("/Loans?tool=Prepayment",
            new()
            {
                ["Tool"] = "Prepayment",
                ["Amount"] = "100000",
                ["Rate"] = "12",
                ["Months"] = "12",
                ["PrepaymentMonth"] = "12",
                ["PrepaymentAmount"] = "20000"
            });

        Assert.Contains("alert-danger", html);
    }

    [Fact]
    public async Task Loans_Compare_Shows_The_Cheaper_Loan_In_A_Table()
    {
        string html = await PostFormAsync("/Loans?tool=Compare",
            new()
            {
                ["Tool"] = "Compare",
                ["Amount"] = "500000",
                ["Rate"] = "10",
                ["Months"] = "60",
                ["Fee"] = "5000",
                ["SecondAmount"] = "500000",
                ["SecondRate"] = "10.5",
                ["SecondMonths"] = "60",
                ["SecondFee"] = ""
            });

        Assert.Contains("Cheaper loan", html);
        Assert.Contains("2,405.80", html);              // the difference in total cost
        Assert.Contains("Total cost", html);            // a row of the side-by-side table
        Assert.DoesNotContain("alert-danger", html);
    }

    [Fact]
    public async Task Loans_Compare_Identical_Loans_Cost_The_Same()
    {
        string html = await PostFormAsync("/Loans?tool=Compare",
            new()
            {
                ["Tool"] = "Compare",
                ["Amount"] = "1000000",
                ["Rate"] = "8.5",
                ["Months"] = "240",
                ["SecondAmount"] = "1000000",
                ["SecondRate"] = "8.5",
                ["SecondMonths"] = "240"
            });

        Assert.Contains("Both cost the same", html);
    }

    [Fact]
    public async Task Loans_Compare_Says_Which_Loan_Is_Invalid()
    {
        string html = await PostFormAsync("/Loans?tool=Compare",
            new()
            {
                ["Tool"] = "Compare",
                ["Amount"] = "500000",
                ["Rate"] = "10",
                ["Months"] = "60",
                ["SecondAmount"] = "500000",
                ["SecondRate"] = "10",
                ["SecondMonths"] = "0"
            });

        Assert.Contains("alert-danger", html);
        Assert.Contains("Second loan", html);
    }

    [Fact]
    public async Task Loans_Borrow_Shows_The_Largest_Loan()
    {
        string html = await PostFormAsync("/Loans?tool=Borrow",
            new() { ["Tool"] = "Borrow", ["MonthlyPayment"] = "20000", ["Rate"] = "9", ["Months"] = "240" });

        Assert.Contains("899.08", html);                // 2,222,899.08 (the digit grouping depends on the server)
        Assert.DoesNotContain("alert-danger", html);
    }

    [Fact]
    public async Task Loans_Missing_Fields_Are_Listed()
    {
        string html = await PostFormAsync("/Loans?tool=Borrow",
            new() { ["Tool"] = "Borrow", ["MonthlyPayment"] = "20000", ["Rate"] = "9" });

        Assert.Contains("Please fill in", html);
    }
    #endregion

    #region Statistics
    [Fact]
    public async Task Statistics_Shows_Every_Measure()
    {
        string html = await PostFormAsync("/Statistics", new() { ["Values"] = "2, 4, 4, 4, 5, 5, 7, 9" });

        Assert.DoesNotContain("alert-danger", html);
        Assert.Equal("8", DetailValue(html, "Count"));
        Assert.Equal("40", DetailValue(html, "Sum"));
        Assert.Equal("2", DetailValue(html, "Minimum"));
        Assert.Equal("9", DetailValue(html, "Maximum"));
        Assert.Equal("5", DetailValue(html, "Mean"));
        Assert.Equal("4.5", DetailValue(html, "Median"));
        Assert.Equal("4", DetailValue(html, "Mode"));
        Assert.Equal("2", DetailValue(html, "Standard deviation (population)"));
        Assert.Equal("2.14", DetailValue(html, "Standard deviation (sample)"));
    }

    [Fact]
    public async Task Statistics_Accepts_Spaces_And_New_Lines()
    {
        string html = await PostFormAsync("/Statistics", new() { ["Values"] = "2 4 4 4\r\n5 5 7 9" });

        Assert.Equal("8", DetailValue(html, "Count"));
        Assert.Equal("5", DetailValue(html, "Mean"));
    }

    [Fact]
    public async Task Statistics_Shows_Every_Tied_Mode()
    {
        string html = await PostFormAsync("/Statistics", new() { ["Values"] = "1 2 2 3 3" });

        Assert.Equal("2, 3", DetailValue(html, "Mode"));
    }

    [Fact]
    public async Task Statistics_Says_When_There_Is_No_Mode()
    {
        string html = await PostFormAsync("/Statistics", new() { ["Values"] = "1 2 3" });

        Assert.Equal("None (no value repeats)", DetailValue(html, "Mode"));
    }

    [Fact]
    public async Task Statistics_Of_One_Number_Has_No_Sample_Standard_Deviation()
    {
        string html = await PostFormAsync("/Statistics", new() { ["Values"] = "5" });

        Assert.Equal("0", DetailValue(html, "Standard deviation (population)"));
        Assert.Equal("Needs at least two numbers", DetailValue(html, "Standard deviation (sample)"));
    }

    [Fact]
    public async Task Statistics_Names_The_Entry_That_Is_Not_A_Number()
    {
        string html = await PostFormAsync("/Statistics", new() { ["Values"] = "1, abc" });

        Assert.Contains("alert-danger", html);
        Assert.Contains("abc", html);
        Assert.Contains("is not a number", html);
    }

    [Fact]
    public async Task Statistics_Empty_Box_Asks_For_Numbers()
    {
        string html = await PostFormAsync("/Statistics", new() { ["Values"] = "" });

        Assert.Contains("Please enter at least one number", html);
    }

    [Fact]
    public async Task Statistics_Numbers_Too_Large_To_Add_Show_An_Error()
    {
        string html = await PostFormAsync("/Statistics",
            new() { ["Values"] = "79228162514264337593543950335 79228162514264337593543950335" });

        Assert.Contains("alert-danger", html);
    }
    #endregion

    #region Unit converter
    [Fact]
    public async Task Units_Opens_On_Length_With_Length_Units()
    {
        string html = await _client.GetStringAsync("/Units");

        Assert.Contains("Mile", html);
        Assert.Contains("Kilometre", html);
        Assert.DoesNotContain("Celsius", html.Substring(html.IndexOf("<form", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Units_Temperature_Tab_Lists_Temperature_Units()
    {
        string html = await _client.GetStringAsync("/Units?category=Temperature");
        string form = html.Substring(html.IndexOf("<form", StringComparison.Ordinal));

        Assert.Contains("Celsius", form);
        Assert.Contains("Fahrenheit", form);
        Assert.Contains("Kelvin", form);
        Assert.DoesNotContain("Mile", form);
    }

    [Fact]
    public async Task Units_Has_A_Tab_For_Every_Category()
    {
        string html = await _client.GetStringAsync("/Units");

        foreach (string category in new[] { "Length", "Weight", "Temperature", "Area", "Volume" })
        {
            Assert.Contains($"category={category}", html);
        }
    }

    [Fact]
    public async Task Units_Mile_To_Kilometre_Shows_The_Result_And_A_Sentence()
    {
        string html = await PostFormAsync("/Units?category=Length",
            new() { ["Value"] = "1", ["From"] = "Mile", ["To"] = "Kilometre" });

        Assert.DoesNotContain("alert-danger", html);
        Assert.Contains(">1.609344</p>", html);
        Assert.Contains("1 Mile = 1.609344 Kilometre", html);
    }

    [Fact]
    public async Task Units_Whole_Results_Have_No_Trailing_Zeros()
    {
        string html = await PostFormAsync("/Units?category=Length",
            new() { ["Value"] = "1", ["From"] = "Metre", ["To"] = "Millimetre" });

        Assert.Contains(">1000</p>", html);
    }

    [Fact]
    public async Task Units_Celsius_To_Fahrenheit_Works()
    {
        string html = await PostFormAsync("/Units?category=Temperature",
            new() { ["Value"] = "100", ["From"] = "Celsius", ["To"] = "Fahrenheit" });

        Assert.Contains(">212</p>", html);
    }

    [Fact]
    public async Task Units_Below_Absolute_Zero_Shows_An_Error()
    {
        string html = await PostFormAsync("/Units?category=Temperature",
            new() { ["Value"] = "-300", ["From"] = "Celsius", ["To"] = "Fahrenheit" });

        Assert.Contains("alert-danger", html);
        Assert.Contains("absolute zero", html);
    }

    [Fact]
    public async Task Units_From_Different_Categories_Show_An_Error()
    {
        string html = await PostFormAsync("/Units?category=Length",
            new() { ["Value"] = "1", ["From"] = "Metre", ["To"] = "Kilogram" });

        Assert.Contains("alert-danger", html);
        Assert.Contains("Cannot convert", html);
    }

    [Fact]
    public async Task Units_Empty_Value_Asks_For_It()
    {
        string html = await PostFormAsync("/Units?category=Length",
            new() { ["Value"] = "", ["From"] = "Metre", ["To"] = "Foot" });

        Assert.Contains("Please enter the value", html);
    }

    [Fact]
    public async Task Units_Unknown_Category_Still_Opens_The_Page()
    {
        var response = await _client.GetAsync("/Units?category=Nope");

        response.EnsureSuccessStatusCode();
    }
    #endregion

    #region Navigation
    [Fact]
    public async Task The_Menu_Links_To_Statistics_And_Units()
    {
        string html = await _client.GetStringAsync("/");

        Assert.Contains("href=\"/Statistics\"", html);
        Assert.Contains("href=\"/Units\"", html);
    }

    [Fact]
    public async Task Home_Page_Has_Cards_For_Statistics_And_Units()
    {
        string html = await _client.GetStringAsync("/");

        Assert.Matches("card-title\">Statistics</h2>", html);
        Assert.Matches("card-title\">Unit converter</h2>", html);
        Assert.Matches("<a [^>]*href=\"/Statistics\"[^>]*>Open</a>", html);
        Assert.Matches("<a [^>]*href=\"/Units\"[^>]*>Open</a>", html);
    }
    #endregion

    #region Date calculator
    [Fact]
    public async Task Dates_Has_A_Tab_For_Every_Tool_And_Real_Date_Pickers()
    {
        string html = await _client.GetStringAsync("/Dates");

        Assert.Contains("tool=Age", html);
        Assert.Contains("tool=Between", html);
        Assert.Contains("tool=Add", html);
        Assert.Contains("type=\"date\"", html);
    }

    [Fact]
    public async Task Dates_Age_Shows_Years_Months_Days_And_Total_Days()
    {
        string html = await PostFormAsync("/Dates?tool=Age",
            new() { ["FirstDate"] = "2000-01-15", ["SecondDate"] = "2026-11-27" });

        Assert.DoesNotContain("alert-danger", html);
        Assert.Contains(">26 years, 10 months, 12 days</p>", html);
        Assert.Equal("9813", DetailValue(html, "Total days"));
        Assert.Contains("Age on Friday, 27 November 2026.", html);
    }

    [Fact]
    public async Task Dates_The_Form_Keeps_The_Dates_That_Were_Entered()
    {
        string html = await PostFormAsync("/Dates?tool=Age",
            new() { ["FirstDate"] = "2000-01-15", ["SecondDate"] = "2026-11-27" });

        Assert.Contains("value=\"2000-01-15\"", html);
        Assert.Contains("value=\"2026-11-27\"", html);
    }

    [Fact]
    public async Task Dates_Age_Uses_Singular_Words_For_One()
    {
        string html = await PostFormAsync("/Dates?tool=Age",
            new() { ["FirstDate"] = "2000-02-29", ["SecondDate"] = "2001-02-28" });

        Assert.Contains(">1 year, 0 months, 0 days</p>", html);
    }

    [Fact]
    public async Task Dates_Age_Without_An_End_Date_Uses_Todays_Date_In_India()
    {
        // 26 November 20:00 UTC is already 27 November, 01:30, in India.
        var client = ClientWithClock(new DateTimeOffset(2026, 11, 26, 20, 0, 0, TimeSpan.Zero));

        string html = await PostFormAsync("/Dates?tool=Age",
            new() { ["FirstDate"] = "2000-01-15", ["SecondDate"] = "" }, client);

        Assert.Contains(">26 years, 10 months, 12 days</p>", html);
        Assert.Contains("Age on Friday, 27 November 2026.", html);
    }

    [Fact]
    public async Task Dates_Age_Before_The_Birth_Date_Shows_An_Error()
    {
        string html = await PostFormAsync("/Dates?tool=Age",
            new() { ["FirstDate"] = "2026-11-28", ["SecondDate"] = "2026-11-27" });

        Assert.Contains("alert-danger", html);
        Assert.Contains("cannot be after", html);
    }

    [Fact]
    public async Task Dates_Age_Without_A_Birth_Date_Asks_For_It()
    {
        string html = await PostFormAsync("/Dates?tool=Age",
            new() { ["FirstDate"] = "", ["SecondDate"] = "2026-11-27" });

        Assert.Contains("Please enter the date of birth", html);
    }

    [Fact]
    public async Task Dates_Between_Shows_The_Gap_In_Either_Order()
    {
        string forwards = await PostFormAsync("/Dates?tool=Between",
            new() { ["FirstDate"] = "2024-01-01", ["SecondDate"] = "2025-01-01" });
        string backwards = await PostFormAsync("/Dates?tool=Between",
            new() { ["FirstDate"] = "2025-01-01", ["SecondDate"] = "2024-01-01" });

        Assert.Contains(">1 year, 0 months, 0 days</p>", forwards);
        Assert.Equal("366", DetailValue(forwards, "Total days"));
        Assert.Contains(">1 year, 0 months, 0 days</p>", backwards);
        Assert.Equal("366", DetailValue(backwards, "Total days"));
    }

    [Fact]
    public async Task Dates_Between_Needs_Both_Dates()
    {
        string html = await PostFormAsync("/Dates?tool=Between",
            new() { ["FirstDate"] = "2024-01-01", ["SecondDate"] = "" });

        Assert.Contains("Please enter both dates", html);
    }

    [Theory]
    [InlineData("2024-01-31", "1", "Months", "Thursday, 29 February 2024")]    // the month is shorter, so the last day
    [InlineData("2024-03-31", "-1", "Months", "Thursday, 29 February 2024")]   // a minus sign goes back
    [InlineData("2024-02-28", "2", "Days", "Friday, 1 March 2024")]
    [InlineData("2024-03-01", "-1", "Days", "Thursday, 29 February 2024")]
    public async Task Dates_Add_Shows_The_New_Date_With_Its_Weekday(string date, string amount, string unit, string expected)
    {
        string html = await PostFormAsync("/Dates?tool=Add",
            new() { ["FirstDate"] = date, ["Amount"] = amount, ["Unit"] = unit });

        Assert.DoesNotContain("alert-danger", html);
        Assert.Contains($">{expected}</p>", html);
    }

    [Fact]
    public async Task Dates_Add_Past_The_Last_Supported_Year_Shows_An_Error()
    {
        string html = await PostFormAsync("/Dates?tool=Add",
            new() { ["FirstDate"] = "9999-12-31", ["Amount"] = "1", ["Unit"] = "Days" });

        Assert.Contains("alert-danger", html);
    }

    [Fact]
    public async Task Dates_Add_Without_An_Amount_Asks_For_It()
    {
        string html = await PostFormAsync("/Dates?tool=Add",
            new() { ["FirstDate"] = "2024-01-31", ["Amount"] = "", ["Unit"] = "Months" });

        Assert.Contains("Please enter how many", html);
    }

    [Fact]
    public async Task Dates_A_Date_That_Is_Not_A_Date_Shows_An_Error()
    {
        string html = await PostFormAsync("/Dates?tool=Age",
            new() { ["FirstDate"] = "notadate", ["SecondDate"] = "2026-11-27" });

        Assert.Contains("alert-danger", html);
        Assert.Contains("valid dates", html);
    }
    #endregion

    #region Programmer calculator
    [Fact]
    public async Task Programmer_Has_A_Tab_For_Each_Tool()
    {
        string html = await _client.GetStringAsync("/Programmer");

        Assert.Contains("tool=Convert", html);
        Assert.Contains("tool=Bitwise", html);
    }

    [Theory]
    [InlineData("255", "Decimal")]
    [InlineData("FF", "Hexadecimal")]
    [InlineData("0xff", "Hexadecimal")]
    [InlineData("377", "Octal")]
    [InlineData("1111 1111", "Binary")]          // spaces from a pasted, grouped number are ignored
    [InlineData("1111_1111", "Binary")]          // and so are underscores
    public async Task Programmer_Convert_Shows_The_Number_In_Every_Base(string number, string inputBase)
    {
        string html = await PostFormAsync("/Programmer?tool=Convert",
            new() { ["Number"] = number, ["InputBase"] = inputBase });

        Assert.DoesNotContain("alert-danger", html);
        Assert.Equal("255", DetailValue(html, "Decimal"));
        Assert.Equal("1111 1111", DetailValue(html, "Binary"));
        Assert.Equal("377", DetailValue(html, "Octal"));
        Assert.Equal("FF", DetailValue(html, "Hexadecimal"));
    }

    [Fact]
    public async Task Programmer_Convert_Shows_Negative_Numbers_As_64_Bits()
    {
        string html = await PostFormAsync("/Programmer?tool=Convert",
            new() { ["Number"] = "-1", ["InputBase"] = "Decimal" });

        Assert.Equal("-1", DetailValue(html, "Decimal"));
        Assert.Equal(string.Join(" ", Enumerable.Repeat("1111", 16)), DetailValue(html, "Binary"));
        Assert.Equal("FFFFFFFFFFFFFFFF", DetailValue(html, "Hexadecimal"));
        Assert.Contains("64 bits", html);
    }

    [Fact]
    public async Task Programmer_Convert_Names_The_Wrong_Digit()
    {
        string html = await PostFormAsync("/Programmer?tool=Convert",
            new() { ["Number"] = "102", ["InputBase"] = "Binary" });

        Assert.Contains("alert-danger", html);
        Assert.Contains("not a valid binary digit", html);
    }

    [Fact]
    public async Task Programmer_Convert_Reports_A_Number_That_Is_Too_Big()
    {
        string html = await PostFormAsync("/Programmer?tool=Convert",
            new() { ["Number"] = "9223372036854775808", ["InputBase"] = "Decimal" });

        Assert.Contains("alert-danger", html);
        Assert.Contains("does not fit", html);
    }

    [Fact]
    public async Task Programmer_Convert_Empty_Box_Asks_For_A_Number()
    {
        string html = await PostFormAsync("/Programmer?tool=Convert",
            new() { ["Number"] = "  ", ["InputBase"] = "Decimal" });

        Assert.Contains("Please enter a number", html);
    }

    [Fact]
    public async Task Programmer_Bitwise_And_Shows_The_Result_In_Every_Base()
    {
        string html = await PostFormAsync("/Programmer?tool=Bitwise",
            new() { ["Left"] = "12", ["Operation"] = "And", ["Right"] = "10", ["InputBase"] = "Decimal" });

        Assert.Equal("8", DetailValue(html, "Decimal"));
        Assert.Equal("1000", DetailValue(html, "Binary"));
    }

    [Fact]
    public async Task Programmer_Bitwise_Reads_The_Numbers_In_The_Chosen_Base()
    {
        string html = await PostFormAsync("/Programmer?tool=Bitwise",
            new() { ["Left"] = "F0F0", ["Operation"] = "And", ["Right"] = "FF00", ["InputBase"] = "Hexadecimal" });

        Assert.Equal("F000", DetailValue(html, "Hexadecimal"));
        Assert.Equal("61440", DetailValue(html, "Decimal"));
    }

    [Fact]
    public async Task Programmer_Not_Needs_Only_The_First_Number()
    {
        string html = await PostFormAsync("/Programmer?tool=Bitwise",
            new() { ["Left"] = "5", ["Operation"] = "Not", ["Right"] = "", ["InputBase"] = "Decimal" });

        Assert.DoesNotContain("alert-danger", html);
        Assert.Equal("-6", DetailValue(html, "Decimal"));
    }

    [Fact]
    public async Task Programmer_Shift_Left_Works()
    {
        string html = await PostFormAsync("/Programmer?tool=Bitwise",
            new() { ["Left"] = "1", ["Operation"] = "ShiftLeft", ["Right"] = "4", ["InputBase"] = "Decimal" });

        Assert.Equal("16", DetailValue(html, "Decimal"));
    }

    [Fact]
    public async Task Programmer_Shifting_By_64_Shows_An_Error()
    {
        string html = await PostFormAsync("/Programmer?tool=Bitwise",
            new() { ["Left"] = "1", ["Operation"] = "ShiftLeft", ["Right"] = "64", ["InputBase"] = "Decimal" });

        Assert.Contains("alert-danger", html);
        Assert.Contains("0 to 63", html);
    }

    [Fact]
    public async Task Programmer_Bitwise_Asks_For_The_Missing_Numbers()
    {
        string noRight = await PostFormAsync("/Programmer?tool=Bitwise",
            new() { ["Left"] = "5", ["Operation"] = "And", ["Right"] = "", ["InputBase"] = "Decimal" });
        string noLeft = await PostFormAsync("/Programmer?tool=Bitwise",
            new() { ["Left"] = "", ["Operation"] = "And", ["Right"] = "5", ["InputBase"] = "Decimal" });

        Assert.Contains("Please enter the second number", noRight);
        Assert.Contains("Please enter the first number", noLeft);
    } 
    #endregion

    // ---- Menu and home page ----

    [Fact]
    public async Task The_Menu_And_Home_Page_Link_To_Dates_And_Programmer()
    {
        string html = await _client.GetStringAsync("/");

        Assert.Matches("<a [^>]*href=\"/Dates\"[^>]*>Dates</a>", html);
        Assert.Matches("<a [^>]*href=\"/Programmer\"[^>]*>Programmer</a>", html);
        Assert.Matches("card-title\">Date calculator</h2>", html);
        Assert.Matches("card-title\">Programmer calculator</h2>", html);
        Assert.Matches("<a [^>]*href=\"/Dates\"[^>]*>Open</a>", html);
        Assert.Matches("<a [^>]*href=\"/Programmer\"[^>]*>Open</a>", html);
    }
}