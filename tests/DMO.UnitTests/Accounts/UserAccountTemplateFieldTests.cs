using DMO.Application.Accounts;
using DMO.Application.Authentication;
using DMO.Application.Session;
using DMO.UnitTests.Accounts.Fakes;

namespace DMO.UnitTests.Accounts;

/// <summary>
/// P1-T03 tests — <see cref="UserAccount.TemplateId"/> is a nullable persisted fact that
/// grants nothing.
/// </summary>
/// <remarks>
/// The field is carried through resolution so the account record reflects the persisted
/// <c>users.template_id</c> state. No resolver branch reads it: Template access resolution is
/// P1-T04.
/// </remarks>
public sealed class UserAccountTemplateFieldTests
{
    private static readonly Guid TemplateId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static readonly UserAccount WithTemplate = new(
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        CompanyNumber: "2661",
        DisplayName: "João Silva",
        Email: "joao@dmo.test",
        RoleLabel: "Reparador",
        IsActive: true,
        TemplateId: TemplateId);

    [Fact]
    public void TemplateId_IsNullable_AndDefaultsToNull()
    {
        var account = new UserAccount(
            Guid.NewGuid(), "2661", "João Silva", "joao@dmo.test", "Reparador", IsActive: true);

        Assert.Null(account.TemplateId);
    }

    [Fact]
    public void UserAccount_CanCarryTemplateId()
    {
        Assert.Equal(TemplateId, WithTemplate.TemplateId);
    }

    [Fact]
    public async Task AccountMatchUser_CarriesTemplateId()
    {
        var match = new AccountMatch.User(WithTemplate);
        var matchUser = Assert.IsType<AccountMatch.User>(match);

        Assert.Equal(TemplateId, matchUser.Account.TemplateId);
    }

    [Fact]
    public async Task ResolutionUser_CarriesTemplateId_WithoutGrantingAnything()
    {
        // Preconditions: an active USER whose persisted record carries a Template id.
        var resolver = new AccountResolver(new FakeAccountLookup(new AccountMatch.User(WithTemplate)));

        // Action: resolve with the User path.
        var resolution = await resolver.ResolveAsync(
            new AuthenticatedIdentity("subject-1", AuthenticationPath.User),
            CancellationToken.None);

        // Assertions: resolution succeeds exactly as without a Template (the field is a
        // carried fact), and the Template id is preserved on the result.
        var resolved = Assert.IsType<AccountResolution.User>(resolution);
        Assert.Equal(TemplateId, resolved.Account.TemplateId);
        Assert.True(resolved.Account.IsActive);
    }

    [Fact]
    public async Task CurrentAccountUser_CarriesTemplateId()
    {
        // The Template id is a persistence-visible fact on the session's current account too.
        var current = new CurrentAccount.User(WithTemplate);
        var user = Assert.IsType<CurrentAccount.User>(current);

        Assert.Equal(TemplateId, user.Account.TemplateId);
    }
}