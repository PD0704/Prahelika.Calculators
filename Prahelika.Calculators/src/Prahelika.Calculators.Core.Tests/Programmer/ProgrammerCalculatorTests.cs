using System.Globalization;
using Prahelika.Calculators.Core.Programmer;

namespace Prahelika.Calculators.Core.Tests.Programmer;

public class ProgrammerCalculatorTests
{
    private readonly ProgrammerCalculator _calculator = new();

    [Theory]
    [InlineData("1010", NumberBase.Binary, 10L)]
    [InlineData("0b1010", NumberBase.Binary, 10L)]
    [InlineData("0B1010", NumberBase.Binary, 10L)]
    [InlineData("  1010  ", NumberBase.Binary, 10L)]          // spaces around are ignored
    [InlineData("17", NumberBase.Octal, 15L)]
    [InlineData("0o17", NumberBase.Octal, 15L)]
    [InlineData("255", NumberBase.Decimal, 255L)]
    [InlineData("-255", NumberBase.Decimal, -255L)]
    [InlineData("+7", NumberBase.Decimal, 7L)]
    [InlineData("FF", NumberBase.Hexadecimal, 255L)]
    [InlineData("ff", NumberBase.Hexadecimal, 255L)]
    [InlineData("0xFF", NumberBase.Hexadecimal, 255L)]
    [InlineData("0XfF", NumberBase.Hexadecimal, 255L)]
    [InlineData("0b1", NumberBase.Hexadecimal, 177L)]         // in hex, "0b1" is just the digits B1
    [InlineData("000000000000000000FF", NumberBase.Hexadecimal, 255L)]   // leading zeros never overflow
    [InlineData("0", NumberBase.Binary, 0L)]
    [InlineData("0", NumberBase.Octal, 0L)]
    [InlineData("0", NumberBase.Decimal, 0L)]
    [InlineData("0", NumberBase.Hexadecimal, 0L)]
    public void Parse_Reads_Numbers_In_Each_Base(string text, NumberBase numberBase, long expected)
    {
        var result = _calculator.Parse(text, numberBase);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Parse_Reads_The_Largest_And_Smallest_Numbers()
    {
        Assert.Equal(long.MaxValue, _calculator.Parse("9223372036854775807", NumberBase.Decimal).Value);
        Assert.Equal(long.MinValue, _calculator.Parse("-9223372036854775808", NumberBase.Decimal).Value);
        Assert.Equal(long.MaxValue, _calculator.Parse("7FFFFFFFFFFFFFFF", NumberBase.Hexadecimal).Value);
        Assert.Equal(long.MinValue, _calculator.Parse("8000000000000000", NumberBase.Hexadecimal).Value);
        Assert.Equal(-1L, _calculator.Parse("FFFFFFFFFFFFFFFF", NumberBase.Hexadecimal).Value);
        Assert.Equal(-1L, _calculator.Parse(new string('1', 64), NumberBase.Binary).Value);
        Assert.Equal(-1L, _calculator.Parse("1777777777777777777777", NumberBase.Octal).Value);
    }

    [Theory]
    [InlineData("", NumberBase.Decimal)]
    [InlineData("   ", NumberBase.Binary)]
    [InlineData("102", NumberBase.Binary)]
    [InlineData("8", NumberBase.Octal)]
    [InlineData("12A", NumberBase.Decimal)]
    [InlineData("GG", NumberBase.Hexadecimal)]
    [InlineData("0x", NumberBase.Hexadecimal)]               // a prefix with no digits
    [InlineData("0b", NumberBase.Binary)]
    [InlineData("0x1F", NumberBase.Binary)]                  // the prefix of another base is not accepted
    [InlineData("-101", NumberBase.Binary)]                  // negative numbers are written as 64 bits instead
    [InlineData("-FF", NumberBase.Hexadecimal)]
    [InlineData("-", NumberBase.Decimal)]
    [InlineData("--5", NumberBase.Decimal)]
    [InlineData("1 0", NumberBase.Decimal)]
    [InlineData("1.5", NumberBase.Decimal)]
    [InlineData("1,000", NumberBase.Decimal)]
    [InlineData("٣", NumberBase.Decimal)]                    // an Arabic-Indic digit three is not an ASCII digit
    public void Parse_Rejects_Text_That_Is_Not_A_Number_In_That_Base(string text, NumberBase numberBase)
    {
        var result = _calculator.Parse(text, numberBase);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Theory]
    [InlineData("9223372036854775808", NumberBase.Decimal)]    // one more than the largest
    [InlineData("-9223372036854775809", NumberBase.Decimal)]   // one less than the smallest
    [InlineData("10000000000000000", NumberBase.Hexadecimal)]  // 17 hex digits: 2 to the power 64
    [InlineData("2000000000000000000000", NumberBase.Octal)]   // 2 to the power 64
    public void Parse_Reports_Numbers_That_Do_Not_Fit_In_64_Bits(string text, NumberBase numberBase)
    {
        var result = _calculator.Parse(text, numberBase);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.Overflow, result.Error);
    }

