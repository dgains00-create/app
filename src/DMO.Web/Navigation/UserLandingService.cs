using DMO.Application.Session;
using DMO.Web.Frontend.Shell;

namespace DMO.Web.Navigation;

/// <summary>
/// Runtime USER landing result consumed by the root router and post-login dispatch.
/// </summary>
public abstract record UserLanding
{
    /// <summary>The USER must be redirected to the given route.</summary>
    /// <param name="Route">The real registered destination route.</param>
    public sealed record RedirectTo(string Route) : UserLanding;

    /// <summary>The USER has no valid landing: fail closed (no-access).</summary>
    public sealed record NoAccess : UserLanding;
}

/// <summary>
/// P1-T07 runtime landing resolution for an active USER account.
/// </summary>
/// <remarks>
/// <para>
/// The service consumes <b>exactly one</b> navigation projection — the accepted A2
/// <see cref="NavigationProjectionService"/> — and never resolves access separately, never
/// queries the Template directly, never re-reads the Module Registry and never re-runs
/// authorization. It only translates the projection output into a routing decision:
/// </para>
/// <code>
/// projection.AccessResolutionFailed                       → NoAccess
/// selection is NoLanding                                  → NoAccess
/// selection is InvalidExplicitLanding                     → NoAccess   (fail closed; §31.1 settled)
/// selection is ExplicitValid(destination)                 → RedirectTo(destination.Href)
/// selection is FirstValid(destination)                    → RedirectTo(destination.Href)
/// </code>
/// <para>
/// A <see cref="RedirectTo"/> target is always a real registered route carried by the A2
/// destination; this service never fabricates routes.
/// </para>
/// </remarks>
public sealed class UserLandingService
{
    private readonly NavigationProjectionService _projection;

    /// <summary>Creates the service over the accepted A2 navigation projection.</summary>
    public UserLandingService(NavigationProjectionService projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        _projection = projection;
    }

    /// <summary>Resolves the landing for the given active USER account.</summary>
    /// <param name="user">The current USER account (already active/resolved by the session boundary).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<UserLanding> ResolveAsync(
        CurrentAccount.User user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        var presentation = await _projection.ProjectAsync(user, cancellationToken);

        // Fail closed on any resolution failure: no landing is derived from a denied outcome.
        if (presentation.AccessResolutionFailed)
        {
            return new UserLanding.NoAccess();
        }

        var selection = LandingSelector.Select(
            presentation.LandingDestinationId,
            presentation.LiveDestinations);

        return selection switch
        {
            LandingSelection.ExplicitValid(var destination) =>
                new UserLanding.RedirectTo(destination.Href),
            LandingSelection.FirstValid(var destination) =>
                new UserLanding.RedirectTo(destination.Href),
            LandingSelection.NoLanding => new UserLanding.NoAccess(),
            // Architect decision (§31.1 settled, PLAN ACCEPT): an explicit persisted landing
            // that is no longer represented among the valid composed/routed destinations is
            // an invalid explicit landing → fail closed → no access. No fallback to the
            // first valid destination, no silent clearing, no Template mutation.
            LandingSelection.InvalidExplicitLanding => new UserLanding.NoAccess(),
            _ => new UserLanding.NoAccess(),
        };
    }
}