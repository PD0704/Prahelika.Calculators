using Prahelika.Calculators.Core;
using Prahelika.Calculators.Core.Finance;

namespace Prahelika.Calculators.Api.Endpoints;

public static class FinanceEndpoints
{
    public static IEndpointRouteBuilder MapFinanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/finance").WithTags("Finance");

        // IFinanceCalculator is supplied by dependency injection (registered in Program.cs).
        // All rates are percentages per year: 5 means 5%.

        group.MapGet("/simple-interest",
                (decimal principal, decimal rate, decimal years, IFinanceCalculator calc) =>
                    calc.SimpleInterest(principal, rate, years).ToHttpResult())
            .WithSummary("Simple interest on a principal for a number of years.")
            .ProducesCalculation<InterestResult>();

        group.MapGet("/compound-interest",
                (decimal principal, decimal rate, decimal years, IFinanceCalculator calc, string frequency = "annually") =>
                {
                    if (!EnumParser.TryParseName(frequency, out CompoundingFrequency parsed))
                    {
                        return ResultMapping.ToProblem(
                            CalcError.InvalidInput,
                            "The frequency must be one of: annually, semiAnnually, quarterly, monthly, weekly, daily.");
                    }

                    return calc.CompoundInterest(principal, rate, years, parsed).ToHttpResult();
                })
            .WithSummary("Compound interest. The frequency is how often interest is added (default: annually).")
            .ProducesCalculation<InterestResult>();

        group.MapGet("/emi",
                (decimal loanAmount, decimal rate, int months, IFinanceCalculator calc) =>
                    calc.Emi(loanAmount, rate, months).ToHttpResult())
            .WithSummary("Monthly loan payment (EMI), total payment and total interest. Months: 1 to 1200.")
            .ProducesCalculation<LoanSummary>();

        group.MapGet("/amortization",
                (decimal loanAmount, decimal rate, int months, IFinanceCalculator calc) =>
                    calc.AmortizationSchedule(loanAmount, rate, months).ToHttpResult())
            .WithSummary("Month-by-month repayment schedule. The last payment clears the remaining balance.")
            .ProducesCalculation<IReadOnlyList<AmortizationRow>>();

        group.MapGet("/sip",
                (decimal monthlyInvestment, decimal rate, int months, IFinanceCalculator calc) =>
                    calc.SipFutureValue(monthlyInvestment, rate, months).ToHttpResult())
            .WithSummary("Future value of a monthly investment (SIP). The rate is the expected return per year.")
            .ProducesCalculation<SipResult>();

        group.MapGet("/roi",
                (decimal amountInvested, decimal finalValue, IFinanceCalculator calc) =>
                    calc.ReturnOnInvestment(amountInvested, finalValue).ToHttpResult())
            .WithSummary("Gain and return on investment as a percentage.")
            .ProducesCalculation<RoiResult>();

        group.MapGet("/gst",
                (decimal amount, decimal rate, IFinanceCalculator calc, string mode = "exclusive") =>
                {
                    if (!EnumParser.TryParseName(mode, out GstMode parsed))
                    {
                        return ResultMapping.ToProblem(
                            CalcError.InvalidInput,
                            "The mode must be 'exclusive' (amount is before GST) or 'inclusive' (amount includes GST).");
                    }

                    return calc.Gst(amount, rate, parsed).ToHttpResult();
                })
            .WithSummary("GST on an amount. Exclusive adds GST to the amount; inclusive takes it out.")
            .ProducesCalculation<GstResult>();


        // ---- India-focused calculators ----
        // Rates are percentages per year: 7.1 means 7.1%. Amounts are in rupees.

        group.MapGet("/fixed-deposit",
                (decimal principal, decimal rate, int months, ISavingsCalculator calc, string frequency = "quarterly") =>
                {
                    if (!EnumParser.TryParseName(frequency, out CompoundingFrequency parsed))
                    {
                        return ResultMapping.ToProblem(
                            CalcError.InvalidInput,
                            "The frequency must be one of: annually, semiAnnually, quarterly, monthly, weekly, daily.");
                    }

                    return calc.FixedDeposit(principal, rate, months, parsed).ToHttpResult();
                })
            .WithSummary("Fixed deposit (FD): maturity amount for a lump sum. Compounded quarterly by default. Months: 1 to 1200.")
            .ProducesCalculation<MaturityResult>();

        group.MapGet("/recurring-deposit",
                (decimal monthlyDeposit, decimal rate, int months, ISavingsCalculator calc) =>
                    calc.RecurringDeposit(monthlyDeposit, rate, months).ToHttpResult())
            .WithSummary("Recurring deposit (RD): maturity amount for a deposit at the start of every month, compounded quarterly. Months: 1 to 1200.")
            .ProducesCalculation<MaturityResult>();

        group.MapGet("/ppf",
                (decimal yearlyDeposit, decimal rate, int years, ISavingsCalculator calc) =>
                    calc.Ppf(yearlyDeposit, rate, years).ToHttpResult())
            .WithSummary("PPF maturity amount. Yearly deposit 500 to 150,000. Years: 15, or longer in blocks of 5 (20, 25, up to 50).")
            .ProducesCalculation<MaturityResult>();

        group.MapGet("/gst-split",
                (decimal amount, decimal rate, IGstSplitCalculator calc, string mode = "exclusive") =>
                {
                    if (!EnumParser.TryParseName(mode, out GstMode parsed))
                    {
                        return ResultMapping.ToProblem(
                            CalcError.InvalidInput,
                            "The mode must be 'exclusive' (amount is before GST) or 'inclusive' (amount includes GST).");
                    }

                    return calc.GstSplit(amount, rate, parsed).ToHttpResult();
                })
            .WithSummary("GST split into CGST and SGST halves. The rate is the total GST rate: 18 gives 9% CGST and 9% SGST.")
            .ProducesCalculation<GstSplitResult>();

        group.MapGet("/income-tax",
                (decimal income, IIncomeTaxCalculator calc, bool salaried = true) =>
                    calc.EstimateNewRegime(income, salaried).ToHttpResult())
            .WithSummary("Income-tax estimate under the new regime (FY 2026-27): slab breakdown, standard deduction if salaried, rebate and cess. Surcharge is not included.")
            .ProducesCalculation<IncomeTaxResult>();

        return app;
    }
}