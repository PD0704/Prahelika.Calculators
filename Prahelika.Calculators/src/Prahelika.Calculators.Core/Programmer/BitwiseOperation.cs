namespace Prahelika.Calculators.Core.Programmer;

/// <summary>An operation on the bits of 64-bit numbers.</summary>
public enum BitwiseOperation
{
    /// <summary>A bit is 1 only if it is 1 in both numbers.</summary>
    And,

    /// <summary>A bit is 1 if it is 1 in either number.</summary>
    Or,

    /// <summary>A bit is 1 if it is 1 in exactly one of the numbers.</summary>
    Xor,

    /// <summary>Flips every bit of the first number. The second number is ignored.</summary>
    Not,

    /// <summary>Moves the bits of the first number left by the second number. Bits moved past the left end are lost.</summary>
    ShiftLeft,

    /// <summary>Moves the bits of the first number right by the second number. The sign is kept.</summary>
    ShiftRight
}