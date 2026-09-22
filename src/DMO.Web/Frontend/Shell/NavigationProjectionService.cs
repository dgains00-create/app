using DMO.Application.Access;
using DMO.Application.Accounts;
using DMO.Application.Session;

namespace DMO.Web.Frontend.Shell;

/// <summary>
/// Projects P1-T04 effective access into visible destinations. This is presentation only;
/// route authorization remains the server-side Module gate.
/// </summary>
/// <remarks>
/// The projection has exactly one source: effective granted Modules ∩ published available
/// Module definitions ∩ non-contextual Modules carrying a <c>DestinationId</c> ∩ Modules whose
/// destination route is actually registered. Nothing is invented when that intersection is
/// empty — the shell then renders its operational empty state.
/// </remarks>
public sealed class NavigationProjectionService
{
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
        // ADMIN (like any non-operational account) has no operational Template and therefore no
        // operational destination. USER access resolution is never invoked for it.
        if (current is not CurrentAccount.User(var account))
        {
            return new NavigationPresentation([], false);
        }

        var outcome = await _access.ResolveUserAccessAsync(
            new AccountResolution.User(account),
            cancellationToken);

        // A denied resolution fails closed: no destination is advertised and the failure is
        // surfaced so the shell can render its fail-closed status.
        if (outcome is not AccessOutcome.Granted(var effectiveModules))
        {
            return new NavigationPresentation([], true);
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

        return new NavigationPresentation(live, false);
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
}
