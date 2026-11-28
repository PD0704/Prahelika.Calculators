using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Text.Json;
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
    // and send it back with the form, like a browser does. The form can be sent to a
    // different address than the one the token was read from (for example, a named handler).
    private async Task<HttpResponseMessage> PostFormResponseAsync(
        string url, Dictionary<string, string> fields, string? postUrl = null)
    {
        var pageResponse = await _client.GetAsync(url);
        Assert.True(pageResponse.IsSuccessStatusCode,
            $"GET {url} returned {(int)pageResponse.StatusCode}, redirect to: {pageResponse.Headers.Location}");
        string page = await pageResponse.Content.ReadAsStringAsync();
        var match = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "The page has no anti-forgery token.");

        fields["__RequestVerificationToken"] = match.Groups[1].Value;
        var response = await _client.PostAsync(postUrl ?? url, new FormUrlEncodedContent(fields));
        Assert.True(response.IsSuccessStatusCode,
            $"POST to {postUrl ?? url} returned {(int)response.StatusCode}, redirect to: {response.Headers.Location}");
        return response;
    }

    private async Task<string> PostFormAsync(string url, Dictionary<string, string> fields)
    {
        var response = await PostFormResponseAsync(url, fields);
        return await response.Content.ReadAsStringAsync();
    }

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
    public async Task Basic_Percentage_Is_B_Percent_Of_A()
    {
        string html = await PostFormAsync("/Basic",
            new() { ["Operation"] = "Percentage", ["A"] = "150", ["B"] = "20" });

        Assert.Contains(">30</p>", html);
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

    // ---- Statistics ----

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

    // ---- Unit converter ----

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

    // ---- Navigation ----

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
}