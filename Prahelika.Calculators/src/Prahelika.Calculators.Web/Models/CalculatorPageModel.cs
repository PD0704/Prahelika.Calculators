using Microsoft.AspNetCore.Mvc.RazorPages;
using Prahelika.Calculators.Core;

namespace Prahelika.Calculators.Web.Models;

/// <summary>Shared by the three calculator pages: holds the result or the error to show.</summary>
public abstract class CalculatorPageModel : PageModel
{
    public ResultView? Result { get; private set; }

    public string? ErrorMessage { get; private set; }

    /// <summary>A small extra line under the result, such as which angle unit was used.</summary>
    public string? Note { get; protected set; }

    protected void Fail(string message)
    {
        Result = null;
        ErrorMessage = message;
    }

    /// <summary>Shows a result that the page built itself, such as a side-by-side table.</summary>
    protected void Display(ResultView view)
    {
        ErrorMessage = null;
        Result = view;
    }

    protected void Show<T>(CalcResult<T> calcResult, bool money = false)
    {
        if (calcResult.IsSuccess)
        {
            Result = ResultView.From(calcResult.Value, money);
        }
        else
        {
            Fail(string.IsNullOrWhiteSpace(calcResult.Message)
                ? "That calculation could not be done."
                : calcResult.Message);
        }
    }
}