using System.Text.Json;
using Prahelika.Calculators.Core.Finance;

namespace Prahelika.Calculators.Web.Models;

/// <summary>The numbers behind the loan charts, as plain lists that the browser can read as JSON.</summary>
public sealed record LoanChartData(
    IReadOnlyList<int> Months,
    IReadOnlyList<decimal> Balance,
    IReadOnlyList<decimal> Principal,
    IReadOnlyList<decimal> Interest)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static LoanChartData From(IReadOnlyList<AmortizationRow> schedule) => new(
        schedule.Select(row => row.Month).ToList(),
        schedule.Select(row => row.RemainingBalance).ToList(),
        schedule.Select(row => row.PrincipalPaid).ToList(),
        schedule.Select(row => row.InterestPaid).ToList());

    /// <summary>The data as JSON with lower-case names, for example {"months":[1,2],"balance":[...]}.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);
}