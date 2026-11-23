using System.Globalization;
using Prahelika.Calculators.Core.Basic;
using Prahelika.Calculators.Core.Finance;
using Prahelika.Calculators.Core.Scientific;

var builder = WebApplication.CreateBuilder(args);

// Numbers in the forms always use "." as the decimal point, whatever the server's language is.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddSingleton<IBasicCalculator, BasicCalculator>();
builder.Services.AddSingleton<IScientificCalculator, ScientificCalculator>();
builder.Services.AddSingleton<IFinanceCalculator, FinanceCalculator>();
builder.Services.AddSingleton<ISavingsCalculator, SavingsCalculator>();
builder.Services.AddSingleton<IGstSplitCalculator, GstSplitCalculator>();
builder.Services.AddSingleton<IIncomeTaxCalculator, IncomeTaxCalculator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

public partial class Program { }