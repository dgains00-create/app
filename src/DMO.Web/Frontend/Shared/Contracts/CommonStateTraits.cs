namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// Presentation traits derived from the frozen state definitions (freeze §4 table).
/// </summary>
/// <remarks>
/// Domain-neutral: the traits describe <b>how</b> a state is presented and announced, never
/// what it means industrially. No trait grants an action, infers a lifecycle, or decides
/// access.
/// </remarks>
public static class CommonStateTraits
{
    /// <summary>
    /// Whether the affected region is busy because information is being obtained or a
    /// save/transition is in progress.
    /// </summary>
    /// <remarks>
    /// Freeze §4: <c>loading</c>, <c>saving</c> and <c>submitting</c> mark the region busy.
    /// </remarks>
    public static bool IsBusy(CommonState state) =>
        state is CommonState.Loading or CommonState.Saving or CommonState.Submitting;

    /// <summary>
    /// Whether the state announces assertively rather than politely.
    /// </summary>
    /// <remarks>
    /// Freeze §4: <c>lookup-failed</c> and <c>conflict</c> announce assertively without
    /// automatic focus theft; every other state announces politely or as ordinary content.
    /// </remarks>
    public static bool IsAssertive(CommonState state) =>
        state is CommonState.LookupFailed or CommonState.Conflict;

    /// <summary>
    /// The stable CSS token for the state (kebab-case, matching the frozen vocabulary).
    /// </summary>
    public static string CssToken(CommonState state) => state switch
    {
        CommonState.Loading => "loading",
        CommonState.Ready => "ready",
        CommonState.Empty => "empty",
        CommonState.LookupFailed => "lookup-failed",
        CommonState.Unavailable => "unavailable",
        CommonState.PermissionDenied => "permission-denied",
        CommonState.Saving => "saving",
        CommonState.Submitting => "submitting",
        CommonState.Stale => "stale",
        CommonState.Conflict => "conflict",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown common state."),
    };

    /// <summary>
    /// Whether the state requires a programmatically associated supplied reason so dependent
    /// actions can be linked to it.
    /// </summary>
    /// <remarks>
    /// Freeze §4: <c>unavailable</c> links its supplied reason to the affected
    /// region/actions; <c>permission-denied</c> identifies the unavailable function;
    /// <c>lookup-failed</c> is announced assertively for the affected region.
    /// </remarks>
    public static bool RequiresAssociatedReason(CommonState state) =>
        state is CommonState.Unavailable or CommonState.PermissionDenied or CommonState.LookupFailed;

    /// <summary>
    /// The set of states that must never render identically to <see cref="CommonState.Empty"/>
    /// (freeze §4 closing rule).
    /// </summary>
    public static IReadOnlyList<CommonState> MutuallyDistinctFromEmpty { get; } =
    [
        CommonState.Empty,
        CommonState.LookupFailed,
        CommonState.Unavailable,
        CommonState.PermissionDenied,
    ];

    /// <summary>Every frozen state, in vocabulary order.</summary>
    public static IReadOnlyList<CommonState> All { get; } = Enum.GetValues<CommonState>();
}
