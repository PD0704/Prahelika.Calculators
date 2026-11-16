using Prahelika.Calculators.Core.Basic;
using Microsoft.AspNetCore.Mvc;
using Prahelika.Calculators.Web.Models;

namespace Prahelika.Calculators.Web.Pages;

public class BasicModel(IBasicCalculator calculator) : CalculatorPageModel
{
    [BindProperty]
    public BasicOperation Operation { get; set; } = BasicOperation.Add;

    [BindProperty]
    public decimal? A { get; set; }

    [BindProperty]
    public decimal? B { get; set; }

    public void OnGet()
    {
    }

    public void OnPost()
    {
        if (!ModelState.IsValid)
        {
            Fail("Please enter valid numbers.");
            return;
        }

        if (A is null || B is null)
        {
            Fail("Please enter both numbers.");
            return;
        }

        decimal a = A.Value;
        decimal b = B.Value;

        var result = Operation switch
        {
            BasicOperation.Add => calculator.Add(a, b),
            BasicOperation.Subtract => calculator.Subtract(a, b),
            BasicOperation.Multiply => calculator.Multiply(a, b),
            BasicOperation.Divide => calculator.Divide(a, b),
            BasicOperation.Modulo => calculator.Modulo(a, b),
            // "B% of A". If your Percentage method takes its arguments the other way round, swap them here.
            BasicOperation.Percentage => calculator.Percentage(a, b),
            _ => throw new InvalidOperationException($"Unknown operation {Operation}.")
        };

        Show(result);
    }
}