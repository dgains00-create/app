using DMO.Application.Access;
using DMO.Application.Accounts;
using DMO.Application.Templates;
using DMO.UnitTests.Access.Fakes;

namespace DMO.UnitTests.Access;

/// <summary>
/// P1-T04 Module access service tests — the reusable route/action gate core
/// (<see cref="IModuleAccessService"/>): per-Module checks are derived from a fully valid
/// effective set, and no partial access survives a denied resolution.
/// </summary>
public sealed class ModuleAccessServiceTests
{
    [Fact]
    public async Task RequiredModulePresent_Allows()
    {
        var (service, templateId) = ServiceWith("controlo-create");

        Assert.True(await service.HasModuleAsync(User(templateId), ModuleCatalog.ControloCreate, CancellationToken.None));
    }

    [Fact]
    public async Task RequiredModuleAbsent_Denies()
    {
        var (service, templateId) = ServiceWith("controlo-create");
        var user = User(templateId);

        Assert.False(await service.HasModuleAsync(user, ModuleCatalog.ControloApprove, CancellationToken.None));
        Assert.False(await service.HasModuleAsync(user, ModuleCatalog.JobOnView, CancellationToken.None));
    }

    [Fact]
    public async Task SiblingOnSharedDestination_Denies()
    {
        // controlo-create is granted; the sibling controlo-approve on the same visible
        // destination is NOT granted (shared destinations never merge Module rights).
        var (service, templateId) = ServiceWith("controlo-create");
        var user = User(templateId);

        Assert.True(await service.HasModuleAsync(user, ModuleCatalog.ControloCreate, CancellationToken.None));
        Assert.False(await service.HasModuleAsync(user, ModuleCatalog.ControloApprove, CancellationToken.None));
    }

    [Fact]
    public async Task Admin_Denies()
    {
        var (service, _) = ServiceWith("controlo-create");
        var admin = new AdminAccount(Guid.NewGuid(), "DMO Admin", "admin@dmo.test", IsActive: true);

        var resolution = await service.ResolveUserAccessAsync(new AccountResolution.Admin(admin), CancellationToken.None);
        var denied = Assert.IsType<AccessOutcome.Denied>(resolution);
        Assert.Equal(AccessDenialReason.NotOperationalUser, denied.Reason);

        Assert.False(await service.HasModuleAsync(new AccountResolution.Admin(admin), ModuleCatalog.ControloCreate, CancellationToken.None));
        Assert.False(await service.HasModuleAsync(new AccountResolution.Admin(admin), ModuleCatalog.ControloApprove, CancellationToken.None));
    }

    [Fact]
    public async Task UserWithBothCreateAndApprove_MaySatisfyBothGates()
    {
        // Create and Approve are independent Modules, not mutually exclusive user types.
        var (service, templateId) = ServiceWith("controlo-create", "controlo-approve");
        var user = User(templateId);

        Assert.True(await service.HasModuleAsync(user, ModuleCatalog.ControloCreate, CancellationToken.None));
        Assert.True(await service.HasModuleAsync(user, ModuleCatalog.ControloApprove, CancellationToken.None));
    }

    [Fact]
    public async Task UnavailableModule_DeniesService_EvenWhenAvailableSiblingPersisted()
    {
        // Template composition: controlo-create (available) + armazem (known but not
        // registered in the test availability list).
        var templateId = Guid.NewGuid();
        var templates = new FakeTemplateRepository();
        templates.Seed(new Template(templateId, "Template", null, Version: 1));
        var modules = new FakeTemplateModuleRepository();
        modules.Seed(templateId, "controlo-create", "armazem");
        var service = new ModuleAccessService(
            new AccessResolver(TestModuleDefinitions.TestRegistry(), templates, modules));
        var user = User(templateId);

        var resolution = await service.ResolveUserAccessAsync(user, CancellationToken.None);
        var denied = Assert.IsType<AccessOutcome.Denied>(resolution);
        Assert.Equal(AccessDenialReason.UnavailableModule, denied.Reason);

        // The available sibling must NOT survive the denied resolution: every gate returns
        // false — no partial access.
        Assert.False(await service.HasModuleAsync(user, ModuleCatalog.ControloCreate, CancellationToken.None));
        Assert.False(await service.HasModuleAsync(user, ModuleCatalog.Armazem, CancellationToken.None));
        Assert.False(await service.HasModuleAsync(user, ModuleCatalog.ControloApprove, CancellationToken.None));
    }

    private static (IModuleAccessService Service, Guid TemplateId) ServiceWith(params string[] moduleIds)
    {
        var templateId = Guid.NewGuid();
        var templates = new FakeTemplateRepository();
        templates.Seed(new Template(templateId, "Template", null, Version: 1));
        var modules = new FakeTemplateModuleRepository();
        modules.Seed(templateId, moduleIds);
        return (new ModuleAccessService(
            new AccessResolver(TestModuleDefinitions.TestRegistry(), templates, modules)), templateId);
    }

    private static AccountResolution User(Guid templateId, string role = "Job") =>
        new AccountResolution.User(new UserAccount(
            Guid.NewGuid(),
            CompanyNumber: "2661",
            DisplayName: "João Silva",
            Email: "joao@dmo.test",
            RoleLabel: role,
            IsActive: true,
            TemplateId: templateId,
            Version: 1));
}