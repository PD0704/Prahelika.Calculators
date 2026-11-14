using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Prahelika.Calculators.Web.Models;

namespace Prahelika.Calculators.Web.Pages;

public class BasicModel : PageModel
{
    [BindProperty]
    public BasicOperation Operation { get; set; } = BasicOperation.Add;

    // decimal? (nullable) so an empty box stays empty instead of showing 0.
    [BindProperty]
    public decimal? A { get; set; }

    [BindProperty]
    public decimal? B { get; set; }

    public void OnGet()
    {
    }

    public void OnPost()
    {
        // The calculation is connected in task 3.6.
        // For now the page just shows the form again, with the values you typed.
    }
}