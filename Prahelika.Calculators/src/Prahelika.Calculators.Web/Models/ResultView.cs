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

    /// <summary>Column names and rows, used when the result is a list (the loan schedule), or has a list inside it.</summary>
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
                view.AddTable(items, money);
                break;

            default:
                foreach (var property in ReadableProperties(value.GetType()))
                {
                    object? propertyValue = property.GetValue(value);

                    // A list inside a record (such as the income-tax slab breakdown) becomes a table.
                    // Everything else becomes a label and a value.
                    if (propertyValue is IEnumerable list and not string)
                    {
                        view.AddTable(list, money);
                    }
                    else
                    {
                        view.Details.Add(new(Label(property.Name), Format(propertyValue, money)));
                    }
                }
                break;
        }

        return view;
    }

    /// <summary>Formats an amount of money like the result boxes do: two decimals, with Indian digit grouping where the server supports it.</summary>
    public static string Money(decimal value) => value.ToString("N2", MoneyCulture);

    /// <summary>
    /// A short text for the history list: the single value, or the first two rows of a record,
    /// or the size of a table.
    /// </summary>
    public string HistoryText
    {
        get
        {
            if (Summary is not null)
            {
                return Summary;
            }

            if (Details.Count > 0)
            {
                return string.Join("; ", Details.Take(2).Select(detail => $"{detail.Key}: {detail.Value}"));
            }

            return Rows.Count > 0
                ? $"Table with {Rows.Count} row{(Rows.Count == 1 ? string.Empty : "s")}"
                : string.Empty;
        }
    }

    /// <summary>Formats a plain number like the result boxes do: no trailing zeros (150.0 shows as 150).</summary>
    public static string Number(decimal value) => Format(value, money: false);

    private void AddTable(IEnumerable items, bool money)
    {
        var list = items.Cast<object>().ToList();
        if (list.Count == 0)
        {
            return;
        }

        var columns = ReadableProperties(list[0].GetType());
        Columns.AddRange(columns.Select(p => Label(p.Name)));
        foreach (var item in list)
        {
            Rows.Add(columns.Select(p => Format(p.GetValue(item), money)).ToList());
        }
    }

    private static PropertyInfo[] ReadableProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .ToArray();

    // "TotalInterest" becomes "Total Interest", and GST, CGST and SGST are written in capitals.
    private static string Label(string name)
    {
        string spaced = Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");
        return Regex.Replace(spaced, @"\b(Cgst|Sgst|Gst)\b", match => match.Value.ToUpperInvariant());
    }

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