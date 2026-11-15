using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Prahelika.Calculators.Core.Scientific;
using Prahelika.Calculators.Web.Models;

namespace Prahelika.Calculators.Web.Pages;

public class ScientificModel : PageModel
{
    [BindProperty]
    public ScientificOperation Operation { get; set; } = ScientificOperation.Power;

    [BindProperty]
    public double? X { get; set; }

    // The second value is only used by power, nth root and log of X in base Y.
    [BindProperty]
    public double? Y { get; set; }

    [BindProperty]
    public AngleUnit Unit { get; set; } = AngleUnit.Radians;

    public void OnGet()
    {
    }

    public void OnPost()
    {
        // The calculation is connected in task 3.6.
    }
}