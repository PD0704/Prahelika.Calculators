namespace Prahelika.Calculators.Core.Finance;

/// <summary>How often interest is added to the balance. The number is how many times per year.</summary>
public enum CompoundingFrequency
{
    /// <summary>Once a year.</summary>
    Annually = 1,

    /// <summary>Twice a year.</summary>
    SemiAnnually = 2,

    /// <summary>Four times a year.</summary>
    Quarterly = 4,

    /// <summary>Twelve times a year.</summary>
    Monthly = 12,

    /// <summary>Fifty-two times a year.</summary>
    Weekly = 52,

    /// <summary>Every day of a 365-day year.</summary>
    Daily = 365
}