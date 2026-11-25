namespace Prahelika.Calculators.Core.Finance;

/// <summary>
/// "What if" calculations for loans, using <see cref="decimal"/> arithmetic.
/// Months are worked out one at a time with the same rules as <see cref="FinanceCalculator.AmortizationSchedule"/>.
/// </summary>
public sealed class LoanPlanner : ILoanPlanner
{
    private readonly FinanceCalculator _finance;

    /// <param name="decimalPlaces">How many decimal places to round amounts to (0 to 10). The default is 2.</param>
    public LoanPlanner(int decimalPlaces = 2)
    {
        // FinanceCalculator checks the range and throws for a value that is not allowed.
        _finance = new FinanceCalculator(decimalPlaces);
    }

    /// <inheritdoc />
    public int DecimalPlaces => _finance.DecimalPlaces;

    /// <inheritdoc />
    public CalcResult<PrepaymentResult> Prepay(
        decimal loanAmount,
        decimal annualRatePercent,
        int months,
        int prepaymentMonth,
        decimal prepaymentAmount)
    {
        // The loan amount, rate and months follow the same rules as the EMI calculation.
        var emi = _finance.Emi(loanAmount, annualRatePercent, months);
        if (!emi.IsSuccess)
        {
            return CalcResult<PrepaymentResult>.Failure(emi.Error, emi.Message ?? "The loan details are not valid.");
        }

        if (prepaymentMonth < 1 || prepaymentMonth >= months)
        {
            return Invalid("The prepayment month must be at least 1 and less than the number of months.");
        }

        if (prepaymentAmount <= 0m)
        {
            return Invalid("The prepayment amount must be above zero.");
        }

        try
        {
            decimal payment = emi.Value!.MonthlyPayment;

            Repayment original = Repay(loanAmount, annualRatePercent, months, payment, prepaymentMonth: 0, prepaymentAmount: 0m);
            Repayment withPrepayment = Repay(loanAmount, annualRatePercent, months, payment, prepaymentMonth, prepaymentAmount);

            return CalcResult<PrepaymentResult>.Success(new PrepaymentResult(
                payment,
                months,
                withPrepayment.Months,
                months - withPrepayment.Months,
                withPrepayment.LumpSumApplied,
                original.TotalInterest,
                withPrepayment.TotalInterest,
                original.TotalInterest - withPrepayment.TotalInterest));
        }
        catch (OverflowException)
        {
            return CalcResult<PrepaymentResult>.Failure(CalcError.Overflow, "The result is too large to represent.");
        }
    }

    private readonly record struct Repayment(int Months, decimal TotalInterest, decimal LumpSumApplied);

    // Pays the loan off month by month. A prepayment month of 0 means there is no prepayment.
    private Repayment Repay(
        decimal loanAmount, decimal annualRatePercent, int months, decimal payment,
        int prepaymentMonth, decimal prepaymentAmount)
    {
        decimal monthlyRate = annualRatePercent / 12m / 100m;
        decimal balance = loanAmount;
        decimal totalInterest = 0m;
        decimal lumpSumApplied = 0m;

        for (int month = 1; month <= months; month++)
        {
            decimal interest = Round(balance * monthlyRate);

            // The same rule as the schedule: the last month clears whatever is left.
            decimal principalPaid = month == months
                ? balance
                : Math.Clamp(payment - interest, 0m, balance);

            balance -= principalPaid;
            totalInterest += interest;

            // The lump sum goes in right after that month's EMI. It cannot be more than what is still owed.
            if (month == prepaymentMonth)
            {
                lumpSumApplied = Math.Min(prepaymentAmount, balance);
                balance -= lumpSumApplied;
            }

            if (balance == 0m)
            {
                return new Repayment(month, totalInterest, lumpSumApplied);
            }
        }

        return new Repayment(months, totalInterest, lumpSumApplied);
    }

    private decimal Round(decimal value) => decimal.Round(value, DecimalPlaces, MidpointRounding.AwayFromZero);

    private static CalcResult<PrepaymentResult> Invalid(string message) =>
        CalcResult<PrepaymentResult>.Failure(CalcError.InvalidInput, message);

