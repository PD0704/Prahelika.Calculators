using System.Globalization;
using System.Text;
using Prahelika.Calculators.Core.Finance;

namespace Prahelika.Calculators.Web.Models;

/// <summary>Writes a loan repayment schedule as CSV: one line per month, ready for Excel.</summary>
public static class LoanScheduleCsv
{
    public const string FileName = "loan-schedule.csv";
    public const string ContentType = "text/csv";

    /// <summary>
    /// The CSV text. Amounts always have two decimals and a dot, whatever the computer's language is,
    /// so the file means the same everywhere. Lines end with CR LF, as the CSV standard (RFC 4180) says.
    /// </summary>
    public static string Build(IReadOnlyList<AmortizationRow> schedule)
    {
        var csv = new StringBuilder();
        csv.Append("Month,Payment,Principal,Interest,Balance\r\n");

        foreach (var row in schedule)
        {
            csv.Append(row.Month.ToString(CultureInfo.InvariantCulture)).Append(',')
               .Append(Amount(row.Payment)).Append(',')
               .Append(Amount(row.PrincipalPaid)).Append(',')
               .Append(Amount(row.InterestPaid)).Append(',')
               .Append(Amount(row.RemainingBalance))
               .Append("\r\n");
        }

        return csv.ToString();
    }

    /// <summary>The text as UTF-8 bytes, starting with the three-byte marker (BOM) that tells Excel the file is UTF-8.</summary>
    public static byte[] ToBytes(string csv)
    {
        byte[] marker = Encoding.UTF8.GetPreamble();
        byte[] body = Encoding.UTF8.GetBytes(csv);

        return [.. marker, .. body];
    }

    private static string Amount(decimal value) => value.ToString("F2", CultureInfo.InvariantCulture);
}