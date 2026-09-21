using DMO.Application.Accounts;
using DMO.Application.Authentication;
using DMO.UnitTests.Accounts.Fakes;

namespace DMO.UnitTests.Accounts;

/// <summary>
/// P1-T02 tests — active/inactive/unknown/ambiguous resolution semantics against the
/// <b>real</b> <see cref="AccountResolver"/>. The fake lookup supplies data only.
/// </summary>
public sealed class AccountResolverTests
{
    private static readonly AdminAccount ActiveAdmin = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"), "DMO Admin", "admin@dmo.test", IsActive: true);

    private static readonly AdminAccount InactiveAdmin = ActiveAdmin with { IsActive = false };

    private static readonly UserAccount ActiveUser = new(
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        CompanyNumber: "2661",
        DisplayName: "João Silva",
        Email: "joao@dmo.test",
        RoleLabel: "Reparador",
        IsActive: true);

    private static readonly UserAccount InactiveUser = ActiveUser with { IsActive = false };

    private static readonly AuthenticatedIdentity AnyIdentity =
        new("provider-subject-1", AuthenticationPath.Admin);

    private static readonly AuthenticatedIdentity UserPathIdentity =
        new("provider-subject-user", AuthenticationPath.User);

    private static AccountResolver Resolver(params AccountMatch[] matches) =>
        new(new FakeAccountLookup(matches));

    [Fact]
    public async Task Resolve_SingleActiveAdmin_ReturnsAdmin()
    {
        var resolution = await Resolver(new AccountMatch.Admin(ActiveAdmin))
            .ResolveAsync(AnyIdentity, CancellationToken.None);

        var admin = Assert.IsType<AccountResolution.Admin>(resolution);
        Assert.Equal(ActiveAdmin.AccountId, admin.Account.AccountId);

        // ADMIN resolution output has no Template anywhere (type-shape, see also
        // AccountTypeAndResultShapeTests).
    }

    [Fact]
    public async Task Resolve_SingleActiveUser_ReturnsUser()
    {
        var resolution = await Resolver(new AccountMatch.User(ActiveUser))
            .ResolveAsync(UserPathIdentity, CancellationToken.None);

        var user = Assert.IsType<AccountResolution.User>(resolution);
        Assert.Equal("2661", user.Account.CompanyNumber);
        Assert.True(user.Account.IsActive);
    }

    [Fact]
    public async Task Resolve_NoMatches_FailsClosed()
    {
        var resolution = await Resolver()
            .ResolveAsync(AnyIdentity, CancellationToken.None);

        var noAccess = Assert.IsType<AccountResolution.NoAccess>(resolution);
        Assert.Equal(NoAccessReason.UnknownAccount, noAccess.Reason);
    }

    [Fact]
    public async Task Resolve_SingleInactiveAdmin_FailsClosed()
    {
        var resolution = await Resolver(new AccountMatch.Admin(InactiveAdmin))
            .ResolveAsync(AnyIdentity, CancellationToken.None);

        var noAccess = Assert.IsType<AccountResolution.NoAccess>(resolution);
        Assert.Equal(NoAccessReason.InactiveAdmin, noAccess.Reason);
    }

    [Fact]
    public async Task Resolve_SingleInactiveUser_FailsClosed()
    {
        var resolution = await Resolver(new AccountMatch.User(InactiveUser))
            .ResolveAsync(UserPathIdentity, CancellationToken.None);

        var noAccess = Assert.IsType<AccountResolution.NoAccess>(resolution);
        Assert.Equal(NoAccessReason.InactiveUser, noAccess.Reason);
    }

    [Fact]
    public async Task Resolve_AdminPathWithUserMatch_FailsClosed()
    {
        // Preconditions: ADMIN boundary path but the single lookup match is an active USER
        // account — the wrong account type for the path that established the identity.
        var resolution = await Resolver(new AccountMatch.User(ActiveUser))
            .ResolveAsync(AnyIdentity, CancellationToken.None);

        // Assertions: an active wrong-type account can never rescue the resolution.
        var noAccess = Assert.IsType<AccountResolution.NoAccess>(resolution);
        Assert.Equal(NoAccessReason.AuthenticationPathMismatch, noAccess.Reason);
    }

    [Fact]
    public async Task Resolve_UserPathWithAdminMatch_FailsClosed()
    {
        // Preconditions: USER boundary path but the single lookup match is an active ADMIN
        // account — the wrong account type for the path that established the identity.
        var resolution = await Resolver(new AccountMatch.Admin(ActiveAdmin))
            .ResolveAsync(UserPathIdentity, CancellationToken.None);

        // Assertions: an active wrong-type account can never rescue the resolution.
        var noAccess = Assert.IsType<AccountResolution.NoAccess>(resolution);
        Assert.Equal(NoAccessReason.AuthenticationPathMismatch, noAccess.Reason);
    }

    [Fact]
    public async Task Resolve_MultipleMatches_FailsClosed()
    {
        var resolution = await Resolver(
                new AccountMatch.User(ActiveUser),
                new AccountMatch.Admin(ActiveAdmin))
            .ResolveAsync(AnyIdentity, CancellationToken.None);

        var noAccess = Assert.IsType<AccountResolution.NoAccess>(resolution);
        Assert.Equal(NoAccessReason.AmbiguousMapping, noAccess.Reason);
    }

    [Fact]
    public async Task ProviderClaimsAlone_GrantNothing()
    {
        // Preconditions: the authenticated identity carries a provider subject (which in a
        // real provider world corresponds to provider claims/roles), but the application
        // lookup returns no mapping for it.
        var identity = new AuthenticatedIdentity("provider-subject-with-claims", AuthenticationPath.User);
        var lookup = new FakeAccountLookup(_ => []);
        var resolver = new AccountResolver(lookup);

        var resolution = await resolver.ResolveAsync(identity, CancellationToken.None);

        // Assertions: provider identity can never classify; absence of an application
        // mapping means fail closed.
        var noAccess = Assert.IsType<AccountResolution.NoAccess>(resolution);
        Assert.Equal(NoAccessReason.UnknownAccount, noAccess.Reason);
        Assert.Single(lookup.Lookups);
    }

    [Fact]
    public async Task Resolver_ReceivesTheAuthenticatedIdentity()
    {
        // Required non-effect: the resolver forwards the exact identity to the lookup
        // instead of fabricating its own linkage.
        var identity = new AuthenticatedIdentity("subject-42", AuthenticationPath.Admin);
        var lookup = new FakeAccountLookup(_ => []);
        var resolver = new AccountResolver(lookup);

        await resolver.ResolveAsync(identity, CancellationToken.None);

        Assert.Same(identity, Assert.Single(lookup.Lookups));
    }
}