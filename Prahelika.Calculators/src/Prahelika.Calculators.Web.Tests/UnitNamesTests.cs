using Prahelika.Calculators.Core.Units;
using Prahelika.Calculators.Web.Models;

namespace Prahelika.Calculators.Web.Tests;

public class UnitNamesTests
{
    [Theory]
    [InlineData(MeasurementUnit.Metre, "Metre")]
    [InlineData(MeasurementUnit.SquareMetre, "Square metre")]
    [InlineData(MeasurementUnit.CubicMetre, "Cubic metre")]
    [InlineData(MeasurementUnit.UsGallon, "US gallon")]
    [InlineData(MeasurementUnit.UsFluidOunce, "US fluid ounce")]
    [InlineData(MeasurementUnit.Celsius, "Celsius")]
    public void Units_Have_Friendly_Names(MeasurementUnit unit, string expected)
    {
        Assert.Equal(expected, UnitNames.For(unit));
    }

    [Fact]
    public void Every_Unit_Has_A_Different_Name()
    {
        var names = Enum.GetValues<MeasurementUnit>().Select(UnitNames.For).ToList();

        Assert.All(names, name => Assert.False(string.IsNullOrWhiteSpace(name)));
        Assert.Equal(names.Count, names.Distinct().Count());
    }
}