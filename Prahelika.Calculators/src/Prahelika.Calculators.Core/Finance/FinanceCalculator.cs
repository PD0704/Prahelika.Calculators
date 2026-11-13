namespace Prahelika.Calculators.Core.Finance;

/// <summary>Default implementation of <see cref="IFinanceCalculator"/>.</summary>
public sealed class FinanceCalculator : IFinanceCalculator
{
    private const int MaxDecimalPlaces = 10;

    /// <summary>Creates a calculator that rounds money amounts to the given number of decimal places (2 by default).</summary>
    /// <exception cref="ArgumentOutOfRangeException">decimalPlaces is below 0 or above 10.</exception>
    public FinanceCalculator(int decimalPlaces = 2)
    {
        if (decimalPlaces < 0 || decimalPlaces > MaxDecimalPlaces)
        {
            throw new ArgumentOutOfRangeException(
                nameof(decimalPlaces), decimalPlaces, $"Decimal places must be between 0 and {MaxDecimalPlaces}.");
        }

        DecimalPlaces = decimalPlaces;
    }

    /// <inheritdoc />
    public int DecimalPlaces { get; }

    /// <inheritdoc />
    public CalcResult<InterestResult> SimpleInterest(decimal principal, decimal annualRatePercent, decimal years)
    {
        var invalid = Validate(principal, annualRatePercent, years);
        if (invalid is not null)
        {
            return invalid;
        }

        try
        {
            decimal interest = Round(principal * annualRatePercent * years / 100m);
            return Succeeded(principal, interest);
        }
        catch (OverflowException)
        {
            return OverflowFailure();
        }
    }

    /// <inheritdoc />
    public CalcResult<InterestResult> CompoundInterest(
        decimal principal,
        decimal annualRatePercent,
        decimal years,
        CompoundingFrequency frequency = CompoundingFrequency.Annually)
    {
        var invalid = Validate(principal, annualRatePercent, years);
        if (invalid is not null)
        {
            return invalid;
        }

        if (!Enum.IsDefined(frequency))
        {
            return CalcResult<InterestResult>.Failure(CalcError.InvalidInput, "The compounding frequency is not valid.");
        }

        // Nothing can grow in these cases, so skip the maths.
        if (principal == 0m || annualRatePercent == 0m || years == 0m)
        {
            return Succeeded(principal, 0m);
        }

        try
        {
            int periodsPerYear = (int)frequency;
            decimal ratePerPeriod = annualRatePercent / 100m / periodsPerYear;
            decimal totalPeriods = years * periodsPerYear;
            decimal wholePeriods = decimal.Truncate(totalPeriods);
            decimal partialPeriod = totalPeriods - wholePeriods;

            decimal growth = 1m + ratePerPeriod;

            // Whole periods are calculated exactly in decimal.
            decimal factor = Power(growth, (long)wholePeriods);

            // A part of a period (for example 2.5 years of annual compounding) needs a fractional power.
            // decimal has no such function, so this one step uses double (about 15 digits of precision).
            if (partialPeriod > 0m)
            {
                factor *= (decimal)Math.Pow((double)growth, (double)partialPeriod);
            }

            decimal interest = Round(principal * (factor - 1m));
            return Succeeded(principal, interest);
        }
        catch (OverflowException)
        {
            return OverflowFailure();
        }
    }

    // Exponentiation by squaring: only about 2 x log2(exponent) multiplications, and exact in decimal.
    private static decimal Power(decimal baseValue, long exponent)
    {
        decimal result = 1m;

        while (exponent > 0)
        {
            if ((exponent & 1) == 1)
            {
                result *= baseValue;
            }

            exponent >>= 1;

            if (exponent > 0)
            {
                baseValue *= baseValue;
            }
        }

        return result;
    }

    private decimal Round(decimal value) =>
        Math.Round(value, DecimalPlaces, MidpointRounding.AwayFromZero);

    private static CalcResult<InterestResult>? Validate(decimal principal, decimal annualRatePercent, decimal years)
    {
        if (principal < 0m)
        {
            return CalcResult<InterestResult>.Failure(CalcError.InvalidInput, "The principal cannot be negative.");
        }

        if (annualRatePercent < 0m)
        {
            return CalcResult<InterestResult>.Failure(CalcError.InvalidInput, "The interest rate cannot be negative.");
        }

        if (years < 0m)
        {
            return CalcResult<InterestResult>.Failure(CalcError.InvalidInput, "The time period cannot be negative.");
        }

        return null;
    }

    private static CalcResult<InterestResult> Succeeded(decimal principal, decimal interest) =>
        CalcResult<InterestResult>.Success(new InterestResult(principal, interest, principal + interest));

    private static CalcResult<InterestResult> OverflowFailure() =>
        CalcResult<InterestResult>.Failure(CalcError.Overflow, "The result is too large to represent.");
}