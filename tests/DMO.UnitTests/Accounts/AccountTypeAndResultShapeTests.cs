using System.Reflection;
using DMO.Application.Accounts;
using DMO.Application.Authentication;
using DMO.Application.Session;

namespace DMO.UnitTests.Accounts;

/// <summary>
/// P1-T02 tests — the account/result type shapes carry no Template/Module/access semantics,
/// and NoAccess/None are states, never account types.
/// </summary>
public sealed class AccountTypeAndResultShapeTests
{
    [Fact]
    public void NoAccess_IsResolutionState_NotAccountType()
    {
        // Preconditions: the account type enum and the resolution union.
        var accountTypes = Enum.GetValues<AccountType>();

        // Assertions: exactly two account types; NoAccess/None cannot be account types.
        Assert.Equal(new[] { AccountType.Admin, AccountType.User }, accountTypes);
        Assert.Equal(2, accountTypes.Length);
        Assert.DoesNotContain(accountTypes, type => type.ToString() is "NoAccess" or "None");
    }

    [Fact]
    public void AccountResolution_HasNoModuleOrAccessSemantics()
    {
        // Required non-effect: no account/result type introduces Module, Access, Permission
        // or Capability members. Template appears ONLY as the nullable UserAccount.TemplateId
        // persistence fact (P1-T03); it grants nothing and is checked separately.
        Type[] accountTypes =
        [
            typeof(AdminAccount),
            typeof(UserAccount),
            typeof(AccountResolution),
            typeof(AccountMatch),
            typeof(CurrentAccount),
        ];

        foreach (var type in accountTypes)
        {
            var memberNames = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => property.Name)
                .Concat(
                    type.GetNestedTypes(BindingFlags.Public)
                        .SelectMany(nested => nested.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        .Select(property => property.Name))
                .ToArray();

            Assert.DoesNotContain(memberNames, name =>
                name.Contains("Module", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Access", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Permission", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Capability", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void AdminAccount_HasNoTemplateField()
    {
        // Required non-effect: the single ADMIN account is Template-free by shape.
        var properties = typeof(AdminAccount)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "AccountId", "DisplayName", "Email", "IsActive" }, properties);
    }

    [Fact]
    public void UserAccount_CarriesOnlyTheNullableTemplateIdFact()
    {
        // P1-T03: the USER account record carries exactly the nullable TemplateId persistence
        // fact (grants nothing, no resolver branch reads it) plus the pessimistic-concurrency
        // Version observed at read time (P1-T03 correction: mirror of Template.Version); no
        // Module/access state exists.
        var properties = typeof(UserAccount)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "AccountId", "CompanyNumber", "DisplayName", "Email", "IsActive", "RoleLabel", "TemplateId", "Version",
            },
            properties);

        // The only Template-shaped member on the USER model is the nullable TemplateId.
        var templateMembers = typeof(UserAccount)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .Where(name => name.Contains("Template", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.Equal(new[] { "TemplateId" }, templateMembers);
    }

    [Fact]
    public void AuthenticatedIdentity_HasOnlyProviderSubjectAndPath()
    {
        // Required non-effect: no provider claims/roles/tokens can leak into the identity.
        var properties = typeof(AuthenticatedIdentity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "AuthenticationPath", "ProviderSubject" }, properties);
    }
}