using DMO.Application.Access;
using DMO.Application.Accounts;
using DMO.Application.Session;
using DMO.Web.Frontend.Shell;
using DMO.Web.Navigation;

namespace DMO.UnitTests.Navigation;

/// <summary>
/// P1-T07 runtime USER landing — the real <see cref="NavigationProjectionService"/> (fixed
/// access outcome, controlled test registry, dictionary route registry) feeding the real
/// <see cref="UserLandingService"/>. The service consumes the projection exactly once and
/// never resolves access itself.
/// </summary>
public sealed class UserLandingServiceTests
{
    [Fact]
    public async Task Resolve_DeniedAccess_FailsClosedToNoAccess()
    {
        var access = new FixedAccessService(new AccessOutcome.Denied(AccessDenialReason.NoTemplate));
        var service = CreateService([], access);

        var landing = await service.ResolveAsync(User(), CancellationToken.None);

        Assert.IsType<UserLanding.NoAccess>(landing);
        // The projection was consumed exactly once.
        Assert.Equal(1, access.ResolveCallCount);
    }

    [Fact]
    public async Task Resolve_ExplicitValidLanding_RedirectsToTheRegisteredRoute()
    {
        var jobOn = Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        var access = new FixedAccessService(GrantedWithLanding("job-on", jobOn));
        var service = CreateService([jobOn], access, new Dictionary<string, string>
        {
            ["job-on"] = "/implemented/job-on",
        });

        var landing = await service.ResolveAsync(User(), CancellationToken.None);

        var redirect = Assert.IsType<UserLanding.RedirectTo>(landing);
        Assert.Equal("/implemented/job-on", redirect.Route);
        Assert.Equal(1, access.ResolveCallCount);
    }

    [Fact]
    public async Task Resolve_NullExplicitLanding_RedirectsToFirstDestinationInOrder()
    {
        var controlo = Definition(ModuleCatalog.ControloCreate, "Controlo Create", "controlo", "Controlo");
        var jobOn = Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        var access = new FixedAccessService(GrantedWithLanding(null, controlo, jobOn));
        var service = CreateService([controlo, jobOn], access, new Dictionary<string, string>
        {
            ["controlo"] = "/implemented/controlo",
            ["job-on"] = "/implemented/job-on",
        });

        var landing = await service.ResolveAsync(User(), CancellationToken.None);

        var redirect = Assert.IsType<UserLanding.RedirectTo>(landing);
        Assert.Equal("/implemented/controlo", redirect.Route);
    }

    [Fact]
    public async Task Resolve_GrantedWithNoRoutableDestinations_ReturnsNoAccess()
    {
        var jobOn = Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        var access = new FixedAccessService(GrantedWithLanding(null, jobOn));
        // Available definition but no registered route: live projection is empty.
        var service = CreateService([jobOn], access);

        var landing = await service.ResolveAsync(User(), CancellationToken.None);

        Assert.IsType<UserLanding.NoAccess>(landing);
    }

    [Fact]
    public async Task Resolve_ContextualOnlyComposition_ReturnsNoAccess()
    {
        var ferramentas = Definition(ModuleCatalog.Ferramentas, "Ferramentas", null, "Ferramentas", contextual: true);
        var access = new FixedAccessService(GrantedWithLanding(null, ferramentas));
        // A route is deliberately offered; contextual-only exclusion makes it invisible.
        var service = CreateService([ferramentas], access, new Dictionary<string, string>
        {
            ["ferramentas"] = "/implemented/ferramentas",
        });

        var landing = await service.ResolveAsync(User(), CancellationToken.None);

        Assert.IsType<UserLanding.NoAccess>(landing);
    }

