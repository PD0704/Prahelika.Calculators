# Prahelika.Calculators

[![NuGet](https://img.shields.io/nuget/vpre/Prahelika.Calculators)](https://www.nuget.org/packages/Prahelika.Calculators)
[![CI](https://github.com/PD0704/Prahelika.Calculators/actions/workflows/ci.yml/badge.svg)](https://github.com/PD0704/Prahelika.Calculators/actions/workflows/ci.yml)

A small, well-tested .NET library with basic, scientific and finance calculators. Calculations never throw for bad user input: they return a result object that says what went wrong.

> This is a pre-release (alpha). The API may still change before 1.0.

## Install

```
dotnet add package Prahelika.Calculators --prerelease
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
    Console.WriteLine(emi.Value); // monthly payment 8884.88, total payment and total interest
}
```

Also available: simple and compound interest, loan amortisation schedule, SIP future value, return on investment and GST (added or taken out).

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

- [Live demo](https://prahelika-calculators.onrender.com)
- [Source code and API](https://github.com/PD0704/Prahelika.Calculators)
- [Changelog](https://github.com/PD0704/Prahelika.Calculators/blob/main/CHANGELOG.md)
- Licence: MIT