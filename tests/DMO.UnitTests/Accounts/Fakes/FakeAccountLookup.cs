using DMO.Application.Accounts;
using DMO.Application.Authentication;

namespace DMO.UnitTests.Accounts.Fakes;

/// <summary>
/// Test-only account lookup that supplies configurable <see cref="AccountMatch"/> lists.
/// </summary>
/// <remarks>
/// Used as a data source only. All active/inactive/unknown/ambiguous semantics are exercised
/// against the real <see cref="AccountResolver"/>; this fake never classifies anything.
/// Referenced only from test code, never by production composition.
/// </remarks>
public sealed class FakeAccountLookup : IAccountLookup
{
    private readonly Func<AuthenticatedIdentity, IReadOnlyList<AccountMatch>> _matches;
    private readonly List<AuthenticatedIdentity> _lookups = [];

    /// <summary>Creates a lookup returning the same matches for every identity.</summary>
    public FakeAccountLookup(params AccountMatch[] matches)
    {
        _matches = _ => matches;
    }

    /// <summary>Creates a lookup whose matches are derived from the identity.</summary>
    public FakeAccountLookup(Func<AuthenticatedIdentity, IReadOnlyList<AccountMatch>> matches)
    {
        ArgumentNullException.ThrowIfNull(matches);
        _matches = matches;
    }

    /// <summary>Identities observed by this lookup, in order.</summary>
    public IReadOnlyList<AuthenticatedIdentity> Lookups => _lookups;

    /// <inheritdoc />
    public Task<IReadOnlyList<AccountMatch>> LookupAsync(
        AuthenticatedIdentity identity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identity);
        _lookups.Add(identity);
        return Task.FromResult(_matches(identity));
    }
}