    [Fact]
    public async Task Resolve_InvalidExplicitLanding_FailsClosedToNoAccess_NeverFirstValid()
    {
        // Architect decision (§31.1 settled): an explicit persisted landing that is not
        // represented among the valid composed/routed destinations is FAIL CLOSED. It must
        // not fall back to the first valid destination.
        var controlo = Definition(ModuleCatalog.ControloCreate, "Controlo Create", "controlo", "Controlo");
        var jobOn = Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        var access = new FixedAccessService(GrantedWithLanding("nao-existe", controlo, jobOn));
        var service = CreateService([controlo, jobOn], access, new Dictionary<string, string>
        {
            ["controlo"] = "/implemented/controlo",
            ["job-on"] = "/implemented/job-on",
        });

        var landing = await service.ResolveAsync(User(), CancellationToken.None);

        Assert.IsType<UserLanding.NoAccess>(landing);
        Assert.Equal(1, access.ResolveCallCount);
    }

    [Fact]
    public async Task Resolve_SharedDestinationLanding_RedirectsToCollapsedRoute()
    {
        var jobOnView = Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        var jobOnCreate = Definition(ModuleCatalog.JobOnCreate, "Job On Create", "job-on", "Job On");
        var access = new FixedAccessService(GrantedWithLanding("job-on", jobOnView, jobOnCreate));
        var service = CreateService([jobOnView, jobOnCreate], access, new Dictionary<string, string>
        {
            ["job-on"] = "/implemented/job-on",
        });

        var landing = await service.ResolveAsync(User(), CancellationToken.None);

        var redirect = Assert.IsType<UserLanding.RedirectTo>(landing);
        Assert.Equal("/implemented/job-on", redirect.Route);
    }

    [Fact]
    public async Task Resolve_AdminIsNeverPassedToTheService_UserRequirementIsStructural()
    {
        // The service contract is CurrentAccount.User; ADMIN routing is handled by the root
        // router before this service is ever called (proven at the HTTP boundary). Here we
        // prove the service never fabricates a landing for a non-granted state.
        var access = new FixedAccessService(GrantedWithLanding(null));
        var service = CreateService([], access);

        var landing = await service.ResolveAsync(User(), CancellationToken.None);

        Assert.IsType<UserLanding.NoAccess>(landing);
    }

    private static UserLandingService CreateService(
        IReadOnlyList<ModuleDefinition> available,
        FixedAccessService access,
        IReadOnlyDictionary<string, string>? routes = null) =>
        new(new NavigationProjectionService(
            access,
            ModuleRegistry.Create(available),
            new DictionaryRouteRegistry(routes ?? new Dictionary<string, string>())));

    private static AccessOutcome GrantedWithLanding(string? landing, params ModuleDefinition[] modules) =>
        new AccessOutcome.Granted(landing, modules);

    private static CurrentAccount.User User() => new(new UserAccount(
        Guid.NewGuid(), "1042", "Maria Operadora", "maria@example.test", "Turno A", true, Guid.NewGuid(), 1));

    private static ModuleDefinition Definition(
        ModuleId id,
        string displayName,
        string? destinationId,
        string surfaceName,
        bool contextual = false) =>
        new(id, displayName, destinationId, !contextual, new ModuleSurfaceDescriptor(
            surfaceName,
            destinationId,
            contextual,
            []));

    private sealed class FixedAccessService : IModuleAccessService
    {
        private readonly AccessOutcome _outcome;

        public FixedAccessService(AccessOutcome outcome) => _outcome = outcome;

        public int ResolveCallCount { get; private set; }

        public Task<AccessOutcome> ResolveUserAccessAsync(AccountResolution resolution, CancellationToken cancellationToken)
        {
            ResolveCallCount++;
            return Task.FromResult(_outcome);
        }

        public Task<bool> HasModuleAsync(AccountResolution resolution, ModuleId requiredModule, CancellationToken cancellationToken) =>
            Task.FromResult(_outcome is AccessOutcome.Granted(_, var modules) && modules.Any(module => module.Id == requiredModule));
    }

    private sealed class DictionaryRouteRegistry : IDestinationRouteRegistry
    {
        private readonly IReadOnlyDictionary<string, string> _routes;

        public DictionaryRouteRegistry(IReadOnlyDictionary<string, string> routes) => _routes = routes;

        public bool TryGetRoute(string destinationId, out string route) =>
            _routes.TryGetValue(destinationId, out route!);
    }
}