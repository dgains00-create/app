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
    public void AccountResolution_HasNoTemplateAccessSemantics()
    {
        // Required non-effect: no resolution/account type introduces Template, Module or
        // access fields — a Template/access outcome is structurally impossible from these
        // types (verified over the public surface of every P1-T02 account type).

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
                name.Contains("Template", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Module", StringComparison.OrdinalIgnoreCase)
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
    public void UserAccount_HasNoTemplateState()
    {
        // Required non-effect: the USER account record carries no Template reference and no
        // Template/access state in P1-T02.
        var properties = typeof(UserAccount)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[] { "AccountId", "CompanyNumber", "DisplayName", "Email", "IsActive", "RoleLabel" },
            properties);
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