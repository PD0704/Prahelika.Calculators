# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- `LoanPlanner.Prepay` shows what a lump-sum prepayment does to a loan: the new number of months, and the interest saved. The EMI stays the same.
- `LoanPlanner.CompareLoans` compares two loans side by side: EMI, total interest, processing fee and total cost of each, and which is cheaper overall.

## [1.1.0] - 2026-11-23

New India-focused finance calculators, in the library, the API and the website. Nothing from 1.0.0 was changed or removed, so it is safe to update.

### Added
- `SavingsCalculator` with `FixedDeposit` (FD), `RecurringDeposit` (RD) and `Ppf` maturity calculations, with quarterly compounding by default for deposits.
- `GstSplitCalculator.GstSplit` splits GST into CGST and SGST halves (CGST + SGST always equals the total GST). The existing `Gst` method and `GstResult` are unchanged.
- `IncomeTaxCalculator.EstimateNewRegime` estimates income tax under the new regime for FY 2026-27: slab-by-slab breakdown, standard deduction, rebate with marginal relief, and 4% cess. The slabs and limits live in one `IncomeTaxRules` table, so they can be updated each year or replaced.
- API endpoints for the new calculators, under `/api/finance`: `fixed-deposit`, `recurring-deposit`, `ppf`, `gst-split` and `income-tax`.
- Website page **India finance** with the FD, RD, PPF, GST split (CGST and SGST) and income-tax calculators. The income-tax result shows a slab-by-slab breakdown.
- `Dockerfile.api` to run the API as its own service, and a live API with Swagger documentation.

### Changed
- Result labels now write GST, CGST and SGST in capitals.
- The package README no longer says "pre-release", and installing no longer needs `--prerelease`.

## [1.0.0] - 2026-11-16

First stable release. It includes everything from the 0.x releases: the Basic, Scientific and Finance calculators, the REST API with Swagger, and the website.

### Added
- Package icon, a full package README, XML documentation comments (shown in IntelliSense) and Source Link, so you can step into the library's code while debugging.
- Live demo of the website.

### Changed
- The second parameter of `Log` is now named `logBase` (it was `newBase`).

## [0.3.1-alpha] - 2026-11-16

### Added
- Releases are now published to NuGet and GitHub automatically when a version tag is pushed. This is the first NuGet package with the calculator code (earlier versions were a placeholder).
- Package icon, XML documentation comments (shown in IntelliSense) and Source Link, so you can step into the library's code while debugging.

## [0.3.0-alpha] - 2026-11-15

### Added
- `FinanceCalculator` with simple interest and compound interest (annual, semi-annual, quarterly, monthly, weekly or daily compounding), using `decimal` arithmetic and rounding to 2 decimal places by default.
- Loan calculations: `Emi` (monthly payment, total payment and total interest) and `AmortizationSchedule` (month-by-month payment, principal, interest and balance), for loans of up to 1200 months.
- `SipFutureValue`: the future value of a monthly investment (SIP) with an expected annual return.
- `ReturnOnInvestment`: the gain and the ROI percentage.
- `Gst`: GST added to a price (exclusive) or taken out of one (inclusive).
- Finance endpoints in the API under `/api/finance`: `simple-interest`, `compound-interest`, `emi`, `amortization`, `sip`, `roi` and `gst`, with integration tests.
- Web UI built with Razor Pages: Basic, Scientific and Finance calculator pages with results and friendly error messages.
- Scientific page lets you choose degrees or radians and shows which unit was used.

### Changed
- The scientific `unit` parameter no longer accepts bare numbers such as `1`; use `radians` or `degrees`.

## [0.2.0-alpha] - 2026-11-13

### Added
- `ScientificCalculator`: power, square root, nth root, natural log, log10, log in any base, sin, cos, tan, asin, acos, atan and factorial, using `double` arithmetic.
- `ScientificConstants` with `Pi` and `E`.
- Angle unit (radians by default, or degrees) chosen when the scientific calculator is created, and exposed through the `AngleUnit` property.
- ASP.NET Core 10 Minimal API with endpoints for the Basic and Scientific calculators.
- Swagger UI at `/swagger` and an OpenAPI description at `/openapi/v1.json`.
- Trigonometry endpoints accept a `unit` parameter and report which unit was used.
- Unit tests for the Scientific calculator and integration tests for the API, using `WebApplicationFactory`.
- CI uploads test coverage to Codecov, with a coverage badge in the README.

### Changed
- Invalid scientific input (for example the square root of a negative number, or the logarithm of zero) returns a `CalcError` instead of `NaN` or `Infinity`.
- API errors use the standard problem-details format (RFC 9457), with an extra `code` field.

## [0.1.0-alpha] - 2026-11-12

### Added
- `CalcResult<T>` and `CalcError`: calculations return a result object instead of throwing exceptions.
- `IBasicCalculator` and `BasicCalculator`: add, subtract, multiply, divide, modulo and percentage, using `decimal` arithmetic.
- Unit tests and edge-case tests (division by zero, overflow, precision, negative numbers) with xUnit.
- Code coverage tooling with Coverlet and ReportGenerator.
- GitHub Actions CI workflow that builds and tests on every push and pull request.
- Branch protection on `main`: pull requests and a passing CI check are required.
- NuGet package metadata and package README. Placeholder package `0.0.1-alpha` published to reserve the package ID.

[Unreleased]: https://github.com/PD0704/Prahelika.Calculators/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/PD0704/Prahelika.Calculators/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/PD0704/Prahelika.Calculators/compare/v0.3.1-alpha...v1.0.0
[0.3.1-alpha]: https://github.com/PD0704/Prahelika.Calculators/compare/v0.3.0-alpha...v0.3.1-alpha
[0.3.0-alpha]: https://github.com/PD0704/Prahelika.Calculators/compare/v0.2.0-alpha...v0.3.0-alpha
[0.2.0-alpha]: https://github.com/PD0704/Prahelika.Calculators/compare/v0.1.0-alpha...v0.2.0-alpha
[0.1.0-alpha]: https://github.com/PD0704/Prahelika.Calculators/releases/tag/v0.1.0-alpha