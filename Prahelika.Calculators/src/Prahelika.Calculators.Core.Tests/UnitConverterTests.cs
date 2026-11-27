using System.Globalization;
using Prahelika.Calculators.Core.Units;
using static Prahelika.Calculators.Core.Units.MeasurementUnit;

namespace Prahelika.Calculators.Core.Tests.Units;

public class UnitConverterTests
{
    private readonly UnitConverter _converter = new();

    private static decimal Dec(string text) => decimal.Parse(text, CultureInfo.InvariantCulture);

    [Theory]
    // Length
    [InlineData("1", Mile, Metre, "1609.344")]
    [InlineData("1", Mile, Kilometre, "1.609344")]
    [InlineData("100000", Metre, Mile, "62.137119")]
    [InlineData("1", Inch, Centimetre, "2.54")]
    [InlineData("5", Foot, Metre, "1.524")]
    [InlineData("1", Yard, Foot, "3")]
    [InlineData("250", Millimetre, Centimetre, "25")]
    // Weight
    [InlineData("1", Pound, Kilogram, "0.453592")]
    [InlineData("1", Ounce, Gram, "28.349523")]
    [InlineData("1", Tonne, Kilogram, "1000")]
    [InlineData("500", Gram, Kilogram, "0.5")]
    [InlineData("1500", Milligram, Gram, "1.5")]
    // Area
    [InlineData("1", Acre, Hectare, "0.404686")]
    [InlineData("1", Hectare, SquareMetre, "10000")]
    [InlineData("1", SquareFoot, SquareInch, "144")]
    [InlineData("1", Acre, SquareFoot, "43560")]
    // Volume
    [InlineData("1", UsGallon, Litre, "3.785412")]
    [InlineData("1", CubicMetre, Litre, "1000")]
    [InlineData("1", UsGallon, UsFluidOunce, "128")]
    [InlineData("250", Millilitre, Litre, "0.25")]
    [InlineData("1", UsFluidOunce, Millilitre, "29.57353")]
    public void Converts_Known_Values(string value, MeasurementUnit from, MeasurementUnit to, string expected)
    {
        var result = _converter.Convert(Dec(value), from, to);

        Assert.True(result.IsSuccess);
        Assert.Equal(Dec(expected), result.Value);
    }

    [Theory]
    [InlineData("0", Celsius, Fahrenheit, "32")]
    [InlineData("100", Celsius, Fahrenheit, "212")]
    [InlineData("37", Celsius, Fahrenheit, "98.6")]
    [InlineData("36.6", Celsius, Fahrenheit, "97.88")]
    [InlineData("-40", Celsius, Fahrenheit, "-40")]     // the one temperature where both scales agree
    [InlineData("-40", Fahrenheit, Celsius, "-40")]
    [InlineData("212", Fahrenheit, Celsius, "100")]
    [InlineData("98.6", Fahrenheit, Celsius, "37")]
    [InlineData("72", Fahrenheit, Celsius, "22.222222")]
    [InlineData("0", Celsius, Kelvin, "273.15")]
    [InlineData("300", Kelvin, Celsius, "26.85")]
    [InlineData("100", Fahrenheit, Kelvin, "310.927778")]
    [InlineData("0", Kelvin, Celsius, "-273.15")]        // absolute zero is allowed
    [InlineData("-273.15", Celsius, Fahrenheit, "-459.67")]
    public void Converts_Temperatures(string value, MeasurementUnit from, MeasurementUnit to, string expected)
    {
        var result = _converter.Convert(Dec(value), from, to);

        Assert.True(result.IsSuccess);
        Assert.Equal(Dec(expected), result.Value);
    }

    [Theory]
    [InlineData("-300", Celsius, Fahrenheit)]
    [InlineData("-1", Kelvin, Celsius)]
    [InlineData("-460", Fahrenheit, Celsius)]
    public void Temperatures_Below_Absolute_Zero_Are_Rejected(string value, MeasurementUnit from, MeasurementUnit to)
    {
        var result = _converter.Convert(Dec(value), from, to);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
        Assert.Contains("absolute zero", result.Message);
    }

    [Fact]
    public void The_Same_Unit_Returns_The_Value_Rounded()
    {
        Assert.Equal(12.345679m, _converter.Convert(12.3456789m, Metre, Metre).Value);
        Assert.Equal(25m, _converter.Convert(25m, Celsius, Celsius).Value);
    }

    [Fact]
    public void Negative_Lengths_Are_Allowed()
    {
        Assert.Equal(-1.609344m, _converter.Convert(-1m, Mile, Kilometre).Value);
    }

