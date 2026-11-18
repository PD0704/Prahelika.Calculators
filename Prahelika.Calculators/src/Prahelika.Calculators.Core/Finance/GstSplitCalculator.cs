namespace Prahelika.Calculators.Core.Finance;

/// <summary>
/// GST calculations split into CGST and SGST, using <see cref="decimal"/> arithmetic.
/// The GST itself comes from <see cref="FinanceCalculator.Gst"/>, so both follow the same rules.
/// </summary>
public sealed class GstSplitCalculator : IGstSplitCalculator
{
    private readonly FinanceCalculator _finance;

    /// <param name="decimalPlaces">How many decimal places to round amounts to (0 to 10). The default is 2.</param>
    public GstSplitCalculator(int decimalPlaces = 2)
    {
        // FinanceCalculator checks the range and throws for a value that is not allowed.
        _finance = new FinanceCalculator(decimalPlaces);
    }

    /// <inheritdoc />
    public int DecimalPlaces => _finance.DecimalPlaces;

    /// <inheritdoc />
    public CalcResult<GstSplitResult> GstSplit(decimal amount, decimal gstRatePercent, GstMode mode = GstMode.Exclusive)
    {
        var gst = _finance.Gst(amount, gstRatePercent, mode);

        if (!gst.IsSuccess)
        {
            return CalcResult<GstSplitResult>.Failure(gst.Error, gst.Message ?? string.Empty);
        }

        GstResult values = gst.Value!;

        decimal cgst = decimal.Round(values.GstAmount / 2m, DecimalPlaces, MidpointRounding.AwayFromZero);
        decimal sgst = values.GstAmount - cgst;

        return CalcResult<GstSplitResult>.Success(
            new GstSplitResult(values.NetAmount, cgst, sgst, values.GstAmount, values.TotalAmount));
    }
}