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



    // ---- Loans, SIP, ROI and GST ----

    /// <summary>The longest loan or investment accepted, in months (1200 months is 100 years).</summary>
    public const int MaxMonths = 1200;

    /// <inheritdoc />
    public CalcResult<LoanSummary> Emi(decimal loanAmount, decimal annualRatePercent, int months)
    {
        var invalid = ValidateRecurring<LoanSummary>(loanAmount, "loan amount", annualRatePercent, months);
        if (invalid is not null)
        {
            return invalid;
        }

        try
        {
            decimal monthlyPayment = Round(ExactEmi(loanAmount, annualRatePercent, months));
            decimal totalPayment = monthlyPayment * months;

            return CalcResult<LoanSummary>.Success(
                new LoanSummary(monthlyPayment, totalPayment, totalPayment - loanAmount));
        }
        catch (OverflowException)
        {
            return OverflowFailure<LoanSummary>();
        }
    }

    /// <inheritdoc />
    public CalcResult<IReadOnlyList<AmortizationRow>> AmortizationSchedule(
        decimal loanAmount, decimal annualRatePercent, int months)
    {
        var invalid = ValidateRecurring<IReadOnlyList<AmortizationRow>>(loanAmount, "loan amount", annualRatePercent, months);
        if (invalid is not null)
        {
            return invalid;
        }

        try
        {
            decimal monthlyRate = annualRatePercent / 12m / 100m;
            decimal payment = Round(ExactEmi(loanAmount, annualRatePercent, months));
            decimal balance = loanAmount;
            var rows = new List<AmortizationRow>(months);

            for (int month = 1; month <= months; month++)
            {
                decimal interest = Round(balance * monthlyRate);

                // The last month repays whatever is left, so rounding differences never leave a balance behind.
                // Clamping keeps the balance from going below zero or growing, even with unusual rounding settings.
                decimal principalPaid = month == months
                    ? balance
                    : Math.Clamp(payment - interest, 0m, balance);

                balance -= principalPaid;
                rows.Add(new AmortizationRow(month, principalPaid + interest, principalPaid, interest, balance));
            }

            return CalcResult<IReadOnlyList<AmortizationRow>>.Success(rows);
        }
        catch (OverflowException)
        {
            return OverflowFailure<IReadOnlyList<AmortizationRow>>();
        }
    }

    /// <inheritdoc />
    public CalcResult<SipResult> SipFutureValue(decimal monthlyInvestment, decimal expectedAnnualReturnPercent, int months)
    {
        var invalid = ValidateRecurring<SipResult>(monthlyInvestment, "monthly investment", expectedAnnualReturnPercent, months);
        if (invalid is not null)
        {
            return invalid;
        }

        try
        {
            decimal monthlyRate = expectedAnnualReturnPercent / 12m / 100m;
            decimal growth = 1m + monthlyRate;
            decimal invested = monthlyInvestment * months;

            // Money goes in at the start of each month: FV = P x ((1 + i)^n - 1) / i x (1 + i).
            // With no growth the formula would divide by zero, and the answer is just the money put in.
            decimal futureValue = growth == 1m
                ? invested
                : monthlyInvestment * (Power(growth, months) - 1m) / monthlyRate * growth;

            decimal roundedFutureValue = Round(futureValue);

            return CalcResult<SipResult>.Success(
                new SipResult(invested, roundedFutureValue - invested, roundedFutureValue));
        }
        catch (OverflowException)
        {
            return OverflowFailure<SipResult>();
        }
    }

    /// <inheritdoc />
    public CalcResult<RoiResult> ReturnOnInvestment(decimal amountInvested, decimal finalValue)
    {
        if (amountInvested <= 0m)
        {
            return Invalid<RoiResult>("The amount invested must be greater than zero.");
        }

        if (finalValue < 0m)
        {
            return Invalid<RoiResult>("The final value cannot be negative.");
        }

        try
        {
            decimal gain = finalValue - amountInvested;
            decimal percent = Round(gain / amountInvested * 100m);

            return CalcResult<RoiResult>.Success(new RoiResult(Round(gain), percent));
        }
        catch (OverflowException)
        {
            return OverflowFailure<RoiResult>();
        }
    }

    /// <inheritdoc />
    public CalcResult<GstResult> Gst(decimal amount, decimal gstRatePercent, GstMode mode = GstMode.Exclusive)
    {
        if (amount < 0m)
        {
            return Invalid<GstResult>("The amount cannot be negative.");
        }

        if (gstRatePercent < 0m)
        {
            return Invalid<GstResult>("The GST rate cannot be negative.");
        }

        if (!Enum.IsDefined(mode))
        {
            return Invalid<GstResult>("The GST mode is not valid.");
        }

        try
        {
            if (mode == GstMode.Exclusive)
            {
                decimal gst = Round(amount * gstRatePercent / 100m);
                return CalcResult<GstResult>.Success(new GstResult(amount, gst, amount + gst));
            }

            // The amount already contains GST: net = amount x 100 / (100 + rate).
            decimal net = Round(amount * 100m / (100m + gstRatePercent));
            return CalcResult<GstResult>.Success(new GstResult(net, amount - net, amount));
        }
        catch (OverflowException)
        {
            return OverflowFailure<GstResult>();
        }
    }

    // EMI = P x r / (1 - (1 + r)^-n), where r is the monthly rate. Raising 1/(1 + r) to the power n,
    // instead of dividing by a huge (1 + r)^n, makes a very long loan shrink towards zero instead of overflowing.
    private static decimal ExactEmi(decimal loanAmount, decimal annualRatePercent, int months)
    {
        if (annualRatePercent == 0m)
        {
            return loanAmount / months;
        }

        decimal monthlyRate = annualRatePercent / 12m / 100m;
        decimal discount = Power(1m / (1m + monthlyRate), months);
        decimal denominator = 1m - discount;

        // A rate so tiny that 1 + r rounds to 1 behaves like no interest at all.
        return denominator == 0m
            ? loanAmount / months
            : loanAmount * monthlyRate / denominator;
    }

    private static CalcResult<T>? ValidateRecurring<T>(decimal amount, string amountName, decimal annualRatePercent, int months)
    {
        if (amount <= 0m)
        {
            return Invalid<T>($"The {amountName} must be greater than zero.");
        }

        if (annualRatePercent < 0m)
        {
            return Invalid<T>("The rate cannot be negative.");
        }

        if (months < 1 || months > MaxMonths)
        {
            return Invalid<T>($"The number of months must be between 1 and {MaxMonths}.");
        }

        return null;
    }

    private static CalcResult<T> Invalid<T>(string message) =>
        CalcResult<T>.Failure(CalcError.InvalidInput, message);

    private static CalcResult<T> OverflowFailure<T>() =>
        CalcResult<T>.Failure(CalcError.Overflow, "The result is too large to represent.");
}