using DMO.Application.Accounts;
using DMO.Application.Authentication;
using DMO.Web.Resolution;

namespace DMO.UnitTests.Accounts;

/// <summary>
/// P1-T02 test — the production P1-T02 lookup returns no matches, so the real resolver
/// always fails closed until P1-T03 supplies the persistence-backed lookup.
/// </summary>
public sealed class UnavailableAccountLookupTests
{
    [Fact]
    public async Task UnavailableAccountLookup_ReturnsNoMatches()
    {
        // Preconditions: any authenticated identity (even one that "would" map once a
        // persisted store exists).
        var identity = new AuthenticatedIdentity("any-subject", AuthenticationPath.Admin);
        var lookup = new UnavailableAccountLookup();
        var resolver = new AccountResolver(lookup);

        // Action: request the candidate matches, then resolve.
        var matches = await lookup.LookupAsync(identity, CancellationToken.None);
        var resolution = await resolver.ResolveAsync(identity, CancellationToken.None);

        // Assertions: no matches exist in P1-T02, and the real resolver therefore fails
        // closed with exactly the honest reason — no schema is required for this to hold.
        Assert.Empty(matches);
        var noAccess = Assert.IsType<AccountResolution.NoAccess>(resolution);
        Assert.Equal(NoAccessReason.UnknownAccount, noAccess.Reason);
    }
}