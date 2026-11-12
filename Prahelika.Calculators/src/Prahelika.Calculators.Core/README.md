# Prahelika.Calculators

A calculator library for .NET. Calculations return a result object instead of throwing exceptions, so errors like division by zero are easy to handle.

> Early alpha: the API may change. Currently includes the Basic calculator. Scientific and Finance calculators are planned.

## Install

```
dotnet add package Prahelika.Calculators --prerelease
```

## Usage

```csharp
using Prahelika.Calculators.Core.Basic;

var calculator = new BasicCalculator();

var sum = calculator.Add(2m, 3m);
if (sum.IsSuccess)
{
    Console.WriteLine(sum.Value);   // 5
}

var bad = calculator.Divide(10m, 0m);
if (!bad.IsSuccess)
{
    Console.WriteLine(bad.Error);   // DivisionByZero
}
```

## Links

Source code and issues: https://github.com/PD0704/Prahelika.Calculators

Licensed under the MIT license.