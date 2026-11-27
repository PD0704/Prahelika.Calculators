using System.Globalization;

namespace Prahelika.Calculators.Core.Programmer;

/// <summary>Default implementation of <see cref="IProgrammerCalculator"/>, using 64-bit signed numbers.</summary>
public sealed class ProgrammerCalculator : IProgrammerCalculator
{
    private const int MaxShift = 63;

    /// <inheritdoc />
    public CalcResult<long> Parse(string text, NumberBase numberBase)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (!Enum.IsDefined(numberBase))
        {
            return CalcResult<long>.Failure(CalcError.InvalidInput, "That number base is not supported.");
        }

        string trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return CalcResult<long>.Failure(CalcError.InvalidInput, "Enter a number.");
        }

        return numberBase == NumberBase.Decimal ? ParseDecimal(trimmed) : ParseBits(trimmed, numberBase);
    }

    /// <inheritdoc />
    public CalcResult<NumberConversion> ConvertBase(string text, NumberBase from)
    {
        CalcResult<long> parsed = Parse(text, from);

        return parsed.IsSuccess
            ? CalcResult<NumberConversion>.Success(Describe(parsed.Value))
            : CalcResult<NumberConversion>.Failure(parsed.Error, parsed.Message ?? string.Empty);
    }

    /// <inheritdoc />
    public CalcResult<NumberConversion> Bitwise(long left, BitwiseOperation operation, long right = 0)
    {
        long result;

        switch (operation)
        {
            case BitwiseOperation.And:
                result = left & right;
                break;
            case BitwiseOperation.Or:
                result = left | right;
                break;
            case BitwiseOperation.Xor:
                result = left ^ right;
                break;
            case BitwiseOperation.Not:
                result = ~left;
                break;
            case BitwiseOperation.ShiftLeft:
            case BitwiseOperation.ShiftRight:
                // C# quietly uses only the last 6 bits of a shift amount, so "shift by 64" would do nothing. Reject it instead.
                if (right is < 0 or > MaxShift)
                {
                    return CalcResult<NumberConversion>.Failure(CalcError.InvalidInput, "Shift by a number from 0 to 63.");
                }

                result = operation == BitwiseOperation.ShiftLeft ? left << (int)right : left >> (int)right;
                break;
            default:
                return CalcResult<NumberConversion>.Failure(CalcError.InvalidInput, "That operation is not supported.");
        }

        return CalcResult<NumberConversion>.Success(Describe(result));
    }

    private static CalcResult<long> ParseDecimal(string text)
    {
        // Check the characters ourselves: only an optional sign and the digits 0-9 are allowed.
        int firstDigit = text[0] is '-' or '+' ? 1 : 0;
        bool onlyDigits = firstDigit < text.Length && text.Skip(firstDigit).All(char.IsAsciiDigit);

        if (!onlyDigits)
        {
            return CalcResult<long>.Failure(CalcError.InvalidInput, "Enter a whole number using the digits 0 to 9.");
        }

        return long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long value)
            ? CalcResult<long>.Success(value)
            : CalcResult<long>.Failure(CalcError.Overflow, "That number does not fit in 64 bits.");
    }

    // Binary, octal and hex: the digits are the 64 bits of the number, so the value is built as an unsigned number
    // and then read as signed. Each step is value = value * base + digit, checked so it never passes 64 bits.
    private static CalcResult<long> ParseBits(string text, NumberBase numberBase)
    {
        string digits = RemovePrefix(text, numberBase);
        if (digits.Length == 0)
        {
            return CalcResult<long>.Failure(CalcError.InvalidInput, "Enter some digits after the prefix.");
        }

        ulong radix = (ulong)numberBase;
        ulong value = 0;

        foreach (char c in digits)
        {
            int digit = DigitValue(c);
            if (digit < 0 || (ulong)digit >= radix)
            {
                return CalcResult<long>.Failure(
                    CalcError.InvalidInput,
                    $"'{c}' is not a valid {numberBase.ToString().ToLowerInvariant()} digit.");
            }

            if (value > (ulong.MaxValue - (ulong)digit) / radix)
            {
                return CalcResult<long>.Failure(CalcError.Overflow, "That number does not fit in 64 bits.");
            }

            value = value * radix + (ulong)digit;
        }

        return CalcResult<long>.Success(unchecked((long)value));
    }

    // Removes the prefix of this base only: "0b" is a prefix in binary but just two digits in hex.
    private static string RemovePrefix(string text, NumberBase numberBase)
    {
        string prefix = numberBase switch
        {
            NumberBase.Binary => "0b",
            NumberBase.Octal => "0o",
            NumberBase.Hexadecimal => "0x",
            _ => string.Empty
        };

        return prefix.Length > 0 && text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? text[prefix.Length..]
            : text;
    }

    private static int DigitValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1
    };

    // System.Convert.ToString(number, base) writes a negative number as its 64 bits (two's complement).
    private static NumberConversion Describe(long value) => new(
        value,
        value.ToString(CultureInfo.InvariantCulture),
        Convert.ToString(value, 2),
        Convert.ToString(value, 8),
        Convert.ToString(value, 16).ToUpperInvariant());
}