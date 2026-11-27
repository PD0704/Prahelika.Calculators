namespace Prahelika.Calculators.Core.Units;

/// <summary>Default implementation of <see cref="IUnitConverter"/> using <see cref="decimal"/> arithmetic.</summary>
public sealed class UnitConverter : IUnitConverter
{
    private readonly record struct UnitInfo(UnitCategory Category, decimal ToBase);

    // How big each unit is in its category's base unit: metre, gram, square metre, litre.
    // These are the exact legal definitions (a mile is exactly 1609.344 m, a pound exactly 453.59237 g), not approximations.
    // Temperature needs a formula instead of a factor (see ToCelsius), so its factor is unused and set to 1.
    private static readonly (MeasurementUnit Unit, UnitCategory Category, decimal ToBase)[] Table =
    {
        (MeasurementUnit.Millimetre, UnitCategory.Length, 0.001m),
        (MeasurementUnit.Centimetre, UnitCategory.Length, 0.01m),
        (MeasurementUnit.Metre, UnitCategory.Length, 1m),
        (MeasurementUnit.Kilometre, UnitCategory.Length, 1000m),
        (MeasurementUnit.Inch, UnitCategory.Length, 0.0254m),
        (MeasurementUnit.Foot, UnitCategory.Length, 0.3048m),
        (MeasurementUnit.Yard, UnitCategory.Length, 0.9144m),
        (MeasurementUnit.Mile, UnitCategory.Length, 1609.344m),

        (MeasurementUnit.Milligram, UnitCategory.Weight, 0.001m),
        (MeasurementUnit.Gram, UnitCategory.Weight, 1m),
        (MeasurementUnit.Kilogram, UnitCategory.Weight, 1000m),
        (MeasurementUnit.Tonne, UnitCategory.Weight, 1000000m),
        (MeasurementUnit.Ounce, UnitCategory.Weight, 28.349523125m),
        (MeasurementUnit.Pound, UnitCategory.Weight, 453.59237m),

        (MeasurementUnit.Celsius, UnitCategory.Temperature, 1m),
        (MeasurementUnit.Fahrenheit, UnitCategory.Temperature, 1m),
        (MeasurementUnit.Kelvin, UnitCategory.Temperature, 1m),

        (MeasurementUnit.SquareCentimetre, UnitCategory.Area, 0.0001m),
        (MeasurementUnit.SquareMetre, UnitCategory.Area, 1m),
        (MeasurementUnit.Hectare, UnitCategory.Area, 10000m),
        (MeasurementUnit.SquareKilometre, UnitCategory.Area, 1000000m),
        (MeasurementUnit.SquareInch, UnitCategory.Area, 0.00064516m),
        (MeasurementUnit.SquareFoot, UnitCategory.Area, 0.09290304m),
        (MeasurementUnit.Acre, UnitCategory.Area, 4046.8564224m),

        (MeasurementUnit.Millilitre, UnitCategory.Volume, 0.001m),
        (MeasurementUnit.Litre, UnitCategory.Volume, 1m),
        (MeasurementUnit.CubicMetre, UnitCategory.Volume, 1000m),
        (MeasurementUnit.UsFluidOunce, UnitCategory.Volume, 0.0295735295625m),
        (MeasurementUnit.UsGallon, UnitCategory.Volume, 3.785411784m),
    };

    private static readonly Dictionary<MeasurementUnit, UnitInfo> Lookup =
        Table.ToDictionary(row => row.Unit, row => new UnitInfo(row.Category, row.ToBase));

    private const decimal AbsoluteZeroCelsius = -273.15m;

    /// <param name="decimalPlaces">How many decimal places results are rounded to (0 to 10).</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="decimalPlaces"/> is not between 0 and 10.</exception>
    public UnitConverter(int decimalPlaces = 6)
    {
        if (decimalPlaces is < 0 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces), decimalPlaces, "Decimal places must be between 0 and 10.");
        }

        DecimalPlaces = decimalPlaces;
    }

    /// <inheritdoc />
    public int DecimalPlaces { get; }

    /// <inheritdoc />
    public UnitCategory CategoryOf(MeasurementUnit unit)
    {
        if (!Lookup.TryGetValue(unit, out UnitInfo info))
        {
            throw new ArgumentOutOfRangeException(nameof(unit), unit, "Not a known unit.");
        }

        return info.Category;
    }

    /// <inheritdoc />
    public IReadOnlyList<MeasurementUnit> UnitsIn(UnitCategory category) =>
        Table.Where(row => row.Category == category).Select(row => row.Unit).ToArray();

    /// <inheritdoc />
    public CalcResult<decimal> Convert(decimal value, MeasurementUnit from, MeasurementUnit to)
    {
        if (!Lookup.TryGetValue(from, out UnitInfo fromInfo) || !Lookup.TryGetValue(to, out UnitInfo toInfo))
        {
            return CalcResult<decimal>.Failure(CalcError.InvalidInput, "That unit is not supported.");
        }

        if (fromInfo.Category != toInfo.Category)
        {
            return CalcResult<decimal>.Failure(
                CalcError.InvalidInput,
                $"Cannot convert {from} to {to}: one measures {fromInfo.Category.ToString().ToLowerInvariant()} and the other {toInfo.Category.ToString().ToLowerInvariant()}.");
        }

        try
        {
            decimal converted;

            if (fromInfo.Category == UnitCategory.Temperature)
            {
                decimal celsius = ToCelsius(value, from);
                if (celsius < AbsoluteZeroCelsius)
                {
                    return CalcResult<decimal>.Failure(CalcError.InvalidInput, "That temperature is below absolute zero.");
                }

                converted = FromCelsius(celsius, to);
            }
            else
            {
                converted = value * fromInfo.ToBase / toInfo.ToBase;
            }

            return CalcResult<decimal>.Success(Math.Round(converted, DecimalPlaces, MidpointRounding.AwayFromZero));
        }
        catch (OverflowException)
        {
            return CalcResult<decimal>.Failure(CalcError.Overflow, "The result is too large or too small to represent.");
        }
    }

    // Temperature goes through Celsius: any scale to Celsius, then Celsius to the target scale.
    private static decimal ToCelsius(decimal value, MeasurementUnit unit) => unit switch
    {
        MeasurementUnit.Fahrenheit => (value - 32m) * 5m / 9m,
        MeasurementUnit.Kelvin => value - 273.15m,
        _ => value
    };

    private static decimal FromCelsius(decimal celsius, MeasurementUnit unit) => unit switch
    {
        MeasurementUnit.Fahrenheit => celsius * 9m / 5m + 32m,
        MeasurementUnit.Kelvin => celsius + 273.15m,
        _ => celsius
    };
}