using DMO.Application.Access;
using DMO.Application.Accounts;
using DMO.Application.Session;
using DMO.Web.Frontend.Shell;

namespace DMO.UnitTests.Frontend.Shared;

/// <summary>
/// A2 correction: production navigation is derived only from
/// granted effective Modules ∩ published available definitions ∩ non-contextual Modules with a
/// DestinationId ∩ actually registered destination route. No provisional/fixture carrier exists
/// in production, and no operational destination is invented when nothing is live.
/// </summary>
public sealed class NavigationProjectionServiceTests
{
    [Fact]
    public async Task Project_EmptyRegistry_ReturnsZeroDestinationsAndNoProvisionalCarrier()
    {
        var access = new FixedAccessService(Granted());
        var service = CreateService([], access);

        var result = await service.ProjectAsync(User("Operador"), CancellationToken.None);

        Assert.Empty(result.LiveDestinations);
        Assert.False(result.AccessResolutionFailed);

        // The production presentation contract carries no provisional/fixture state at all:
        // no ProvisionalDestinations member and no provisional-typed member.
        var presentationProperties = typeof(NavigationPresentation).GetProperties();
        Assert.DoesNotContain(presentationProperties, property => property.Name == "ProvisionalDestinations");
        Assert.DoesNotContain(
            presentationProperties,
            property => property.PropertyType.Name.Contains("Provisional", StringComparison.Ordinal));

        // No provisional/fixture destination type survives anywhere in the production
        // frontend assembly (covers ProvisionalDestinationPresentation and any successor).
        var provisionalTypes = typeof(NavigationPresentation).Assembly
            .GetTypes()
            .Where(type => type.Name.Contains("Provisional", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(provisionalTypes);

        // The empty registry is a normal granted outcome, not an access failure.
        Assert.Equal(1, access.ResolveCallCount);
    }

    [Fact]
    public async Task Project_Admin_ReturnsZeroOperationalDestinations()
    {
        // The registry offers a real definition and a route exists, so an ADMIN that wrongly
        // became operational would visibly render a destination.
        var jobOn = Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        var access = new FixedAccessService(Granted(jobOn));
        var service = CreateService([jobOn], access, new Dictionary<string, string>
        {
            ["job-on"] = "/implemented/job-on",
        });

        var result = await service.ProjectAsync(Admin(), CancellationToken.None);

        Assert.Empty(result.LiveDestinations);
        Assert.False(result.AccessResolutionFailed);
    }

    [Fact]
    public async Task Project_Admin_DoesNotInvokeUserAccessResolution()
    {
        var access = new FixedAccessService(Granted(), failsWhenInvoked: true);
        var service = CreateService([], access);

        var result = await service.ProjectAsync(Admin(), CancellationToken.None);

        Assert.Empty(result.LiveDestinations);
        Assert.False(result.AccessResolutionFailed);
        Assert.Equal(0, access.ResolveCallCount);
    }

    [Fact]
    public async Task Project_DeniedUser_ReturnsZeroDestinationsAndAccessResolutionFailure()
    {
        var access = new FixedAccessService(new AccessOutcome.Denied(AccessDenialReason.NoTemplate));
        var service = CreateService([], access);

        var result = await service.ProjectAsync(User("Operador"), CancellationToken.None);

        Assert.Empty(result.LiveDestinations);
        Assert.True(result.AccessResolutionFailed);
    }

    [Fact]
    public async Task Project_GrantedUserWithNoAvailableDefinition_ReturnsZeroDestinationsWithoutFailure()
    {
        // A granted resolution whose definitions never became available is not an access
        // failure: it is zero live destinations, distinct from the denied state above.
        var jobOn = Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        var access = new FixedAccessService(Granted(jobOn));
        var service = CreateService([], access, new Dictionary<string, string>
        {
            ["job-on"] = "/implemented/job-on",
        });

        var result = await service.ProjectAsync(User("Operador"), CancellationToken.None);

        Assert.Empty(result.LiveDestinations);
        Assert.False(result.AccessResolutionFailed);
    }

    [Fact]
    public async Task Project_ContextualModule_IsExcludedFromTopLevelNavigation()
    {
        var ferramentas = Definition(ModuleCatalog.Ferramentas, "Ferramentas", null, "Ferramentas", contextual: true);
        var service = CreateService(
            [ferramentas],
            new FixedAccessService(Granted(ferramentas)),
            // A route is deliberately offered so exclusion is proven by the contextual
            // predicate, not merely by the absence of a route.
            new Dictionary<string, string> { ["ferramentas"] = "/implemented/ferramentas" });

        var result = await service.ProjectAsync(User("Responsável"), CancellationToken.None);

        Assert.Empty(result.LiveDestinations);
        Assert.DoesNotContain(result.LiveDestinations, item => item.DestinationId == "ferramentas");
        Assert.DoesNotContain(result.LiveDestinations, item => item.Label == "Ferramentas");
    }

    [Fact]
    public async Task Project_UnavailableModule_IsExcludedFromTopLevelNavigation()
    {
        var grantedButUnavailable = Definition(ModuleCatalog.ControloCreate, "Controlo Create", "controlo", "Controlo");
        var service = CreateService(
            available: [],
            new FixedAccessService(Granted(grantedButUnavailable)),
            // The route exists, so exclusion is proven by published availability only.
            new Dictionary<string, string> { ["controlo"] = "/implemented/controlo" });

        var result = await service.ProjectAsync(User("Responsável"), CancellationToken.None);

        Assert.Empty(result.LiveDestinations);
        Assert.DoesNotContain(result.LiveDestinations, item => item.DestinationId == "controlo");
    }

    [Fact]
    public async Task Project_UnroutedModule_IsExcludedFromTopLevelNavigation()
    {
        var jobOn = Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        var service = CreateService([jobOn], new FixedAccessService(Granted(jobOn)));

        var result = await service.ProjectAsync(User("Responsável"), CancellationToken.None);

        Assert.Empty(result.LiveDestinations);
        Assert.DoesNotContain(result.LiveDestinations, item => item.DestinationId == "job-on");
    }

    [Fact]
    public async Task Project_SharedDestination_GroupsOneVisibleDestinationAndRetainsCanonicalGrants()
    {
        var jobOnView = Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        var jobOnCreate = Definition(ModuleCatalog.JobOnCreate, "Job On Create", "job-on", "Job On");
        var service = CreateService(
            [jobOnView, jobOnCreate],
            new FixedAccessService(Granted(jobOnView, jobOnCreate)),
            new Dictionary<string, string> { ["job-on"] = "/implemented/job-on" });

        var result = await service.ProjectAsync(User("Operador"), CancellationToken.None);

        var destination = Assert.Single(result.LiveDestinations);
        Assert.Equal("job-on", destination.DestinationId);
        Assert.Equal("/implemented/job-on", destination.Href);
        Assert.Equal(
            new[] { ModuleCatalog.JobOnView, ModuleCatalog.JobOnCreate },
            destination.GrantedModuleIds);
        Assert.Equal(2, destination.GrantedModuleIds.Count);
    }

    [Fact]
    public async Task Project_DoesNotUseRoleLabelToInfluenceAccess()
    {
        var jobOn = Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        var service = CreateService(
            [jobOn],
            new FixedAccessService(Granted(jobOn)),
            new Dictionary<string, string> { ["job-on"] = "/implemented/job-on" });

        var first = await service.ProjectAsync(User("Operador"), CancellationToken.None);
        var second = await service.ProjectAsync(User("Qualquer texto livre"), CancellationToken.None);

        var firstDestination = Assert.Single(first.LiveDestinations);
        var secondDestination = Assert.Single(second.LiveDestinations);
        Assert.Equal(firstDestination.DestinationId, secondDestination.DestinationId);
        Assert.Equal(firstDestination.Href, secondDestination.Href);
        Assert.Equal(firstDestination.GrantedModuleIds, secondDestination.GrantedModuleIds);
    }

    // ---- P1-T07 additive propagation: the persisted landing destination id is carried as a
    // routing fact; the projection algorithm itself is unchanged.

    [Fact]
    public async Task Project_GrantedUser_PropagatesPersistedLandingDestinationId()
    {
        var jobOn = Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        var service = CreateService(
            [jobOn],
            new FixedAccessService(GrantedWithLanding("job-on", jobOn)),
            new Dictionary<string, string> { ["job-on"] = "/implemented/job-on" });

        var result = await service.ProjectAsync(User("Operador"), CancellationToken.None);

        Assert.False(result.AccessResolutionFailed);
        Assert.Equal("job-on", result.LandingDestinationId);

        // The live projection is exactly the pre-existing behavior.
        var destination = Assert.Single(result.LiveDestinations);
        Assert.Equal("job-on", destination.DestinationId);
        Assert.Equal("/implemented/job-on", destination.Href);
        Assert.Equal(new[] { ModuleCatalog.JobOnView }, destination.GrantedModuleIds);
    }

    [Fact]
    public async Task Project_GrantedUser_NullLanding_PropagatesNull()
    {
        var jobOn = Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        var service = CreateService(
            [jobOn],
            new FixedAccessService(GrantedWithLanding(null, jobOn)),
            new Dictionary<string, string> { ["job-on"] = "/implemented/job-on" });

        var result = await service.ProjectAsync(User("Operador"), CancellationToken.None);

        Assert.Null(result.LandingDestinationId);
        Assert.Single(result.LiveDestinations);
    }

    [Fact]
    public async Task Project_DeniedUser_PropagatesNoLandingFact()
    {
        var jobOn = Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        var service = CreateService(
            [jobOn],
            new FixedAccessService(new AccessOutcome.Denied(AccessDenialReason.NoTemplate)),
            new Dictionary<string, string> { ["job-on"] = "/implemented/job-on" });

        var result = await service.ProjectAsync(User("Operador"), CancellationToken.None);

        Assert.True(result.AccessResolutionFailed);
        Assert.Empty(result.LiveDestinations);
        Assert.Null(result.LandingDestinationId);
    }

    [Fact]
    public async Task Project_Admin_PropagatesNoLandingFact()
    {
        var jobOn = Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        var access = new FixedAccessService(GrantedWithLanding("job-on", jobOn), failsWhenInvoked: true);
        var service = CreateService(
            [jobOn],
            access,
            new Dictionary<string, string> { ["job-on"] = "/implemented/job-on" });

        var result = await service.ProjectAsync(Admin(), CancellationToken.None);

        Assert.False(result.AccessResolutionFailed);
        Assert.Empty(result.LiveDestinations);
        Assert.Null(result.LandingDestinationId);
        Assert.Equal(0, access.ResolveCallCount);
    }

    private static NavigationProjectionService CreateService(
        IReadOnlyList<ModuleDefinition> available,
        FixedAccessService access,
        IReadOnlyDictionary<string, string>? routes = null) =>
        new(access, ModuleRegistry.Create(available), new DictionaryRouteRegistry(routes ?? new Dictionary<string, string>()));

    private static AccessOutcome Granted(params ModuleDefinition[] modules) =>
        new AccessOutcome.Granted(null, modules);

    private static AccessOutcome GrantedWithLanding(string? landing, params ModuleDefinition[] modules) =>
        new AccessOutcome.Granted(landing, modules);

    private static CurrentAccount User(string roleLabel) => new CurrentAccount.User(
        new UserAccount(Guid.NewGuid(), "1042", "Maria Operadora", "maria@example.test", roleLabel, true, Guid.NewGuid(), 1));

    private static CurrentAccount Admin() =>
        new CurrentAccount.Admin(new AdminAccount(Guid.NewGuid(), "Ana Administradora", "admin@example.test", true));

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
        private readonly bool _failsWhenInvoked;

        public FixedAccessService(AccessOutcome outcome, bool failsWhenInvoked = false)
        {
            _outcome = outcome;
            _failsWhenInvoked = failsWhenInvoked;
        }

        public int ResolveCallCount { get; private set; }

        public Task<AccessOutcome> ResolveUserAccessAsync(AccountResolution resolution, CancellationToken cancellationToken)
        {
            ResolveCallCount++;
            ThrowWhenInvocationIsForbidden();
            return Task.FromResult(_outcome);
        }

        public Task<bool> HasModuleAsync(AccountResolution resolution, ModuleId requiredModule, CancellationToken cancellationToken)
        {
            ThrowWhenInvocationIsForbidden();
            return Task.FromResult(_outcome is AccessOutcome.Granted(_, var modules) && modules.Any(module => module.Id == requiredModule));
        }

        private void ThrowWhenInvocationIsForbidden()
        {
            if (_failsWhenInvoked)
            {
                throw new InvalidOperationException(
                    "Operational USER access resolution must not be invoked for a non-USER account.");
            }
        }
    }

    private sealed class DictionaryRouteRegistry : IDestinationRouteRegistry
    {
        private readonly IReadOnlyDictionary<string, string> _routes;

        public DictionaryRouteRegistry(IReadOnlyDictionary<string, string> routes) => _routes = routes;

        public bool TryGetRoute(string destinationId, out string route) =>
            _routes.TryGetValue(destinationId, out route!);
    }
}
