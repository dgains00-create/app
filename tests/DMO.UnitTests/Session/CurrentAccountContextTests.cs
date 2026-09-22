using System.Reflection;
using DMO.Application.Accounts;
using DMO.Application.Authentication;
using DMO.Application.Session;
using DMO.UnitTests.Accounts.Fakes;
using DMO.Web.Auth;

namespace DMO.UnitTests.Session;

/// <summary>
/// P1-T02 tests — the <b>real</b> <see cref="CurrentAccountContext"/> with a stub session
/// identity and a fake lookup as data source.
/// </summary>
/// <remarks>
/// The context itself is the real production class. The stub only supplies the session
/// identity the context reads; resolution always runs through the real
/// <see cref="AccountResolver"/>.
/// </remarks>
public sealed class CurrentAccountContextTests
{
    private static readonly AdminAccount Admin = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"), "DMO Admin", "admin@dmo.test", IsActive: true);

    private static readonly UserAccount User = new(
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        CompanyNumber: "2661",
        DisplayName: "João Silva",
        Email: "joao@dmo.test",
        RoleLabel: "Reparador",
        IsActive: true);

    private static ICurrentAccountContext Context(
        ISessionAuthentication session,
        params AccountMatch[] matches) =>
        new CurrentAccountContext(session, new AccountResolver(new FakeAccountLookup(matches)));

    [Fact]
    public async Task CurrentAccount_NoSession_ReturnsNone()
    {
        var context = Context(new StubSession { Identity = null });

        var current = await context.GetCurrentAsync(CancellationToken.None);

        Assert.IsType<CurrentAccount.None>(current);
    }

    [Fact]
    public async Task CurrentAccount_ActiveAdminSession_ReturnsAdmin()
    {
        var session = new StubSession
        {
            Identity = new AuthenticatedIdentity("subject-1", AuthenticationPath.Admin),
        };

        var current = await Context(session, new AccountMatch.Admin(Admin)).GetCurrentAsync(CancellationToken.None);

        var admin = Assert.IsType<CurrentAccount.Admin>(current);
        Assert.Equal(Admin.AccountId, admin.Account.AccountId);
    }

    [Fact]
    public async Task CurrentAccount_ActiveUserSession_ReturnsUser()
    {
        var session = new StubSession
        {
            Identity = new AuthenticatedIdentity("subject-2", AuthenticationPath.User),
        };

        var current = await Context(session, new AccountMatch.User(User)).GetCurrentAsync(CancellationToken.None);

        var user = Assert.IsType<CurrentAccount.User>(current);
        Assert.Equal("2661", user.Account.CompanyNumber);
    }

    [Fact]
    public async Task CurrentAccount_ResolutionNoAccess_ReturnsNone()
    {
        // Preconditions: a session exists but the application lookup cannot resolve it
        // (fail-closed production posture when no persisted mapping exists).
        var session = new StubSession
        {
            Identity = new AuthenticatedIdentity("subject-3", AuthenticationPath.Admin),
        };

        var current = await Context(session).GetCurrentAsync(CancellationToken.None);

        // Assertions: an unresolvable session yields None (fail closed), never a granted
        // account and never an exception surface.
        Assert.IsType<CurrentAccount.None>(current);
    }

    [Fact]
    public void CurrentAccount_ReadOnly_HasNoSessionMutationMethods()
    {
        // Required non-effect: the current-account contract exposes exactly one read-only
        // method; session mutation stays in the runtime session element.
        var contract = typeof(ICurrentAccountContext);

        var declaredMethods = contract
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .ToArray();

        Assert.Equal(new[] { nameof(ICurrentAccountContext.GetCurrentAsync) }, declaredMethods);
    }

    /// <summary>Test-only stub session identity. Never used by production composition.</summary>
    private sealed class StubSession : ISessionAuthentication
    {
        public AuthenticatedIdentity? Identity { get; init; }

        public bool EstablishCalled { get; private set; }

        public bool SignOutCalled { get; private set; }

        public Task<AuthenticatedIdentity?> GetIdentityAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Identity);

        public Task EstablishAsync(AuthenticatedIdentity identity, CancellationToken cancellationToken)
        {
            EstablishCalled = true;
            return Task.CompletedTask;
        }

        public Task SignOutAsync(CancellationToken cancellationToken)
        {
            SignOutCalled = true;
            return Task.CompletedTask;
        }
    }
}