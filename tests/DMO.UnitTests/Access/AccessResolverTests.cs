using DMO.Application.Access;
using DMO.Application.Accounts;
using DMO.Application.Templates;
using DMO.UnitTests.Access.Fakes;

namespace DMO.UnitTests.Access;

/// <summary>
/// P1-T04 access resolver tests — the fail-closed USER → Template → Modules resolution flow
/// over the real resolver with in-memory repository fakes and the controlled test registry.
/// </summary>
public sealed class AccessResolverTests
{
    [Fact]
    public async Task ActiveUser_ValidTemplate_ValidModules_ResolvesExpectedEffectiveModules()
    {
        var templateId = Guid.NewGuid();
        var templates = new FakeTemplateRepository();
        templates.Seed(Template(templateId));
        var modules = new FakeTemplateModuleRepository();
        modules.Seed(templateId, "controlo-create", "controlo-approve");

        var outcome = await Resolver(templates, modules).ResolveAccessAsync(
            new AccountResolution.User(User(templateId)), CancellationToken.None);

        var granted = Assert.IsType<AccessOutcome.Granted>(outcome);
        Assert.Equal(
            new[] { ModuleCatalog.ControloCreate, ModuleCatalog.ControloApprove },
            granted.EffectiveModules.Select(module => module.Id).ToArray());
    }

    [Fact]
    public async Task ActiveUser_NullTemplate_FailsClosed()
    {
        var outcome = await Resolver(new FakeTemplateRepository(), new FakeTemplateModuleRepository())
            .ResolveAccessAsync(new AccountResolution.User(User(templateId: null)), CancellationToken.None);

        var denied = Assert.IsType<AccessOutcome.Denied>(outcome);
        Assert.Equal(AccessDenialReason.NoTemplate, denied.Reason);
    }

    [Fact]
    public async Task MissingTemplateRow_FailsClosed()
    {
        var templateId = Guid.NewGuid();
        // No Template row seeded, even though a composition exists for that id.
        var modules = new FakeTemplateModuleRepository();
        modules.Seed(templateId, "controlo-create");

        var outcome = await Resolver(new FakeTemplateRepository(), modules)
            .ResolveAccessAsync(new AccountResolution.User(User(templateId)), CancellationToken.None);

        var denied = Assert.IsType<AccessOutcome.Denied>(outcome);
        Assert.Equal(AccessDenialReason.TemplateMissing, denied.Reason);
    }

    [Fact]
    public async Task UnknownPersistedModuleId_FailsClosedEntirely()
    {
        var templateId = Guid.NewGuid();
        var templates = new FakeTemplateRepository();
        templates.Seed(Template(templateId));
        var modules = new FakeTemplateModuleRepository();
        modules.Seed(templateId, "controlo-approve", "nao-existe");

        var outcome = await Resolver(templates, modules).ResolveAccessAsync(
            new AccountResolution.User(User(templateId)), CancellationToken.None);

        // Whole resolution denied: the valid sibling grants nothing.
        var denied = Assert.IsType<AccessOutcome.Denied>(outcome);
        Assert.Equal(AccessDenialReason.UnknownModule, denied.Reason);
    }

    [Fact]
    public async Task KnownButUnavailablePersistedModule_FailsClosedEntireResolution()
    {
        var templateId = Guid.NewGuid();
        var templates = new FakeTemplateRepository();
        templates.Seed(Template(templateId));
        var modules = new FakeTemplateModuleRepository();
        // One available Module + one canonical-but-unavailable Module (armazem is not in the
        // test availability list).
        modules.Seed(templateId, "controlo-create", "armazem");

        var outcome = await Resolver(templates, modules).ResolveAccessAsync(
            new AccountResolution.User(User(templateId)), CancellationToken.None);

        // NO partial grant: the available sibling is NOT granted.
        var denied = Assert.IsType<AccessOutcome.Denied>(outcome);
        Assert.Equal(AccessDenialReason.UnavailableModule, denied.Reason);
    }

    [Fact]
    public async Task RoleLabel_DoesNotChangeEffectiveModules()
    {
        var templateId = Guid.NewGuid();
        var templates = new FakeTemplateRepository();
        templates.Seed(Template(templateId));
        var modules = new FakeTemplateModuleRepository();
        modules.Seed(templateId, "controlo-create");
        var resolver = Resolver(templates, modules);

        var operador = await resolver.ResolveAccessAsync(
            new AccountResolution.User(User(templateId, role: "Operador")), CancellationToken.None);
        var responsavel = await resolver.ResolveAccessAsync(
            new AccountResolution.User(User(templateId, role: "Responsável")), CancellationToken.None);

        var grantedOperador = Assert.IsType<AccessOutcome.Granted>(operador);
        var grantedResponsavel = Assert.IsType<AccessOutcome.Granted>(responsavel);
        Assert.Equal(
            grantedOperador.EffectiveModules.Select(module => module.Id),
            grantedResponsavel.EffectiveModules.Select(module => module.Id));
    }

