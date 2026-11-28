using System.Globalization;

namespace Prahelika.Calculators.Web.Models;

/// <summary>Reads a list of numbers typed into a text box: separated by commas, semicolons, spaces or new lines.</summary>
public static class NumberListParser
{
    private static readonly char[] Separators = { ',', ';', ' ', '\t', '\r', '\n' };

    public static bool TryParse(string? text, out decimal[] numbers, out string error)
    {
        numbers = [];
        error = string.Empty;

        string[] parts = (text ?? string.Empty).Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            error = "Please enter at least one number.";
            return false;
        }

        var parsed = new List<decimal>();

        foreach (string part in parts)
        {
            // Invariant culture, so 1.5 means one and a half whatever the server's language is.
            if (!decimal.TryParse(part, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out decimal number))
            {
                error = $"'{part}' is not a number.";
                return false;
            }

            parsed.Add(number);
        }

        numbers = parsed.ToArray();
        return true;
    }
}