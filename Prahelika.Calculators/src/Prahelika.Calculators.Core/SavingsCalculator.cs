namespace Prahelika.Calculators.Core.Finance;

/// <summary>
/// Fixed deposit (FD), recurring deposit (RD) and PPF calculations, using <see cref="decimal"/> arithmetic.
/// Amounts are rounded to the chosen number of decimal places, half away from zero.
/// </summary>
public sealed class SavingsCalculator : ISavingsCalculator
{
    private const int MaxMonths = 1200;
    private const decimal PpfMinDeposit = 500m;
    private const decimal PpfMaxDeposit = 150000m;
    private const int PpfMinYears = 15;
    private const int PpfMaxYears = 50;
    private const int PpfYearStep = 5;

    /// <param name="decimalPlaces">How many decimal places to round amounts to (0 to 10). The default is 2.</param>
    public SavingsCalculator(int decimalPlaces = 2)
    {
        // Keep this range the same as FinanceCalculator's constructor.
        if (decimalPlaces < 0 || decimalPlaces > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "Decimal places must be between 0 and 10.");
        }

        DecimalPlaces = decimalPlaces;
    }

    /// <inheritdoc />
    public int DecimalPlaces { get; }

    /// <inheritdoc />
    public CalcResult<MaturityResult> FixedDeposit(
        decimal principal,
        decimal annualRatePercent,
        int months,
        CompoundingFrequency frequency = CompoundingFrequency.Quarterly)
    {
        if (principal <= 0) return Invalid("The deposit amount must be above zero.");
        if (annualRatePercent < 0) return Invalid("The interest rate cannot be negative.");
        if (months < 1 || months > MaxMonths) return Invalid($"The number of months must be between 1 and {MaxMonths}.");
        if (!Enum.IsDefined(frequency)) return Invalid("The compounding frequency is not valid.");

        return Compute(() =>
        {
            decimal timesPerYear = (int)frequency;
            decimal ratePerPeriod = annualRatePercent / 100m / timesPerYear;
            decimal periods = timesPerYear * months / 12m;

            return Build(principal, principal * Power(1m + ratePerPeriod, periods));
        });
    }

    /// <inheritdoc />
    public CalcResult<MaturityResult> RecurringDeposit(decimal monthlyDeposit, decimal annualRatePercent, int months)
    {
        if (monthlyDeposit <= 0) return Invalid("The monthly deposit must be above zero.");
        if (annualRatePercent < 0) return Invalid("The interest rate cannot be negative.");
        if (months < 1 || months > MaxMonths) return Invalid($"The number of months must be between 1 and {MaxMonths}.");

        return Compute(() =>
        {
            // Interest is added every quarter. A deposit that stays for k months grows by (1 + rate/4) ^ (k / 3).
            double growthPerQuarter = 1.0 + (double)(annualRatePercent / 400m);

            decimal maturity = 0m;
            for (int monthsInvested = 1; monthsInvested <= months; monthsInvested++)
            {
                maturity += monthlyDeposit * (decimal)Math.Pow(growthPerQuarter, monthsInvested / 3.0);
            }

            return Build(monthlyDeposit * months, maturity);
        });
    }

    /// <inheritdoc />
    public CalcResult<MaturityResult> Ppf(decimal yearlyDeposit, decimal annualRatePercent, int years)
    {
        if (yearlyDeposit < PpfMinDeposit || yearlyDeposit > PpfMaxDeposit)
        {
            return Invalid($"The yearly deposit must be between {PpfMinDeposit:0} and {PpfMaxDeposit:0}.");
        }

        if (annualRatePercent < 0) return Invalid("The interest rate cannot be negative.");

        if (years < PpfMinYears || years > PpfMaxYears || (years - PpfMinYears) % PpfYearStep != 0)
        {
            return Invalid("The term must be 15 years, or longer in blocks of 5 years (20, 25 and so on, up to 50).");
        }

        return Compute(() =>
        {
            decimal yearlyGrowth = 1m + annualRatePercent / 100m;

            decimal balance = 0m;
            for (int year = 1; year <= years; year++)
            {
                // The deposit goes in at the start of the year, and interest is added at its end.
                balance = (balance + yearlyDeposit) * yearlyGrowth;
            }

            return Build(yearlyDeposit * years, balance);
        });
    }

    private MaturityResult Build(decimal invested, decimal maturity)
    {
        decimal roundedInvested = Round(invested);
        decimal roundedMaturity = Round(maturity);

        return new MaturityResult(roundedInvested, roundedMaturity - roundedInvested, roundedMaturity);
    }

    private decimal Round(decimal value) => decimal.Round(value, DecimalPlaces, MidpointRounding.AwayFromZero);

    private static CalcResult<MaturityResult> Invalid(string message) =>
        CalcResult<MaturityResult>.Failure(CalcError.InvalidInput, message);

    private static CalcResult<MaturityResult> Compute(Func<MaturityResult> calculation)
    {
        try
        {
            return CalcResult<MaturityResult>.Success(calculation());
        }
        catch (OverflowException)
        {
            return CalcResult<MaturityResult>.Failure(CalcError.Overflow, "The numbers are too large to calculate.");
        }
    }

    // baseValue ^ exponent: exact decimal arithmetic for the whole part, and a double for any fraction.
    private static decimal Power(decimal baseValue, decimal exponent)
    {
        decimal whole = decimal.Truncate(exponent);
        decimal fraction = exponent - whole;

        decimal result = 1m;
        decimal factor = baseValue;
        for (long remaining = (long)whole; remaining > 0; remaining >>= 1)
        {
            if ((remaining & 1) == 1)
            {
                result *= factor;
            }

            if (remaining > 1)
            {
                factor *= factor;
            }
        }

        if (fraction > 0)
        {
            result *= (decimal)Math.Pow((double)baseValue, (double)fraction);
        }

        return result;
    }
}