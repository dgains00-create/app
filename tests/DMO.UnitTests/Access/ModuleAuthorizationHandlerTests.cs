using System.Security.Claims;
using DMO.Application.Access;
using DMO.Application.Accounts;
using DMO.Application.Session;
using DMO.Application.Templates;
using DMO.UnitTests.Access.Fakes;
using DMO.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace DMO.UnitTests.Access;

/// <summary>
/// P1-T04 tests — the thin ASP.NET authorization projection: one policy per canonical Module,
/// handler resolves effective access through the Module access service, ADMIN/no-session fail
/// (no super-user bypass), and no independent permission catalogue exists.
/// </summary>
public sealed class ModuleAuthorizationHandlerTests
{
    [Fact]
    public async Task Handler_AdminSession_Fails()
    {
        var admin = new AdminAccount(Guid.NewGuid(), "DMO Admin", "admin@dmo.test", IsActive: true);
        var handler = Handler(
            current: new CurrentAccount.Admin(admin),
            templates: new FakeTemplateRepository(),
            modules: new FakeTemplateModuleRepository());

        var context = await HandleAsync(handler, ModuleCatalog.ControloApprove);

        // ADMIN has no implicit operational Module access: fail closed.
        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_NoSession_Fails()
    {
        var handler = Handler(
            current: new CurrentAccount.None(),
            templates: new FakeTemplateRepository(),
            modules: new FakeTemplateModuleRepository());

        var context = await HandleAsync(handler, ModuleCatalog.JobOnView);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_UserWithRequiredModule_Succeeds()
    {
        var templateId = Guid.NewGuid();
        var templates = new FakeTemplateRepository();
        templates.Seed(new Template(templateId, "Template", null, Version: 1));
        var modules = new FakeTemplateModuleRepository();
        modules.Seed(templateId, "controlo-approve");
        var handler = Handler(
            current: new CurrentAccount.User(User(templateId)),
            templates: templates,
            modules: modules);

        var context = await HandleAsync(handler, ModuleCatalog.ControloApprove);

        Assert.False(context.HasFailed);
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_UserWithoutRequiredModule_Fails()
    {
        var templateId = Guid.NewGuid();
        var templates = new FakeTemplateRepository();
        templates.Seed(new Template(templateId, "Template", null, Version: 1));
        var modules = new FakeTemplateModuleRepository();
        modules.Seed(templateId, "controlo-create");
        var handler = Handler(
            current: new CurrentAccount.User(User(templateId)),
            templates: templates,
            modules: modules);

        // Direct route/action requires controlo-approve; the USER only has controlo-create.
        // Shared destination does not satisfy the requirement.
        var context = await HandleAsync(handler, ModuleCatalog.ControloApprove);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Policies_OnePerCanonicalModule_PolicyNameDerivedFromModuleId()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddModuleAuthorization();

        await using var provider = services.BuildServiceProvider();

        // Exactly one policy per canonical Module, each carrying exactly one
        // ModuleAuthorizationRequirement for that Module id.
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        foreach (var entry in ModuleCatalog.All)
        {
            var expectedPolicyName = $"dmo.module.{entry.Id.Value}";
            Assert.Equal(expectedPolicyName, ModuleAuthorizationPolicies.PolicyName(entry.Id));

            var policy = await policyProvider.GetPolicyAsync(expectedPolicyName);
            Assert.NotNull(policy);

            var requirement = Assert.Single(policy!.Requirements, item => item is ModuleAuthorizationRequirement);
            Assert.Equal(entry.Id, Assert.IsType<ModuleAuthorizationRequirement>(requirement).RequiredModule);
        }

        // A non-canonical policy name resolves to nothing: policies are exactly the catalog.
        Assert.Null(await policyProvider.GetPolicyAsync("dmo.module.nao-existe"));

        // The handler is registered (scoped, because it consumes scoped services) as exactly
        // one IAuthorizationHandler; it is activated per request within the real composition.
        var handlerDescriptor = Assert.Single(
            services.Where(descriptor => descriptor.ServiceType == typeof(IAuthorizationHandler)),
            descriptor => descriptor.ImplementationType == typeof(ModuleAuthorizationHandler));
        Assert.Equal(ServiceLifetime.Scoped, handlerDescriptor.Lifetime);
    }

    private static ModuleAuthorizationHandler Handler(
        CurrentAccount current,
        FakeTemplateRepository templates,
        FakeTemplateModuleRepository modules) =>
        new(
            new FakeCurrentAccountContext(current),
            new ModuleAccessService(new AccessResolver(TestModuleDefinitions.TestRegistry(), templates, modules)));

    private static async Task<AuthorizationHandlerContext> HandleAsync(
        ModuleAuthorizationHandler handler,
        ModuleId requiredModule)
    {
        var requirement = new ModuleAuthorizationRequirement(requiredModule);
        var context = new AuthorizationHandlerContext(
            new IAuthorizationRequirement[] { requirement },
            new ClaimsPrincipal(),
            resource: null);

        await handler.HandleAsync(context);

        return context;
    }

    private static UserAccount User(Guid templateId, string role = "Job") => new(
        Guid.NewGuid(),
        CompanyNumber: "2661",
        DisplayName: "João Silva",
        Email: "joao@dmo.test",
        RoleLabel: role,
        IsActive: true,
        TemplateId: templateId,
        Version: 1);
}