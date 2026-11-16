# Contributing

Thank you for your interest in Prahelika.Calculators. Bug reports, ideas and pull requests are welcome.

## Ways to help

- **Report a bug:** open an issue using the *Bug report* form.
- **Suggest a feature or a new calculator:** open an issue using the *Feature request* form.
- **Fix something or add something:** open a pull request (see below). For a big change, open an issue first so we can agree on the approach.

## Set up

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and Visual Studio 2022 or later (or any editor).

```
git clone https://github.com/PD0704/Prahelika.Calculators.git
cd Prahelika.Calculators
dotnet build Prahelika.Calculators/Prahelika.Calculators.slnx
dotnet test Prahelika.Calculators/Prahelika.Calculators.slnx
```

All tests should pass before you change anything.

## Making a change

1. **Fork** the repository, or create a branch if you have access.
2. Create a branch from `main` with a short name, such as `feature/loan-comparison` or `fix/modulo-negative`.
3. Make your change, and **add or update tests** for it. A bug fix should come with a test that fails without the fix.
4. Run `dotnet test` and make sure everything passes.
5. Add a line to the `[Unreleased]` section of `CHANGELOG.md` if the change is visible to people who use the library, API or website.
6. Open a pull request against `main`.

`main` is protected. Changes go in through a pull request, and the **build-and-test** check must pass before merging.

## Commit messages

Use a short prefix that says what kind of change it is:

- `feat:` a new feature
- `fix:` a bug fix
- `test:` tests only
- `docs:` documentation only
- `build:` or `ci:` build and pipeline changes
- `chore:` anything else, such as moving files

Example: `fix: return an error for tan of 90 degrees`

## Code guidelines

- **User mistakes return a result; they don't throw.** Bad input (division by zero, invalid values, overflow) returns a failed `CalcResult<T>` with a `CalcError` and a message. Exceptions are only for mistakes in the calling code, such as an invalid constructor argument.
- **Money and basic arithmetic use `decimal`; scientific functions use `double`.**
- Keep the three layers separate: calculation logic in the Core library; the API and the website only call it.
- Add `///` XML comments to public types and methods.
- Warnings are treated as errors, so the build must be clean.

## Tests

- Tests use xUnit, in the `Prahelika.Calculators.*.Tests` projects.
- Use known-answer values you have checked yourself, and test the edge cases (zero, negative numbers, very large values).
- For `double` results, compare with a tolerance, not exact equality.

## Questions

Open an issue and describe what you are trying to do.