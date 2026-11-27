namespace Prahelika.Calculators.Core.Units;

/// <summary>Converts between units of length, weight, temperature, area and volume, using <see cref="decimal"/> arithmetic.</summary>
public interface IUnitConverter
{
    /// <summary>The number of decimal places that results are rounded to.</summary>
    int DecimalPlaces { get; }

    /// <summary>The category a unit belongs to.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a defined unit.</exception>
    UnitCategory CategoryOf(MeasurementUnit unit);

    /// <summary>All the units in a category, in the order they should be shown.</summary>
    IReadOnlyList<MeasurementUnit> UnitsIn(UnitCategory category);

    /// <summary>
    /// Converts a value from one unit to another. Both units must be in the same category.
    /// Temperatures below absolute zero (-273.15 °C) are rejected.
    /// </summary>
    CalcResult<decimal> Convert(decimal value, MeasurementUnit from, MeasurementUnit to);
}