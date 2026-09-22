using System.Reflection;
using DMO.Application.Accounts;
using DMO.Application.Session;
using DMO.Web.Authorization;
using DMO.Web.Pages.Administration.Templates;
using Microsoft.AspNetCore.Authorization;

namespace DMO.UnitTests.TemplateAdministration;

/// <summary>
/// P1-T06 gate-reuse tests — the Template administration surface rides the exact same
/// ADMIN-only gate P1-T05 established (<see cref="AdministrationAuthorizationPolicies.PolicyName"/>,
/// <c>dmo.administration</c>). The shared handler's full behavior matrix (active ADMIN
/// allowed; USER/role-label Admin/operational Modules/no-session denied; single requirement;
/// scoped handler; outside the Module policy namespace) is already proven by the accepted
/// P1-T05 <c>AdminAuthorizationHandlerTests</c>; these tests assert the <b>new surface</b>
/// carries the same policy attribute and that the handler continues to gate it with the same
/// outcomes.
/// </summary>
public sealed class TemplateAdministrationAuthorizationTests
{
    [Fact]
    public void EveryTemplatePageModel_CarriesTheAdministrationPolicy()
    {
        var pageModels = new[]
        {
            typeof(ListModel),
            typeof(CreateModel),
            typeof(EditModel),
            typeof(DeleteModel),
        };

        foreach (var pageModel in pageModels)
        {
            var attribute = pageModel.GetCustomAttribute<AuthorizeAttribute>()
                ?? throw new Xunit.Sdk.XunitException(
                    $"{pageModel.Name} is missing the [Authorize] gate.");

            Assert.True(
                string.Equals(
                    AdministrationAuthorizationPolicies.PolicyName,
                    attribute.Policy,
                    StringComparison.Ordinal),
                $"{pageModel.Name} must carry exactly the dmo.administration policy.");
        }
    }

    [Fact]
    public void PolicyName_IsTheSharedAdministrationPolicy_NotAModulePolicy()
    {
        // The Template surface must reuse the P1-T05 gate verbatim: one administration policy
        // in the whole product, never a per-surfaces copy.
        Assert.Equal("dmo.administration", AdministrationAuthorizationPolicies.PolicyName);
        Assert.Equal(
            AdministrationAuthorizationPolicies.PolicyName,
            P1T05PolicyName());
    }

    [Fact]
    public async Task Handler_ActiveAdmin_Allowed_OnTheTemplateSurface()
    {
        // Same handler, same outcome: an active ADMIN session passes the gate that guards the
        // Template pages/endpoints.
        var handler = new AdminAuthorizationHandler(
            new DMO.UnitTests.Access.Fakes.FakeCurrentAccountContext(
                new CurrentAccount.Admin(new AdminAccount(Guid.NewGuid(), "DMO Admin", "admin@dmo.test", IsActive: true))));

        var context = await HandleAsync(handler);

        Assert.False(context.HasFailed);
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_User_Denied_OnTheTemplateSurface()
    {
        var handler = new AdminAuthorizationHandler(
            new DMO.UnitTests.Access.Fakes.FakeCurrentAccountContext(new CurrentAccount.User(User())));

        var context = await HandleAsync(handler);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_UserWithOperationalModules_Denied()
    {
        // Module availability is never an administration grant — a USER whose Template carries
        // operational Modules cannot reach Template administration.
        var handler = new AdminAuthorizationHandler(
            new DMO.UnitTests.Access.Fakes.FakeCurrentAccountContext(new CurrentAccount.User(User())));

        var context = await HandleAsync(handler);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_NoSession_Denied()
    {
        var handler = new AdminAuthorizationHandler(
            new DMO.UnitTests.Access.Fakes.FakeCurrentAccountContext(new CurrentAccount.None()));

        var context = await HandleAsync(handler);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    private static async Task<AuthorizationHandlerContext> HandleAsync(AdminAuthorizationHandler handler)
    {
        var context = new AuthorizationHandlerContext(
            new IAuthorizationRequirement[] { new AdminAuthorizationRequirement() },
            new System.Security.Claims.ClaimsPrincipal(),
            resource: null);

        await handler.HandleAsync(context);
        return context;
    }

    private static UserAccount User() => new(
        Guid.NewGuid(),
        CompanyNumber: "2661",
        DisplayName: "João Silva",
        Email: "joao@dmo.test",
        RoleLabel: "Reparador",
        IsActive: true,
        TemplateId: Guid.NewGuid(),
        Version: 1);

    /// <summary>Reads the P1-T05 policy name from the same constant (no string duplication).</summary>
    private static string P1T05PolicyName() => AdministrationAuthorizationPolicies.PolicyName;
}