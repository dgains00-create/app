using DMO.Application.Access;
using DMO.Application.Accounts;
using DMO.Application.Session;

namespace DMO.Web.Frontend.Shell;

/// <summary>
/// Projects P1-T04 effective access into visible destinations. This is presentation only;
/// route authorization remains the server-side Module gate.
/// </summary>
public sealed class NavigationProjectionService
{
    private static readonly IReadOnlyList<ProvisionalDestinationPresentation> A2Fixtures =
    [
        new("job-on", "Job On"),
        new("controlo", "Controlo"),
        new("reparacao-interna", "Reparação Interna"),
        new("boquilhas", "Boquilhas"),
        new("armazem", "Armazém"),
        new("reparacao-programada", "Reparação Programada"),
        new("tampoes", "Tampões"),
        new("historia", "História"),
    ];

    private readonly IModuleAccessService _access;
    private readonly IModuleRegistry _registry;
    private readonly IDestinationRouteRegistry _routes;

    public NavigationProjectionService(
        IModuleAccessService access,
        IModuleRegistry registry,
        IDestinationRouteRegistry routes)
    {
        _access = access;
        _registry = registry;
        _routes = routes;
    }

    public async Task<NavigationPresentation> ProjectAsync(
        CurrentAccount current,
        CancellationToken cancellationToken)
    {
        if (current is not CurrentAccount.User(var account))
        {
            return new NavigationPresentation([], ProvisionalFixturesWhenNeeded(), false);
        }

        var outcome = await _access.ResolveUserAccessAsync(
            new AccountResolution.User(account),
            cancellationToken);

        if (outcome is not AccessOutcome.Granted(var effectiveModules))
        {
            return new NavigationPresentation([], ProvisionalFixturesWhenNeeded(), true);
        }

        // Effective access is intersected with the published build registry. Contextual-only
        // Modules have no DestinationId and can never become top-level navigation.
        var live = effectiveModules
            .Where(module => _registry.IsAvailable(module.Id))
            .Where(module => !module.Surface.IsContextualOnly)
            .Where(module => !string.IsNullOrWhiteSpace(module.DestinationId))
            .GroupBy(module => module.DestinationId!, StringComparer.Ordinal)
            .Select(group => CreateDestination(group.Key, group.ToArray()))
            .Where(destination => destination is not null)
            .Cast<PrimaryDestinationPresentation>()
            .ToArray();

        return new NavigationPresentation(live, ProvisionalFixturesWhenNeeded(), false);
    }

    private PrimaryDestinationPresentation? CreateDestination(
        string destinationId,
        IReadOnlyList<ModuleDefinition> modules)
    {
        if (!_routes.TryGetRoute(destinationId, out var route)
            || string.IsNullOrWhiteSpace(route))
        {
            return null;
        }

        // Destination grouping preserves every canonical grant. It never replaces the
        // underlying Module identities with a destination-level permission.
        var label = modules[0].Surface.SurfaceName;
        return new PrimaryDestinationPresentation(
            destinationId,
            label,
            route,
            modules.Select(module => module.Id).ToArray());
    }

    private IReadOnlyList<ProvisionalDestinationPresentation> ProvisionalFixturesWhenNeeded() =>
        _registry.AvailableModules.Count == 0 ? A2Fixtures : [];
}