    [Fact]
    public void Parse_Reports_A_Binary_Number_Of_65_Digits_As_Overflow()
    {
        var result = _calculator.Parse("1" + new string('0', 64), NumberBase.Binary);

        Assert.Equal(CalcError.Overflow, result.Error);
    }

    [Fact]
    public void Parse_With_No_Text_Is_A_Programmer_Error()
    {
        Assert.Throws<ArgumentNullException>(() => _calculator.Parse(null!, NumberBase.Decimal));
    }

    [Fact]
    public void Parse_With_An_Unknown_Base_Returns_An_Error()
    {
        var result = _calculator.Parse("1", (NumberBase)7);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }

    [Theory]
    [InlineData("255", NumberBase.Decimal, "255", "11111111", "377", "FF")]
    [InlineData("FF", NumberBase.Hexadecimal, "255", "11111111", "377", "FF")]
    [InlineData("0b1100", NumberBase.Binary, "12", "1100", "14", "C")]
    [InlineData("0", NumberBase.Decimal, "0", "0", "0", "0")]
    public void ConvertBase_Writes_The_Number_In_Every_Base(
        string text, NumberBase from, string decimalText, string binary, string octal, string hex)
    {
        var result = _calculator.ConvertBase(text, from);

        Assert.True(result.IsSuccess);
        Assert.Equal(decimalText, result.Value!.Decimal);
        Assert.Equal(binary, result.Value.Binary);
        Assert.Equal(octal, result.Value.Octal);
        Assert.Equal(hex, result.Value.Hexadecimal);
    }

    [Fact]
    public void ConvertBase_Shows_Negative_Numbers_As_64_Bits()
    {
        var minusOne = _calculator.ConvertBase("-1", NumberBase.Decimal).Value!;

        Assert.Equal("-1", minusOne.Decimal);
        Assert.Equal(new string('1', 64), minusOne.Binary);
        Assert.Equal("1777777777777777777777", minusOne.Octal);
        Assert.Equal("FFFFFFFFFFFFFFFF", minusOne.Hexadecimal);

        var minusSix = _calculator.ConvertBase("-6", NumberBase.Decimal).Value!;

        Assert.Equal(new string('1', 61) + "010", minusSix.Binary);
        Assert.Equal("FFFFFFFFFFFFFFFA", minusSix.Hexadecimal);
    }

    [Fact]
    public void ConvertBase_Handles_The_Largest_And_Smallest_Numbers()
    {
        var largest = _calculator.ConvertBase(long.MaxValue.ToString(CultureInfo.InvariantCulture), NumberBase.Decimal).Value!;
        var smallest = _calculator.ConvertBase(long.MinValue.ToString(CultureInfo.InvariantCulture), NumberBase.Decimal).Value!;

        Assert.Equal("7FFFFFFFFFFFFFFF", largest.Hexadecimal);
        Assert.Equal("777777777777777777777", largest.Octal);
        Assert.Equal("8000000000000000", smallest.Hexadecimal);
        Assert.Equal("1000000000000000000000", smallest.Octal);
    }

    [Fact]
    public void ConvertBase_Passes_On_Errors_From_Parse()
    {
        Assert.Equal(CalcError.InvalidInput, _calculator.ConvertBase("2", NumberBase.Binary).Error);
        Assert.Equal(CalcError.Overflow, _calculator.ConvertBase("9223372036854775808", NumberBase.Decimal).Error);
    }

