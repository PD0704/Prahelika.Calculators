namespace Prahelika.Calculators.Core.Finance;

/// <summary>GST calculations that show the CGST and SGST halves, using <see cref="decimal"/> arithmetic.</summary>
public interface IGstSplitCalculator
{
    /// <summary>The number of decimal places that money amounts are rounded to.</summary>
    int DecimalPlaces { get; }

    /// <summary>
    /// Works out GST like <see cref="IFinanceCalculator.Gst"/>, then splits it equally into CGST and SGST,
    /// as for a sale within one state. The rate is the total GST rate: 18 gives 9% CGST and 9% SGST.
    /// CGST is rounded first, and SGST is what remains, so the two always add up to the total GST.
    /// </summary>
    CalcResult<GstSplitResult> GstSplit(decimal amount, decimal gstRatePercent, GstMode mode = GstMode.Exclusive);
}