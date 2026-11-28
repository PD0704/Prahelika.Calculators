namespace Prahelika.Calculators.Web.Tests;

/// <summary>A clock that always says the same time, so tests give the same answer on any day.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}