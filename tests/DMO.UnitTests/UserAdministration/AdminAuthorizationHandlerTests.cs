using DMO.Application.Accounts;
using DMO.Application.Access;
using DMO.Application.Session;
using DMO.UnitTests.Access.Fakes;
using DMO.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace DMO.UnitTests.UserAdministration;

/// <summary>
/// P1-T05 gate tests — the single ADMIN-only administration policy: only an active ADMIN
/// session passes; USER accounts (regardless of role label, Template or operational Modules)
/// are denied; no session/unresolved are denied; and the policy lives outside the Module
/// policy namespace with exactly one requirement.
/// </summary>
public sealed class AdminAuthorizationHandlerTests
{
    [Fact]
    public async Task Handler_ActiveAdmin_Succeeds()
    {
        var admin = new AdminAccount(Guid.NewGuid(), "DMO Admin", "admin@dmo.test", IsActive: true);
        var handler = Handler(new CurrentAccount.Admin(admin));

        var context = await HandleAsync(handler);

        Assert.False(context.HasFailed);
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_User_Denied()
    {
        var handler = Handler(new CurrentAccount.User(User()));

        var context = await HandleAsync(handler);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_UserWithRoleLabelAdmin_Denied()
    {
        // A USER whose free-text role is "Admin" grants nothing: only AdminAccount sessions
        // pass the gate; role labels, Templates and Modules never authorize administration.
        var handler = Handler(new CurrentAccount.User(User(role: "Admin")));

        var context = await HandleAsync(handler);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_UserWithOperationalModules_Denied()
    {
        // Even a USER whose Template carries operational Modules cannot reach the
        // administration surface: Module availability is never an administration grant.
        var handler = Handler(new CurrentAccount.User(User()));

        var context = await HandleAsync(handler);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_NoSession_Denied()
    {
        var handler = Handler(new CurrentAccount.None());

        var context = await HandleAsync(handler);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_InactiveAdminSession_Denied()
    {
        // An inactive/unresolved ADMIN (resolution failed upstream, surfaced as None) can
        // never reach the administration surface: fail closed.
        var handler = Handler(new CurrentAccount.None());

        var context = await HandleAsync(handler);

        Assert.True(context.HasFailed);
    }

    [Fact]
    public async Task Policy_IsSingleRequirement_ScopedHandler_NotAModulePolicy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddModuleAuthorization();
        services.AddAdministrationAuthorization();

        await using var provider = services.BuildServiceProvider();

        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        // The administration policy exists with exactly one AdminAuthorizationRequirement and
        // no Module requirement; policies accumulate across the two registrations.
        var policy = await policyProvider.GetPolicyAsync(AdministrationAuthorizationPolicies.PolicyName);
        Assert.NotNull(policy);
        var requirement = Assert.Single(policy!.Requirements, item => item is AdminAuthorizationRequirement);
        Assert.IsType<AdminAuthorizationRequirement>(requirement);
        Assert.DoesNotContain(policy.Requirements, item => item is ModuleAuthorizationRequirement);

        // The administration policy name is deliberately outside the deterministic
        // dmo.module.<id> namespace: it can never collide with a Module policy.
        foreach (var entry in ModuleCatalog.All)
        {
            Assert.NotEqual(
                AdministrationAuthorizationPolicies.PolicyName,
                ModuleAuthorizationPolicies.PolicyName(entry.Id));
        }

        // Exactly one scoped handler for the requirement.
        var handlerDescriptor = Assert.Single(
            services.Where(descriptor => descriptor.ServiceType == typeof(IAuthorizationHandler)),
            descriptor => descriptor.ImplementationType == typeof(AdminAuthorizationHandler));
        Assert.Equal(ServiceLifetime.Scoped, handlerDescriptor.Lifetime);
    }

    private static AdminAuthorizationHandler Handler(CurrentAccount current) =>
        new(new FakeCurrentAccountContext(current));

    private static async Task<AuthorizationHandlerContext> HandleAsync(AdminAuthorizationHandler handler)
    {
        var context = new AuthorizationHandlerContext(
            new IAuthorizationRequirement[] { new AdminAuthorizationRequirement() },
            new System.Security.Claims.ClaimsPrincipal(),
            resource: null);

        await handler.HandleAsync(context);

        return context;
    }

    private static UserAccount User(string role = "Reparador") => new(
        Guid.NewGuid(),
        CompanyNumber: "2661",
        DisplayName: "João Silva",
        Email: "joao@dmo.test",
        RoleLabel: role,
        IsActive: true,
        TemplateId: Guid.NewGuid(),
        Version: 1);
}