    // Writing a number in a base and reading it back must give the same number, for every base.
    [Fact]
    public void Every_Base_Reads_Back_What_It_Wrote()
    {
        long[] numbers = { 0, 1, -1, 7, -6, 255, 123456789, -987654321, long.MaxValue, long.MinValue };

        foreach (long number in numbers)
        {
            var written = _calculator.ConvertBase(number.ToString(CultureInfo.InvariantCulture), NumberBase.Decimal).Value!;

            Assert.Equal(number, _calculator.Parse(written.Decimal, NumberBase.Decimal).Value);
            Assert.Equal(number, _calculator.Parse(written.Binary, NumberBase.Binary).Value);
            Assert.Equal(number, _calculator.Parse(written.Octal, NumberBase.Octal).Value);
            Assert.Equal(number, _calculator.Parse(written.Hexadecimal, NumberBase.Hexadecimal).Value);
        }
    }

    [Theory]
    [InlineData(12L, BitwiseOperation.And, 10L, 8L)]
    [InlineData(12L, BitwiseOperation.Or, 10L, 14L)]
    [InlineData(12L, BitwiseOperation.Xor, 10L, 6L)]
    [InlineData(0xF0F0L, BitwiseOperation.And, 0xFF00L, 0xF000L)]
    [InlineData(0xF0F0L, BitwiseOperation.Or, 0xFF00L, 0xFFF0L)]
    [InlineData(0xF0F0L, BitwiseOperation.Xor, 0xFF00L, 0x0FF0L)]
    [InlineData(-1L, BitwiseOperation.And, 255L, 255L)]
    [InlineData(-8L, BitwiseOperation.Or, 1L, -7L)]
    [InlineData(-1L, BitwiseOperation.Xor, -1L, 0L)]
    [InlineData(5L, BitwiseOperation.Not, 0L, -6L)]
    [InlineData(0L, BitwiseOperation.Not, 0L, -1L)]
    [InlineData(5L, BitwiseOperation.Not, 999L, -6L)]            // the second number is ignored
    [InlineData(1L, BitwiseOperation.ShiftLeft, 4L, 16L)]
    [InlineData(1L, BitwiseOperation.ShiftLeft, 62L, 4611686018427387904L)]
    [InlineData(1L, BitwiseOperation.ShiftLeft, 63L, long.MinValue)]   // the bit lands on the sign
    [InlineData(3L, BitwiseOperation.ShiftLeft, 63L, long.MinValue)]   // the other bit falls off the end
    [InlineData(5L, BitwiseOperation.ShiftLeft, 0L, 5L)]
    [InlineData(256L, BitwiseOperation.ShiftRight, 4L, 16L)]
    [InlineData(-16L, BitwiseOperation.ShiftRight, 2L, -4L)]     // the sign is kept
    [InlineData(-1L, BitwiseOperation.ShiftRight, 63L, -1L)]
    [InlineData(1L, BitwiseOperation.ShiftRight, 1L, 0L)]
    public void Bitwise_Applies_The_Operation(long left, BitwiseOperation operation, long right, long expected)
    {
        var result = _calculator.Bitwise(left, operation, right);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value!.Value);
    }

    [Fact]
    public void Bitwise_Writes_The_Result_In_Every_Base()
    {
        var result = _calculator.Bitwise(12, BitwiseOperation.And, 10).Value!;

        Assert.Equal(8, result.Value);
        Assert.Equal("8", result.Decimal);
        Assert.Equal("1000", result.Binary);
        Assert.Equal("10", result.Octal);
        Assert.Equal("8", result.Hexadecimal);
    }

    [Fact]
    public void Not_Needs_Only_One_Number()
    {
        var result = _calculator.Bitwise(5, BitwiseOperation.Not);

        Assert.Equal(-6, result.Value!.Value);
    }

    [Theory]
    [InlineData(BitwiseOperation.ShiftLeft, -1L)]
    [InlineData(BitwiseOperation.ShiftLeft, 64L)]
    [InlineData(BitwiseOperation.ShiftRight, -1L)]
    [InlineData(BitwiseOperation.ShiftRight, 64L)]
    [InlineData(BitwiseOperation.ShiftLeft, long.MaxValue)]
    public void Shifting_By_An_Amount_Outside_0_To_63_Is_Invalid(BitwiseOperation operation, long amount)
    {
        var result = _calculator.Bitwise(1, operation, amount);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Fact]
    public void An_Unknown_Operation_Returns_An_Error()
    {
        var result = _calculator.Bitwise(1, (BitwiseOperation)99, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(CalcError.InvalidInput, result.Error);
    }
}