    /// <inheritdoc />
    public CalcResult<LoanComparison> CompareLoans(LoanOffer first, LoanOffer second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        try
        {
            var firstSummary = Summarise(first, "First loan");
            if (!firstSummary.IsSuccess)
            {
                return CalcResult<LoanComparison>.Failure(firstSummary.Error, firstSummary.Message ?? "The first loan is not valid.");
            }

            var secondSummary = Summarise(second, "Second loan");
            if (!secondSummary.IsSuccess)
            {
                return CalcResult<LoanComparison>.Failure(secondSummary.Error, secondSummary.Message ?? "The second loan is not valid.");
            }

            LoanOfferSummary a = firstSummary.Value!;
            LoanOfferSummary b = secondSummary.Value!;

            LoanChoice cheaper = a.TotalCost < b.TotalCost ? LoanChoice.First
                : b.TotalCost < a.TotalCost ? LoanChoice.Second
                : LoanChoice.Same;

            return CalcResult<LoanComparison>.Success(
                new LoanComparison(a, b, cheaper, Math.Abs(a.TotalCost - b.TotalCost)));
        }
        catch (OverflowException)
        {
            return CalcResult<LoanComparison>.Failure(CalcError.Overflow, "The result is too large to represent.");
        }
    }

    private CalcResult<LoanOfferSummary> Summarise(LoanOffer offer, string name)
    {
        if (offer.ProcessingFee < 0m)
        {
            return CalcResult<LoanOfferSummary>.Failure(CalcError.InvalidInput, $"{name}: the processing fee cannot be negative.");
        }

        // The loan amount, rate and months follow the same rules (and give the same numbers) as the EMI calculation.
        var emi = _finance.Emi(offer.LoanAmount, offer.AnnualRatePercent, offer.Months);
        if (!emi.IsSuccess)
        {
            return CalcResult<LoanOfferSummary>.Failure(emi.Error, $"{name}: {emi.Message}");
        }

        LoanSummary loan = emi.Value!;
        decimal fee = Round(offer.ProcessingFee);

        return CalcResult<LoanOfferSummary>.Success(new LoanOfferSummary(
            loan.MonthlyPayment, loan.TotalPayment, loan.TotalInterest, fee, loan.TotalInterest + fee));
    }

    /// <inheritdoc />
    public CalcResult<BorrowingResult> MaxLoanAmount(decimal monthlyPayment, decimal annualRatePercent, int months)
    {
        if (monthlyPayment <= 0m)
        {
            return InvalidBorrowing("The monthly payment must be above zero.");
        }

        if (annualRatePercent < 0m)
        {
            return InvalidBorrowing("The rate cannot be negative.");
        }

        if (months < 1 || months > FinanceCalculator.MaxMonths)
        {
            return InvalidBorrowing($"The number of months must be between 1 and {FinanceCalculator.MaxMonths}.");
        }

        try
        {
            // Rounded down, so the EMI for this loan can never be more than the payment.
            decimal maxLoan = decimal.Round(
                ExactMaxLoan(monthlyPayment, annualRatePercent, months), DecimalPlaces, MidpointRounding.ToZero);
            decimal totalPayment = monthlyPayment * months;

            return CalcResult<BorrowingResult>.Success(
                new BorrowingResult(monthlyPayment, maxLoan, totalPayment, totalPayment - maxLoan));
        }
        catch (OverflowException)
        {
            return CalcResult<BorrowingResult>.Failure(CalcError.Overflow, "The result is too large to represent.");
        }
    }

    // P = EMI x (1 - (1 + r)^-n) / r, where r is the monthly rate: the EMI formula solved for the loan amount.
    private static decimal ExactMaxLoan(decimal monthlyPayment, decimal annualRatePercent, int months)
    {
        if (annualRatePercent == 0m)
        {
            return monthlyPayment * months;
        }

        decimal monthlyRate = annualRatePercent / 12m / 100m;
        decimal discount = Power(1m / (1m + monthlyRate), months);
        decimal numerator = 1m - discount;

        // A rate so tiny that 1 + r rounds to 1 behaves like no interest at all.
        return numerator == 0m
            ? monthlyPayment * months
            : monthlyPayment * numerator / monthlyRate;
    }

    // baseValue ^ exponent, by repeated squaring.
    private static decimal Power(decimal baseValue, int exponent)
    {
        decimal result = 1m;
        decimal factor = baseValue;

        for (int remaining = exponent; remaining > 0; remaining >>= 1)
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

        return result;
    }

    private static CalcResult<BorrowingResult> InvalidBorrowing(string message) =>
        CalcResult<BorrowingResult>.Failure(CalcError.InvalidInput, message);
}