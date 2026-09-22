using DMO.Application.Access;
using DMO.Application.Accounts;
using DMO.Application.Templates;
using DMO.UnitTests.Access.Fakes;

namespace DMO.UnitTests.Access;

/// <summary>
/// P1-T04 mandatory regression — fixed profile labels (Operador / Responsável / …) have no
/// authorization meaning. Access comes exclusively from the Modules selected in the USER's
/// effective Template (request §17 / corrected plan §15).
/// </summary>
public sealed class NoProfilesRegressionTests
{
    [Fact]
    public async Task ProfileLabels_NeverGrant()
    {
        // USER A: role "Operador", Template = [Controlo Approve]
        var templateA = Guid.NewGuid();
        var serviceA = Service(
            templates: SeedTemplate(templateA, name: "Operador"),
            modules: SeedModules(templateA, "controlo-approve"));

        Assert.True(await serviceA.HasModuleAsync(new AccountResolution.User(User(templateA, role: "Operador")), ModuleCatalog.ControloApprove, CancellationToken.None));
        Assert.False(await serviceA.HasModuleAsync(new AccountResolution.User(User(templateA, role: "Operador")), ModuleCatalog.ControloCreate, CancellationToken.None));

        // USER B: role "Responsável", Template = [Controlo Create]
        var templateB = Guid.NewGuid();
        var serviceB = Service(
            templates: SeedTemplate(templateB, name: "Responsável"),
            modules: SeedModules(templateB, "controlo-create"));

        Assert.True(await serviceB.HasModuleAsync(new AccountResolution.User(User(templateB, role: "Responsável")), ModuleCatalog.ControloCreate, CancellationToken.None));
        Assert.False(await serviceB.HasModuleAsync(new AccountResolution.User(User(templateB, role: "Responsável")), ModuleCatalog.ControloApprove, CancellationToken.None));

        // USER C: Template = [Controlo Create, Controlo Approve] — both are legitimate.
        var templateC = Guid.NewGuid();
        var serviceC = Service(
            templates: SeedTemplate(templateC, name: "Controlador"),
            modules: SeedModules(templateC, "controlo-create", "controlo-approve"));

        Assert.True(await serviceC.HasModuleAsync(new AccountResolution.User(User(templateC, role: "Controlador")), ModuleCatalog.ControloCreate, CancellationToken.None));
        Assert.True(await serviceC.HasModuleAsync(new AccountResolution.User(User(templateC, role: "Controlador")), ModuleCatalog.ControloApprove, CancellationToken.None));
    }

    [Fact]
    public async Task SiblingModule_SameDestination_DoesNotSatisfyGate()
    {
        var templateId = Guid.NewGuid();
        var service = Service(
            templates: SeedTemplate(templateId, name: "Template"),
            modules: SeedModules(templateId, "job-on-view", "controlo-create"));
        var user = new AccountResolution.User(User(templateId, role: "Reparador"));

        // Shared destination job-on: view does not satisfy create.
        Assert.True(await service.HasModuleAsync(user, ModuleCatalog.JobOnView, CancellationToken.None));
        Assert.False(await service.HasModuleAsync(user, ModuleCatalog.JobOnCreate, CancellationToken.None));

        // Shared destination controlo: create does not satisfy approve.
        Assert.True(await service.HasModuleAsync(user, ModuleCatalog.ControloCreate, CancellationToken.None));
        Assert.False(await service.HasModuleAsync(user, ModuleCatalog.ControloApprove, CancellationToken.None));
    }

    private static FakeTemplateRepository SeedTemplate(Guid templateId, string name)
    {
        var templates = new FakeTemplateRepository();
        templates.Seed(new Template(templateId, name, LandingDestinationId: null, Version: 1));
        return templates;
    }

    private static FakeTemplateModuleRepository SeedModules(Guid templateId, params string[] moduleIds)
    {
        var modules = new FakeTemplateModuleRepository();
        modules.Seed(templateId, moduleIds);
        return modules;
    }

    private static UserAccount User(Guid templateId, string role) => new(
        Guid.NewGuid(),
        CompanyNumber: "2661",
        DisplayName: "João Silva",
        Email: "joao@dmo.test",
        RoleLabel: role,
        IsActive: true,
        TemplateId: templateId,
        Version: 1);

    private static IModuleAccessService Service(
        FakeTemplateRepository templates,
        FakeTemplateModuleRepository modules) =>
        new ModuleAccessService(new AccessResolver(TestModuleDefinitions.TestRegistry(), templates, modules));
}