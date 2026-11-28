using System.Text.RegularExpressions;
using Prahelika.Calculators.Core.Units;

namespace Prahelika.Calculators.Web.Models;

/// <summary>Friendly names for units: SquareMetre becomes "Square metre", UsGallon becomes "US gallon".</summary>
public static class UnitNames
{
    public static string For(MeasurementUnit unit)
    {
        string spaced = Regex.Replace(unit.ToString(), "(?<=[a-z])(?=[A-Z])", " ");   // "Square Metre"
        string name = spaced[..1] + spaced[1..].ToLowerInvariant();                    // "Square metre"

        return name.StartsWith("Us ", StringComparison.Ordinal) ? "US" + name[2..] : name;   // "US gallon"
    }
}