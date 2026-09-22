namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// Presentation model for the shared common-state region.
/// </summary>
/// <remarks>
/// Authority: freeze §4 (the ten-state FINAL PRESENTATION CONTRACT vocabulary) and §3 rules.
/// <para>
/// The region renders exactly one supplied <see cref="CommonState"/>. It is presentation-only:
/// it never decides access, never fetches, never persists and never infers a domain outcome.
/// A consumer maps a real application/backend outcome into the correct state (freeze §4).
/// </para>
/// <para>
/// <see cref="CommonState.Empty"/>, <see cref="CommonState.LookupFailed"/>,
/// <see cref="CommonState.Unavailable"/> and <see cref="CommonState.PermissionDenied"/> are
/// rendered as four structurally and semantically distinct surfaces, never as the same
/// surface (freeze §4 closing rule).
/// </para>
/// </remarks>
public sealed record CommonStateRegionPresentation
{
    private CommonStateRegionPresentation(
        CommonState state,
        string message,
        string regionLabel,
        string? reason,
        IReadOnlyList<SharedActionPresentation> actions,
        CommonState? wrappingState)
    {
        State = state;
        Message = message;
        RegionLabel = regionLabel;
        Reason = reason;
        Actions = actions;
        WrappingState = wrappingState;
    }

    /// <summary>The supplied presentation state.</summary>
    public CommonState State { get; }

    /// <summary>The mandatory visible message. Never blank, so the surface is never colour-only.</summary>
    public string Message { get; }

    /// <summary>The supplied accessible region label.</summary>
    public string RegionLabel { get; }

    /// <summary>Optional supplied reason, programmatically associated when present.</summary>
    public string? Reason { get; }

    /// <summary>Supplied consumer actions (for example a retry or reset action).</summary>
    public IReadOnlyList<SharedActionPresentation> Actions { get; }

    /// <summary>
    /// An optional common state that wraps the present state. Freeze §11 permits common states
    /// to wrap — but never replace — a supplied outcome; this member preserves that wrapping
    /// without collapsing the two.
    /// </summary>
    public CommonState? WrappingState { get; }

    /// <summary>The stable CSS token for the state (kebab-case).</summary>
    public string StateToken => CommonStateTraits.CssToken(State);

    /// <summary>Whether the affected region is busy.</summary>
    public bool IsBusy => CommonStateTraits.IsBusy(State);

    /// <summary>Whether the region announces assertively.</summary>
    public bool IsAssertive => CommonStateTraits.IsAssertive(State);

    /// <summary>Whether a supplied reason must be programmatically associated.</summary>
    public bool RequiresAssociatedReason => CommonStateTraits.RequiresAssociatedReason(State);

    /// <summary>Whether the state was additionally wrapped by a common state.</summary>
    public bool IsWrapped => WrappingState is not null;

    /// <summary>The wrapper's CSS token when wrapped; otherwise <c>null</c>.</summary>
    public string? WrappingStateToken =>
        WrappingState is { } wrapping ? CommonStateTraits.CssToken(wrapping) : null;

    /// <summary>Whether supplied actions are presented.</summary>
    public bool HasActions => Actions.Count > 0;

    /// <summary>Whether a supplied reason accompanies the message.</summary>
    public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

    /// <summary>
    /// Creates a common-state region presentation.
    /// </summary>
    /// <exception cref="ArgumentException">The message or region label is blank.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The state (or wrapping state) is not a defined vocabulary value.
    /// </exception>
    public static CommonStateRegionPresentation Create(
        CommonState state,
        string message,
        string regionLabel,
        string? reason = null,
        IReadOnlyList<SharedActionPresentation>? actions = null,
        CommonState? wrappingState = null)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown common state.");
        }

        if (wrappingState is { } wrapping && !Enum.IsDefined(wrapping))
        {
            throw new ArgumentOutOfRangeException(
                nameof(wrappingState), wrapping, "Unknown wrapping common state.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(regionLabel);

        return new CommonStateRegionPresentation(
            state, message, regionLabel, reason, actions ?? [], wrappingState);
    }

    /// <summary>
    /// Creates the honest no-results state (request succeeded; nothing returned). Any supplied
    /// next action is optional and passed through <paramref name="actions"/>.
    /// </summary>
    public static CommonStateRegionPresentation Empty(
        string message,
        string regionLabel,
        IReadOnlyList<SharedActionPresentation>? actions = null) =>
        Create(CommonState.Empty, message, regionLabel, actions: actions);

    /// <summary>Creates the lookup-failed state, optionally with a supplied retry action.</summary>
    public static CommonStateRegionPresentation LookupFailed(
        string message,
        string regionLabel,
        SharedActionPresentation? retry = null) =>
        Create(CommonState.LookupFailed, message, regionLabel, actions: retry is null ? null : [retry]);

    /// <summary>Creates the non-permission unavailable state with its supplied reason.</summary>
    public static CommonStateRegionPresentation Unavailable(
        string message,
        string regionLabel,
        string? reason = null) =>
        Create(CommonState.Unavailable, message, regionLabel, reason);

    /// <summary>Creates the published permission-denied state.</summary>
    public static CommonStateRegionPresentation PermissionDenied(
        string message,
        string regionLabel,
        string? reason = null) =>
        Create(CommonState.PermissionDenied, message, regionLabel, reason);
}
