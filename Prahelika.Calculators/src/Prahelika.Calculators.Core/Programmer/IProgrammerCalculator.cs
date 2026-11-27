namespace Prahelika.Calculators.Core.Programmer;

/// <summary>Number base conversion and bitwise operations on 64-bit signed numbers.</summary>
public interface IProgrammerCalculator
{
    /// <summary>
    /// Reads a number written in the given base. Surrounding spaces are ignored, and the prefix for the base
    /// (<c>0b</c>, <c>0o</c> or <c>0x</c>, in any letter case) is optional.
    /// In decimal the number can start with + or -. In the other bases the digits are the 64 bits of the number
    /// (two's complement), so <c>FFFFFFFFFFFFFFFF</c> is -1 and a minus sign is not allowed.
    /// Fails with <see cref="CalcError.InvalidInput"/> for a wrong digit and <see cref="CalcError.Overflow"/> for a number that does not fit in 64 bits.
    /// </summary>
    CalcResult<long> Parse(string text, NumberBase numberBase);

    /// <summary>Reads a number in one base (see <see cref="Parse"/>) and writes it in all four bases.</summary>
    CalcResult<NumberConversion> ConvertBase(string text, NumberBase from);

    /// <summary>
    /// Applies a bitwise operation and writes the result in all four bases.
    /// <see cref="BitwiseOperation.Not"/> uses only <paramref name="left"/>. The shifts need <paramref name="right"/>
    /// to be between 0 and 63, otherwise the result is <see cref="CalcError.InvalidInput"/>.
    /// </summary>
    CalcResult<NumberConversion> Bitwise(long left, BitwiseOperation operation, long right = 0);
}