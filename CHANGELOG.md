# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [0.1.0-alpha] - 2026-11-12

### Added
- `CalcResult<T>` and `CalcError`: calculations return a result object instead of throwing exceptions.
- `IBasicCalculator` and `BasicCalculator`: add, subtract, multiply, divide, modulo and percentage, using `decimal` arithmetic.
- Unit tests and edge-case tests (division by zero, overflow, precision, negative numbers) with xUnit.
- Code coverage tooling with Coverlet and ReportGenerator.
- GitHub Actions CI workflow that builds and tests on every push and pull request.
- Branch protection on `main`: pull requests and a passing CI check are required.
- NuGet package metadata and package README. Placeholder package `0.0.1-alpha` published to reserve the package ID.

[Unreleased]: https://github.com/PD0704/Prahelika.Calculators/compare/v0.1.0-alpha...HEAD
[0.1.0-alpha]: https://github.com/PD0704/Prahelika.Calculators/releases/tag/v0.1.0-alpha