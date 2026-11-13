# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- `FinanceCalculator` with simple interest and compound interest (annual, semi-annual, quarterly, monthly, weekly or daily compounding), using `decimal` arithmetic and rounding to 2 decimal places by default.
- Loan calculations: `Emi` (monthly payment, total payment and total interest) and `AmortizationSchedule` (month-by-month payment, principal, interest and balance), for loans of up to 1200 months.
- `SipFutureValue`: the future value of a monthly investment (SIP) with an expected annual return.
- `ReturnOnInvestment`: the gain and the ROI percentage.
- `Gst`: GST added to a price (exclusive) or taken out of one (inclusive).

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

[Unreleased]: https://github.com/PD0704/Prahelika.Calculators/compare/v0.2.0-alpha...HEAD
[0.2.0-alpha]: https://github.com/PD0704/Prahelika.Calculators/compare/v0.1.0-alpha...v0.2.0-alpha
[0.1.0-alpha]: https://github.com/PD0704/Prahelika.Calculators/releases/tag/v0.1.0-alpha