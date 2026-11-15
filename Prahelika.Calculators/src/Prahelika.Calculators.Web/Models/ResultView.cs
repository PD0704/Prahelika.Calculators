using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Prahelika.Calculators.Web.Models;

/// <summary>A display-friendly version of any calculator result.</summary>
public sealed class ResultView
{
    private static readonly CultureInfo MoneyCulture = LoadMoneyCulture();

    /// <summary>Set when the result is a single value, such as 5.</summary>
    public string? Summary { get; private set; }

    /// <summary>Label and value rows, used when the result is a record (EMI summary, SIP result, ...).</summary>
    public List<KeyValuePair<string, string>> Details { get; } = new();

    /// <summary>Column names and rows, used when the result is a list (the loan schedule).</summary>
    public List<string> Columns { get; } = new();
    public List<List<string>> Rows { get; } = new();

    public static ResultView From(object? value, bool money)
    {
        var view = new ResultView();

        switch (value)
        {
            case null:
                break;

            case string or IFormattable:
                view.Summary = Format(value, money);
                break;

            case IEnumerable items:
                var list = items.Cast<object>().ToList();
                if (list.Count == 0)
                {
                    break;
                }

                var columns = ReadableProperties(list[0].GetType());
                view.Columns.AddRange(columns.Select(p => Label(p.Name)));
                foreach (var item in list)
                {
                    view.Rows.Add(columns.Select(p => Format(p.GetValue(item), money)).ToList());
                }
                break;

            default:
                foreach (var property in ReadableProperties(value.GetType()))
                {
                    view.Details.Add(new(Label(property.Name), Format(property.GetValue(value), money)));
                }
                break;
        }

        return view;
    }

    private static PropertyInfo[] ReadableProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .ToArray();

    // "TotalInterest" becomes "Total Interest".
    private static string Label(string name) => Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");

    private static string Format(object? value, bool money) => value switch
    {
        null => string.Empty,
        // Money: two decimals with Indian digit grouping where the server supports it.
        decimal d when money => d.ToString("N2", MoneyCulture),
        // Plain decimals: drop trailing zeros (150.0 shows as 150).
        decimal d => (d / 1.0000000000000000000000000000m).ToString("G29", CultureInfo.InvariantCulture),
        double x => x.ToString("G12", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    private static CultureInfo LoadMoneyCulture()
    {
        try
        {
            return CultureInfo.GetCultureInfo("en-IN");
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture; // servers running without culture data
        }
    }
}