    [Theory]
    [InlineData(Metre, Kilogram)]
    [InlineData(Celsius, Metre)]
    [InlineData(Litre, SquareMetre)]
    [InlineData(Acre, Mile)]
    public void Units_From_Different_Categories_Cannot_Be_Converted(MeasurementUnit from, MeasurementUnit to)
    {
        var result = _converter.Convert(1m, from, to);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Fact]
    public void An_Unknown_Unit_Returns_An_Error()
    {
        var result = _converter.Convert(1m, (MeasurementUnit)999, Metre);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Fact]
    public void A_Result_Too_Large_To_Represent_Returns_An_Overflow_Error()
    {
        var result = _converter.Convert(decimal.MaxValue, Kilometre, Millimetre);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.Overflow, result.Error);
    }

    [Fact]
    public void Decimal_Places_Control_The_Rounding()
    {
        Assert.Equal(1.61m, new UnitConverter(2).Convert(1m, Mile, Kilometre).Value);
        Assert.Equal(2m, new UnitConverter(0).Convert(1m, Mile, Kilometre).Value);
        Assert.Equal(1.609344m, new UnitConverter(10).Convert(1m, Mile, Kilometre).Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Invalid_Decimal_Places_Throw(int places)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnitConverter(places));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void Decimal_Places_From_0_To_10_Are_Accepted(int places)
    {
        Assert.Equal(places, new UnitConverter(places).DecimalPlaces);
    }

    // Converting there and back should land very close to where it started. It cannot be exactly equal,
    // because the answer is rounded on the way, so we allow a tiny difference.
    [Theory]
    [InlineData(Kilometre, Mile)]
    [InlineData(Foot, Inch)]
    [InlineData(Metre, Yard)]
    [InlineData(Centimetre, Inch)]
    [InlineData(Mile, Foot)]
    [InlineData(Kilogram, Pound)]
    [InlineData(Gram, Ounce)]
    [InlineData(Hectare, Acre)]
    [InlineData(SquareMetre, SquareFoot)]
    [InlineData(Acre, SquareMetre)]
    [InlineData(Litre, UsGallon)]
    [InlineData(Millilitre, UsFluidOunce)]
    public void Converting_There_And_Back_Gives_The_Starting_Value(MeasurementUnit first, MeasurementUnit second)
    {
        var precise = new UnitConverter(10);

        foreach (decimal start in new[] { 1000m, 12.5m })
        {
            decimal there = precise.Convert(start, first, second).Value;
            decimal back = precise.Convert(there, second, first).Value;

            Assert.InRange(back, start - 0.0001m, start + 0.0001m);
        }
    }

    [Theory]
    [InlineData(Celsius, Fahrenheit)]
    [InlineData(Celsius, Kelvin)]
    [InlineData(Fahrenheit, Kelvin)]
    public void Temperatures_Round_Trip_Too(MeasurementUnit first, MeasurementUnit second)
    {
        var precise = new UnitConverter(10);

        foreach (decimal start in new[] { 25m, 98.6m, 300m })
        {
            decimal there = precise.Convert(start, first, second).Value;
            decimal back = precise.Convert(there, second, first).Value;

            Assert.InRange(back, start - 0.0001m, start + 0.0001m);
        }
    }

    [Fact]
    public void Every_Unit_Has_A_Category_And_Is_Listed_In_It()
    {
        foreach (MeasurementUnit unit in Enum.GetValues<MeasurementUnit>())
        {
            UnitCategory category = _converter.CategoryOf(unit);

            Assert.Contains(unit, _converter.UnitsIn(category));
        }
    }

    [Theory]
    [InlineData(UnitCategory.Length, 8)]
    [InlineData(UnitCategory.Weight, 6)]
    [InlineData(UnitCategory.Temperature, 3)]
    [InlineData(UnitCategory.Area, 7)]
    [InlineData(UnitCategory.Volume, 5)]
    public void Each_Category_Lists_Its_Units(UnitCategory category, int expectedCount)
    {
        Assert.Equal(expectedCount, _converter.UnitsIn(category).Count);
    }

    [Fact]
    public void Any_Two_Units_Of_The_Same_Category_Can_Be_Converted()
    {
        foreach (UnitCategory category in Enum.GetValues<UnitCategory>())
        {
            foreach (var from in _converter.UnitsIn(category))
            {
                foreach (var to in _converter.UnitsIn(category))
                {
                    Assert.True(_converter.Convert(1m, from, to).IsSuccess, $"{from} to {to} failed.");
                }
            }
        }
    }

    [Fact]
    public void An_Undefined_Unit_Has_No_Category()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _converter.CategoryOf((MeasurementUnit)999));
    }
}