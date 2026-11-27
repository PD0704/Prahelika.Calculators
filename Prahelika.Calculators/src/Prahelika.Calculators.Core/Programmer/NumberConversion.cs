namespace Prahelika.Calculators.Core.Programmer;

/// <summary>A 64-bit number written in all four bases.</summary>
/// <param name="Value">The number.</param>
/// <param name="Decimal">In decimal, with a minus sign if negative.</param>
/// <param name="Binary">In binary. A negative number shows its 64 bits (two's complement).</param>
/// <param name="Octal">In octal. A negative number shows its 64 bits (two's complement).</param>
/// <param name="Hexadecimal">In hexadecimal, in capitals. A negative number shows its 64 bits (two's complement).</param>
public sealed record NumberConversion(long Value, string Decimal, string Binary, string Octal, string Hexadecimal);