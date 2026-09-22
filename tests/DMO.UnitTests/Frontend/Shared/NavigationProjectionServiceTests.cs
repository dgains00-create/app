using DMO.Application.Access;
using DMO.Application.Accounts;
using DMO.Application.Session;
using DMO.Web.Frontend.Shell;

namespace DMO.UnitTests.Frontend.Shared;

public sealed class NavigationProjectionServiceTests
{
    [Fact]
    public async Task Project_GroupsSharedDestinationWithoutMergingCanonicalGrants()
    {
        var definitions = new[]
        {
            Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On"),
            Definition(ModuleCatalog.JobOnCreate, "Job On Create", "job-on", "Job On"),
        };
        var service = CreateService(definitions, definitions, new Dictionary<string, string>
        {
            ["job-on"] = "/implemented/job-on",
        });

        var result = await service.ProjectAsync(User("Operador"), CancellationToken.None);

        var destination = Assert.Single(result.LiveDestinations);
        Assert.Equal("job-on", destination.DestinationId);
        Assert.Equal("/implemented/job-on", destination.Href);
        Assert.Equal(
            new[] { ModuleCatalog.JobOnView, ModuleCatalog.JobOnCreate },
            destination.GrantedModuleIds);
    }

    [Fact]
    public async Task Project_ExcludesContextualUnavailableAndUnroutedModules()
    {
        var jobOn = Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        var controlo = Definition(ModuleCatalog.ControloCreate, "Controlo Create", "controlo", "Controlo");
        var ferramentas = Definition(ModuleCatalog.Ferramentas, "Ferramentas", null, "Ferramentas", contextual: true);
        var service = CreateService(
            available: [jobOn, controlo, ferramentas],
            granted: [jobOn, controlo, ferramentas],
            routes: new Dictionary<string, string> { ["job-on"] = "/implemented/job-on" });

        var result = await service.ProjectAsync(User("Responsável"), CancellationToken.None);

        var destination = Assert.Single(result.LiveDestinations);
        Assert.Equal("job-on", destination.DestinationId);
        Assert.DoesNotContain(result.LiveDestinations, item => item.DestinationId == "controlo");
        Assert.DoesNotContain(result.LiveDestinations, item => item.DestinationId == "ferramentas");
    }

    [Fact]
    public async Task Project_DoesNotUseRoleLabelToInfluenceAccess()
    {
        var jobOn = Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        var service = CreateService(
            [jobOn],
            [jobOn],
            new Dictionary<string, string> { ["job-on"] = "/implemented/job-on" });

        var first = await service.ProjectAsync(User("Operador"), CancellationToken.None);
        var second = await service.ProjectAsync(User("Qualquer texto livre"), CancellationToken.None);

        var firstDestination = Assert.Single(first.LiveDestinations);
        var secondDestination = Assert.Single(second.LiveDestinations);
        Assert.Equal(firstDestination.DestinationId, secondDestination.DestinationId);
        Assert.Equal(firstDestination.Href, secondDestination.Href);
        Assert.Equal(firstDestination.GrantedModuleIds, secondDestination.GrantedModuleIds);
    }

    [Fact]
    public async Task Project_UsesProvisionalNonRouteFixturesOnlyWhenBuildHasNoModules()
    {
        var service = CreateService([], [], new Dictionary<string, string>());

        var result = await service.ProjectAsync(User("Apresentação"), CancellationToken.None);

        Assert.Empty(result.LiveDestinations);
        Assert.NotEmpty(result.ProvisionalDestinations);
        Assert.DoesNotContain(result.ProvisionalDestinations, item => item.DestinationId == "ferramentas");
    }

    private static NavigationProjectionService CreateService(
        IReadOnlyList<ModuleDefinition> available,
        IReadOnlyList<ModuleDefinition> granted,
        IReadOnlyDictionary<string, string> routes) =>
        new(
            new FixedAccessService(new AccessOutcome.Granted(granted)),
            ModuleRegistry.Create(available),
            new DictionaryRouteRegistry(routes));

    private static CurrentAccount User(string roleLabel) => new CurrentAccount.User(
        new UserAccount(Guid.NewGuid(), "1042", "Maria Operadora", "maria@example.test", roleLabel, true, Guid.NewGuid(), 1));

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

        public Task<AccessOutcome> ResolveUserAccessAsync(AccountResolution resolution, CancellationToken cancellationToken) =>
            Task.FromResult(_outcome);

        public Task<bool> HasModuleAsync(AccountResolution resolution, ModuleId requiredModule, CancellationToken cancellationToken) =>
            Task.FromResult(_outcome is AccessOutcome.Granted(var modules) && modules.Any(module => module.Id == requiredModule));
    }

    private sealed class DictionaryRouteRegistry : IDestinationRouteRegistry
    {
        private readonly IReadOnlyDictionary<string, string> _routes;

        public DictionaryRouteRegistry(IReadOnlyDictionary<string, string> routes) => _routes = routes;

        public bool TryGetRoute(string destinationId, out string route) =>
            _routes.TryGetValue(destinationId, out route!);
    }
}