    [Fact]
    public async Task TemplateName_DoesNotChangeEffectiveModules()
    {
        var templateId = Guid.NewGuid();
        var modules = new FakeTemplateModuleRepository();
        modules.Seed(templateId, "controlo-approve");

        var templatesNamedOperador = new FakeTemplateRepository();
        templatesNamedOperador.Seed(Template(templateId, name: "Operador"));
        var outcomeOperador = await Resolver(templatesNamedOperador, modules).ResolveAccessAsync(
            new AccountResolution.User(User(templateId)), CancellationToken.None);

        var templatesNamedComercial = new FakeTemplateRepository();
        templatesNamedComercial.Seed(Template(templateId, name: "Comercial"));
        var outcomeComercial = await Resolver(templatesNamedComercial, modules).ResolveAccessAsync(
            new AccountResolution.User(User(templateId)), CancellationToken.None);

        var grantedOperador = Assert.IsType<AccessOutcome.Granted>(outcomeOperador);
        var grantedComercial = Assert.IsType<AccessOutcome.Granted>(outcomeComercial);
        Assert.Equal(
            grantedOperador.EffectiveModules.Select(module => module.Id),
            grantedComercial.EffectiveModules.Select(module => module.Id));

        // Sanity: the seeded Template rows really differ by name.
        Assert.Equal(
            "Operador",
            (await templatesNamedOperador.ListAsync(CancellationToken.None)).Single().Name);
        Assert.Equal(
            "Comercial",
            (await templatesNamedComercial.ListAsync(CancellationToken.None)).Single().Name);
    }

    [Fact]
    public async Task ProviderClaims_GrantNothing()
    {
        // The resolver input (AccountResolution) transports exactly the application account —
        // no provider claims exist in the model. Claim-like text carried in presentation
        // fields ("Controlo Approve" in role/company number/email/display name) changes
        // nothing and grants nothing.
        var templateId = Guid.NewGuid();
        var templates = new FakeTemplateRepository();
        templates.Seed(Template(templateId));
        var modules = new FakeTemplateModuleRepository();
        modules.Seed(templateId, "job-on-view"); // controlo-approve is NOT persisted
        var resolver = Resolver(templates, modules);

        var userWithClaimLikeText = User(templateId, role: "Controlo Approve") with
        {
            CompanyNumber = "controlo-approve",
            Email = "controlo-approve@provider.test",
            DisplayName = "Controlo Approve",
        };

        var outcome = await resolver.ResolveAccessAsync(
            new AccountResolution.User(userWithClaimLikeText), CancellationToken.None);

        var granted = Assert.IsType<AccessOutcome.Granted>(outcome);
        Assert.Equal(
            new[] { ModuleCatalog.JobOnView },
            granted.EffectiveModules.Select(module => module.Id).ToArray());

        // Identical to the plain resolution: claims change nothing.
        var plain = await resolver.ResolveAccessAsync(
            new AccountResolution.User(User(templateId)), CancellationToken.None);
        var grantedPlain = Assert.IsType<AccessOutcome.Granted>(plain);
        Assert.Equal(
            granted.EffectiveModules.Select(module => module.Id),
            grantedPlain.EffectiveModules.Select(module => module.Id));
    }

    [Fact]
    public async Task ADMIN_Resolution_FailsClosed()
    {
        var admin = new AdminAccount(Guid.NewGuid(), "DMO Admin", "admin@dmo.test", IsActive: true);

        var outcome = await Resolver(new FakeTemplateRepository(), new FakeTemplateModuleRepository())
            .ResolveAccessAsync(new AccountResolution.Admin(admin), CancellationToken.None);

        var denied = Assert.IsType<AccessOutcome.Denied>(outcome);
        Assert.Equal(AccessDenialReason.NotOperationalUser, denied.Reason);
    }

    [Fact]
    public async Task NoAccessResolution_FailsClosed()
    {
        var outcome = await Resolver(new FakeTemplateRepository(), new FakeTemplateModuleRepository())
            .ResolveAccessAsync(new AccountResolution.NoAccess(NoAccessReason.UnknownAccount), CancellationToken.None);

        var denied = Assert.IsType<AccessOutcome.Denied>(outcome);
        Assert.Equal(AccessDenialReason.NotOperationalUser, denied.Reason);
    }

    [Fact]
    public async Task RepositoryFailure_FailsClosed()
    {
        var templateId = Guid.NewGuid();

        // Template repository failure → denied, never an exception surfaced as a grant.
        var failingTemplates = new FakeTemplateRepository { Failure = new InvalidOperationException("db down") };
        var deniedTemplates = Assert.IsType<AccessOutcome.Denied>(
            await Resolver(failingTemplates, new FakeTemplateModuleRepository())
                .ResolveAccessAsync(new AccountResolution.User(User(templateId)), CancellationToken.None));
        Assert.Equal(AccessDenialReason.ResolutionFailure, deniedTemplates.Reason);

        // Template-modules repository failure → denied.
        var templates = new FakeTemplateRepository();
        templates.Seed(Template(templateId));
        var failingModules = new FakeTemplateModuleRepository { Failure = new InvalidOperationException("db down") };
        var deniedModules = Assert.IsType<AccessOutcome.Denied>(
            await Resolver(templates, failingModules)
                .ResolveAccessAsync(new AccountResolution.User(User(templateId)), CancellationToken.None));
        Assert.Equal(AccessDenialReason.ResolutionFailure, deniedModules.Reason);
    }

