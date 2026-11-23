# Prahelika.Calculators

[![NuGet](https://img.shields.io/nuget/vpre/Prahelika.Calculators)](https://www.nuget.org/packages/Prahelika.Calculators)
[![CI](https://github.com/PD0704/Prahelika.Calculators/actions/workflows/ci.yml/badge.svg)](https://github.com/PD0704/Prahelika.Calculators/actions/workflows/ci.yml)

A small, well-tested .NET library with basic, scientific, finance and India-focused calculators (FD, RD, PPF, GST split, income tax). Calculations never throw for bad user input: they return a result object that says what went wrong.

## Install

```
dotnet add package Prahelika.Calculators
```

## Quick start

### Basic (exact `decimal` arithmetic)

```csharp
using Prahelika.Calculators.Core.Basic;

var calculator = new BasicCalculator();

var result = calculator.Divide(10m, 4m);

if (result.IsSuccess)
{
    Console.WriteLine(result.Value);   // 2.5
}
else
{
    Console.WriteLine(result.Message); // for example, a division by zero message
}
```

### Scientific (`double` arithmetic, degrees or radians)

The angle unit is chosen when you create the calculator. Radians is the default.

```csharp
using Prahelika.Calculators.Core.Scientific;

var radians = new ScientificCalculator();
var degrees = new ScientificCalculator(AngleUnit.Degrees);

Console.WriteLine(radians.SquareRoot(16).Value); // 4
Console.WriteLine(degrees.Sin(30).Value);        // about 0.5
```

Functions: power, square root, nth root, natural log, log10, log in any base, sin, cos, tan, asin, acos, atan and factorial.

### Finance (`decimal`, rounded to 2 places by default)

Rates are percentages per year, so `12` means 12%.

```csharp
using Prahelika.Calculators.Core.Finance;

var finance = new FinanceCalculator();

// Loan of 100,000 at 12% a year for 12 months
var emi = finance.Emi(100000m, 12m, 12);

if (emi.IsSuccess)
{
    Console.WriteLine(emi.Value.MonthlyPayment); // 8884.88
}
```

Also available: simple and compound interest, loan amortisation schedule, SIP future value, return on investment and GST (added or taken out).

### India finance: FD, RD, PPF, GST split and income tax

```csharp
using Prahelika.Calculators.Core.Finance;

// Fixed deposit: 1,00,000 at 7% for 12 months, interest added every quarter
var fd = new SavingsCalculator().FixedDeposit(100000m, 7m, 12);
Console.WriteLine(fd.Value!.MaturityAmount);     // 107185.90

// GST of 18% on 1000, split into CGST and SGST
var split = new GstSplitCalculator().GstSplit(1000m, 18m);
Console.WriteLine(split.Value!.Cgst);            // 90 (and SGST is 90)

// Income-tax estimate, new regime, salaried, 15 lakh a year
var tax = new IncomeTaxCalculator().EstimateNewRegime(1500000m);
Console.WriteLine(tax.Value!.TotalTax);          // 97500
```

Also available: recurring deposit (`RecurringDeposit`) and PPF (`Ppf`). The income-tax slabs for FY 2026-27 live in one `IncomeTaxRules` table, so they can be updated or replaced. The estimate does not include surcharge. Check [incometax.gov.in](https://incometax.gov.in) for the latest rules.

## Handling errors

Every calculation returns a `CalcResult<T>`:

| Member | Meaning |
|---|---|
| `IsSuccess` | `true` when the calculation worked |
| `Value` | The answer (only when successful) |
| `Error` | `DivisionByZero`, `InvalidInput`, `Overflow`, or `None` |
| `Message` | A readable explanation of the error |

Only mistakes in your own code, such as an invalid number of decimal places in a constructor, throw exceptions.

## Links

- [Live demo](https://prahelika-calculators.onrender.com) and [live API with Swagger](https://prahelika-calculators-api.onrender.com)
- [Source code](https://github.com/PD0704/Prahelika.Calculators)
- [Changelog](https://github.com/PD0704/Prahelika.Calculators/blob/main/CHANGELOG.md)
- Licence: MIT