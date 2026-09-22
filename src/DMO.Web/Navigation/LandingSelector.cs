using DMO.Web.Frontend.Shell;

namespace DMO.Web.Navigation;

/// <summary>
/// Result of selecting the landing destination among the ordered live destinations.
/// </summary>
/// <remarks>
/// The selector reports, it never invents: <see cref="ExplicitValid"/> and
/// <see cref="FirstValid"/> carry the exact destination from the caller-provided ordered
/// list; <see cref="NoLanding"/> and <see cref="InvalidExplicitLanding"/> carry nothing.
/// The caller decides what a selection means (P1-T07 <see cref="UserLandingService"/>).
/// </remarks>
public abstract record LandingSelection
{
    /// <summary>The explicit persisted landing matches one live destination (represented and navigable).</summary>
    /// <param name="Destination">The matching destination from the ordered list.</param>
    public sealed record ExplicitValid(PrimaryDestinationPresentation Destination) : LandingSelection;

    /// <summary>No explicit landing: the first valid destination of the ordered list is selected.</summary>
    /// <param name="Destination">The first destination of the ordered list.</param>
    public sealed record FirstValid(PrimaryDestinationPresentation Destination) : LandingSelection;

    /// <summary>There is no valid destination at all (zero live destinations).</summary>
    public sealed record NoLanding : LandingSelection;

    /// <summary>
    /// An explicit persisted landing is present but not represented among the live
    /// destinations (absent, unrouted, or otherwise no longer navigable).
    /// </summary>
    public sealed record InvalidExplicitLanding : LandingSelection;
}

/// <summary>
/// Pure P1-T07 landing selector over the accepted A2 projection output.
/// </summary>
/// <remarks>
/// <para>
/// Algorithm (exact, P1-T07 accepted contract):
/// </para>
/// <code>
/// if orderedDestinations is empty:                          → NoLanding
/// else if explicitLandingDestinationId is null:             → FirstValid(orderedDestinations[0])
/// else if a destination with DestinationId == explicit:     → ExplicitValid(that destination)
/// else:                                                     → InvalidExplicitLanding
/// </code>
/// <para>
/// This selector never orders, collapses, filters or fabricates destinations — those remain
/// exclusively A2 <see cref="NavigationProjectionService"/> responsibilities. Ordering here
/// means the caller's list order is trusted and preserved: the first element is selected as
/// <see cref="FirstValid"/> and <see cref="ExplicitValid"/> matches by stable
/// <c>DestinationId</c> only (ordinal comparison, no label/name matching).
/// </para>
/// <para>
/// The invalid-explicit case is <b>reported, not chosen</b>: the caller maps it to no access
/// (fail closed) — P1-T07 Architect decision on §31.1. It never silently falls back to the
/// first valid destination, never clears the persisted landing, never mutates the projection
/// and never invents another landing.
/// </para>
/// </remarks>
public static class LandingSelector
{
    /// <summary>Selects the landing among the ordered live destinations.</summary>
    /// <param name="explicitLandingDestinationId">
    /// The persisted landing destination id (nullable; <c>null</c> means "no explicit
    /// landing", which is the only case that selects the first destination).
    /// </param>
    /// <param name="orderedDestinations">
    /// The live destinations in Template presentation order (A2 projection output).
    /// </param>
    public static LandingSelection Select(
        string? explicitLandingDestinationId,
        IReadOnlyList<PrimaryDestinationPresentation> orderedDestinations)
    {
        ArgumentNullException.ThrowIfNull(orderedDestinations);

        if (orderedDestinations.Count == 0)
        {
            return new LandingSelection.NoLanding();
        }

        if (explicitLandingDestinationId is null)
        {
            return new LandingSelection.FirstValid(orderedDestinations[0]);
        }

        var target = orderedDestinations.FirstOrDefault(destination =>
            string.Equals(destination.DestinationId, explicitLandingDestinationId, StringComparison.Ordinal));

        return target is not null
            ? new LandingSelection.ExplicitValid(target)
            : new LandingSelection.InvalidExplicitLanding();
    }
}