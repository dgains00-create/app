using DMO.Application.Session;

namespace DMO.UnitTests.Access.Fakes;

/// <summary>
/// Test-only <see cref="ICurrentAccountContext"/> whose current account is set directly.
/// </summary>
public sealed class FakeCurrentAccountContext : ICurrentAccountContext
{
    /// <summary>Creates the context over the given current account.</summary>
    public FakeCurrentAccountContext(CurrentAccount current)
    {
        Current = current;
    }

    /// <summary>The account returned as current.</summary>
    public CurrentAccount Current { get; set; }

    /// <inheritdoc />
    public Task<CurrentAccount> GetCurrentAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Current);
}