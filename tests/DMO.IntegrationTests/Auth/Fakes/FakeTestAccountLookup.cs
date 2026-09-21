using DMO.Application.Accounts;
using DMO.Application.Authentication;

namespace DMO.IntegrationTests.Auth.Fakes;

/// <summary>
/// Test-host-only account lookup supplying configurable matches for the auth endpoint
/// plumbing tests.
/// </summary>
/// <remarks>
/// Registered only through <c>ConfigureTestServices</c> inside the test host; production
/// composition keeps <see cref="DMO.Web.Resolution.UnavailableAccountLookup"/>. This fake is
/// a data source only — the real resolver still performs all classification.
/// </remarks>
public sealed class FakeTestAccountLookup : IAccountLookup
{
    private readonly IReadOnlyList<AccountMatch> _matches;

    /// <summary>Creates a lookup returning the supplied matches.</summary>
    public FakeTestAccountLookup(params AccountMatch[] matches)
    {
        _matches = matches;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AccountMatch>> LookupAsync(
        AuthenticatedIdentity identity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identity);
        return Task.FromResult(_matches);
    }
}