using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Prahelika.Calculators.Web.Models;

public enum BasicOperation
{
    Add,
    Subtract,
    Multiply,
    Divide,
    [Display(Name = "Remainder (modulo)")]
    Modulo,
    [Display(Name = "Percentage (B% of A)")]
    Percentage
}

public enum ScientificOperation
{
    [Display(Name = "Power (X to the power Y)")]
    Power,
    [Display(Name = "Square root")]
    SquareRoot,
    [Display(Name = "Nth root (Y-th root of X)")]
    NthRoot,
    [Display(Name = "Natural log (ln)")]
    NaturalLog,
    [Display(Name = "Log base 10")]
    Log10,
    [Display(Name = "Log of X in base Y")]
    Log,
    [Display(Name = "Sine (sin)")]
    Sin,
    [Display(Name = "Cosine (cos)")]
    Cos,
    [Display(Name = "Tangent (tan)")]
    Tan,
    [Display(Name = "Inverse sine (asin)")]
    Asin,
    [Display(Name = "Inverse cosine (acos)")]
    Acos,
    [Display(Name = "Inverse tangent (atan)")]
    Atan,
    Factorial
}

public enum FinanceCalculation
{
    [Display(Name = "Simple interest")]
    SimpleInterest,
    [Display(Name = "Compound interest")]
    CompoundInterest,
    [Display(Name = "Loan EMI")]
    Emi,
    [Display(Name = "Loan schedule")]
    Amortization,
    [Display(Name = "SIP")]
    Sip,
    [Display(Name = "ROI")]
    Roi,
    [Display(Name = "GST")]
    Gst
}

public enum IndiaCalculation
{
    [Display(Name = "Fixed deposit (FD)")]
    FixedDeposit,
    [Display(Name = "Recurring deposit (RD)")]
    RecurringDeposit,
    [Display(Name = "PPF")]
    Ppf,
    [Display(Name = "GST split (CGST + SGST)")]
    GstSplit,
    [Display(Name = "Income tax (new regime)")]
    IncomeTax
}

public static class EnumExtensions
{
    /// <summary>The friendly name from a [Display(Name = "...")] attribute, or the enum name if there is none.</summary>
    public static string DisplayName(this Enum value) =>
        value.GetType().GetField(value.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName()
        ?? value.ToString();
}