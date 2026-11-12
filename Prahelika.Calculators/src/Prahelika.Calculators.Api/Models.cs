namespace Prahelika.Calculators.Api;

/// <summary>A successful calculation.</summary>
public sealed record CalcResponse<T>(T Result);

/// <summary>A successful trigonometric calculation, with the angle unit that was used.</summary>
public sealed record AngleCalcResponse(double Result, string AngleUnit);

/// <summary>The mathematical constants offered by the API.</summary>
public sealed record ConstantsResponse(double Pi, double E);