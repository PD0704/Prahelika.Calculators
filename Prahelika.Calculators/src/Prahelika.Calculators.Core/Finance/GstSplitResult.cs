namespace Prahelika.Calculators.Core.Finance;

/// <summary>The outcome of a GST calculation split into its central and state halves.</summary>
/// <param name="NetAmount">The amount before GST.</param>
/// <param name="Cgst">The central GST (CGST): half of the total GST.</param>
/// <param name="Sgst">The state GST (SGST): the other half, so that CGST + SGST always equals the total GST.</param>
/// <param name="TotalGst">The total GST: CGST plus SGST.</param>
/// <param name="TotalAmount">The amount including GST.</param>
public sealed record GstSplitResult(decimal NetAmount, decimal Cgst, decimal Sgst, decimal TotalGst, decimal TotalAmount);