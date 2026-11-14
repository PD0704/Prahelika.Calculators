namespace Prahelika.Calculators.Api;

internal static class EnumParser
{
    /// <summary>Parses an enum from its name, ignoring case. Numbers such as "12" are rejected.</summary>
    public static bool TryParseName<TEnum>(string? text, out TEnum value) where TEnum : struct, Enum
    {
        value = default;

        return !string.IsNullOrWhiteSpace(text)
            && !int.TryParse(text, out _)
            && Enum.TryParse(text, ignoreCase: true, out value)
            && Enum.IsDefined(value);
    }
}