    [Fact]
    public async Task EffectiveOrder_FollowsPersistedPresentationOrder()
    {
        var templateId = Guid.NewGuid();
        var templates = new FakeTemplateRepository();
        templates.Seed(Template(templateId));
        var modules = new FakeTemplateModuleRepository();
        // Persisted order deliberately differs from canonical-id order.
        modules.Seed(templateId, "controlo-approve", "job-on-create", "controlo-create");

        var outcome = await Resolver(templates, modules).ResolveAccessAsync(
            new AccountResolution.User(User(templateId)), CancellationToken.None);

        var granted = Assert.IsType<AccessOutcome.Granted>(outcome);
        Assert.Equal(
            new[] { ModuleCatalog.ControloApprove, ModuleCatalog.JobOnCreate, ModuleCatalog.ControloCreate },
            granted.EffectiveModules.Select(module => module.Id).ToArray());
    }

    [Fact]
    public async Task EmptyTemplateComposition_ResolvesGrantedWithNoModules()
    {
        var templateId = Guid.NewGuid();
        var templates = new FakeTemplateRepository();
        templates.Seed(Template(templateId));

        var outcome = await Resolver(templates, new FakeTemplateModuleRepository())
            .ResolveAccessAsync(new AccountResolution.User(User(templateId)), CancellationToken.None);

        // Fully interpreted and valid composition without Modules → valid empty grant;
        // per-Module enforcement then denies everything in practice.
        var granted = Assert.IsType<AccessOutcome.Granted>(outcome);
        Assert.Empty(granted.EffectiveModules);
    }

    // ---- P1-T07 additive fact: Granted carries the persisted Template landing destination id.

    [Fact]
    public async Task Granted_CarriesPersistedTemplateLandingDestinationId()
    {
        var templateId = Guid.NewGuid();
        var templates = new FakeTemplateRepository();
        templates.Seed(Template(templateId, landing: "controlo"));
        var modules = new FakeTemplateModuleRepository();
        modules.Seed(templateId, "controlo-create");

        var outcome = await Resolver(templates, modules).ResolveAccessAsync(
            new AccountResolution.User(User(templateId)), CancellationToken.None);

        // The landing id is the persisted Template fact carried through the existing read —
        // it is a routing fact with no authorization meaning of its own.
        var granted = Assert.IsType<AccessOutcome.Granted>(outcome);
        Assert.Equal("controlo", granted.LandingDestinationId);
        Assert.Equal(
            new[] { ModuleCatalog.ControloCreate },
            granted.EffectiveModules.Select(module => module.Id).ToArray());
    }

    [Fact]
    public async Task Granted_NullLanding_WhenTemplateHasNoLanding()
    {
        var templateId = Guid.NewGuid();
        var templates = new FakeTemplateRepository();
        templates.Seed(Template(templateId, landing: null));
        var modules = new FakeTemplateModuleRepository();
        modules.Seed(templateId, "controlo-create");

        var outcome = await Resolver(templates, modules).ResolveAccessAsync(
            new AccountResolution.User(User(templateId)), CancellationToken.None);

        var granted = Assert.IsType<AccessOutcome.Granted>(outcome);
        Assert.Null(granted.LandingDestinationId);
    }

    [Fact]
    public async Task Denied_EntireResolution_CarriesNoLandingFact()
    {
        // A denied resolution (here: unknown persisted Module) is whole-resolution denial;
        // no landing fact survives it.
        var templateId = Guid.NewGuid();
        var templates = new FakeTemplateRepository();
        templates.Seed(Template(templateId, landing: "controlo"));
        var modules = new FakeTemplateModuleRepository();
        modules.Seed(templateId, "controlo-create", "nao-existe");

        var outcome = await Resolver(templates, modules).ResolveAccessAsync(
            new AccountResolution.User(User(templateId)), CancellationToken.None);

        var denied = Assert.IsType<AccessOutcome.Denied>(outcome);
        Assert.Equal(AccessDenialReason.UnknownModule, denied.Reason);
    }

    private static UserAccount User(Guid? templateId, string role = "Job") => new(
        Guid.NewGuid(),
        CompanyNumber: "2661",
        DisplayName: "João Silva",
        Email: "joao@dmo.test",
        RoleLabel: role,
        IsActive: true,
        TemplateId: templateId,
        Version: 1);

    private static Template Template(Guid templateId, string name = "Template", string? landing = null) =>
        new(templateId, name, LandingDestinationId: landing, Version: 1);

    private static IAccessResolver Resolver(
        FakeTemplateRepository templates,
        FakeTemplateModuleRepository modules) =>
        new AccessResolver(TestModuleDefinitions.TestRegistry(), templates, modules);
}