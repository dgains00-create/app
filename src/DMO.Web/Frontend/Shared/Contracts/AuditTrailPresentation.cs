namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The complete consumer-supplied input of an <c>AuditTrail</c>.
/// </summary>
/// <remarks>
/// Authority: P2-T02 contract §6 and A1 freeze §12.
/// <para>
/// <c>AuditTrail</c> is a shared, <b>read-only</b> presentation primitive for supplied
/// historical/audit events. It creates no audit fact, resolves no actor, generates no timestamp,
/// reads no clock or culture, computes no diff, infers no ordering, persists nothing, authorizes
/// nothing and mutates nothing.
/// </para>
/// <para>
/// Concepts that must not be conflated: <c>AuditTrail</c> is this shared supplied-event
/// presentation primitive; <c>HISTÓRICO</c> is local module-specific history functionality
/// inside a module; <c>HISTÓRICO GLOBAL</c> is a future top-level aggregating module and is not
/// part of P2-T02.
/// </para>
/// <para>
/// <b>Ordering.</b> Authority is silent on chronological direction and resolves this by
/// delegation: entries render in <b>exactly</b> the supplied sequence. The component sorts
/// nothing, reorders nothing, asserts no newest-first/oldest-first direction and renders a plain
/// semantic list (no ordered-list chronology claim).
/// </para>
/// </remarks>
public sealed record AuditTrailPresentation
{
    private AuditTrailPresentation(
        CommonState state,
        string regionLabel,
        IReadOnlyList<AuditEntryPresentation> entries,
        string? message,
        string? reason,
        IReadOnlyList<SharedActionPresentation> actions,
        string? contextText)
    {
        State = state;
        RegionLabel = regionLabel;
        Entries = entries;
        Message = message;
        Reason = reason;
        Actions = actions;
        ContextText = contextText;
    }

    /// <summary>The supplied P2-T01 presentation state.</summary>
    public CommonState State { get; }

    /// <summary>Mandatory accessible region label of the history region.</summary>
    public string RegionLabel { get; }

    /// <summary>The supplied entries, in exactly the supplied sequence.</summary>
    public IReadOnlyList<AuditEntryPresentation> Entries { get; }

    /// <summary>The supplied visible message. Mandatory for every non-<c>ready</c> state.</summary>
    public string? Message { get; }

    /// <summary>Optional supplied reason, programmatically associated when present.</summary>
    public string? Reason { get; }

    /// <summary>Supplied region actions (for example a retry or refresh action).</summary>
    public IReadOnlyList<SharedActionPresentation> Actions { get; }

    /// <summary>
    /// Optional supplied parent-controlled filter/page context, rendered verbatim. The component
    /// never computes, applies or filters; parent-owned controls stay in the parent's region.
    /// </summary>
    public string? ContextText { get; }

    /// <summary>The stable P2-T01 CSS token for the supplied state.</summary>
    public string StateToken => CommonStateTraits.CssToken(State);

    /// <summary>Whether the supplied state is <see cref="CommonState.Ready"/>.</summary>
    public bool IsReady => State == CommonState.Ready;

    /// <summary>
    /// Whether supplied entries are rendered: the ready surface, and the stale surface, which
    /// retains the supplied entries alongside the accepted P2-T01 stale warning.
    /// </summary>
    public bool RendersEntries => State is CommonState.Ready or CommonState.Stale;

    /// <summary>
    /// Whether only the accepted P2-T01 state surface is rendered, with no entry markup — so
    /// <c>unavailable</c>/<c>permission-denied</c> never leak partial protected facts.
    /// </summary>
    public bool RendersStateSurfaceOnly => !RendersEntries;

    /// <summary>
    /// Whether the accepted P2-T01 state surface is rendered. Every non-<c>ready</c> state
    /// renders through the accepted P2-T01 presentation; the stale state renders it alongside
    /// the retained entries (contract §7.2).
    /// </summary>
    public bool ShowsStateSurface => !IsReady;

    /// <summary>Whether the region is busy.</summary>
    public bool IsBusy => CommonStateTraits.IsBusy(State);

    /// <summary>Whether the delegated state surface announces assertively.</summary>
    public bool IsAssertive => CommonStateTraits.IsAssertive(State);

    /// <summary>Whether supplied actions accompany the region state.</summary>
    public bool HasActions => Actions.Count > 0;

    /// <summary>Whether a supplied reason accompanies the delegated state surface.</summary>
    public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

    /// <summary>Whether a supplied message accompanies the state.</summary>
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    /// <summary>Whether any entry is supplied.</summary>
    public bool HasEntries => Entries.Count > 0;

    /// <summary>Whether supplied parent-controlled context text is presented.</summary>
    public bool HasContextText => !string.IsNullOrWhiteSpace(ContextText);

    /// <summary>
    /// Whether at least one supplied entry carries a supplied action. Entries are focusable only
    /// when they contain a supplied action; otherwise no entry is in the tab order.
    /// </summary>
    public bool HasInteractiveEntries => Entries.Any(entry => entry.IsInteractive);

    /// <summary>
    /// Projects the supplied state onto the accepted P2-T01 common-state carrier so every
    /// non-<c>ready</c> surface is rendered by the accepted P2-T01 partial rather than by any
    /// audit-specific state markup.
    /// </summary>
    /// <exception cref="InvalidOperationException">The supplied state is <see cref="CommonState.Ready"/>.</exception>
    public CommonStateRegionPresentation ToStateRegion()
    {
        if (IsReady)
        {
            throw new InvalidOperationException(
                "The ready state renders the supplied entries, not a common-state region.");
        }

        return CommonStateRegionPresentation.Create(
            State, Message!, RegionLabel, Reason, Actions);
    }

    /// <summary>
    /// Creates the audit-trail input, failing closed on every supplied inconsistency before
    /// rendering.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The region label is blank, or <paramref name="message"/> is missing for a non-<c>ready</c> state.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">The supplied state is not a defined vocabulary value.</exception>
    public static AuditTrailPresentation Create(
        CommonState state,
        string regionLabel,
        IReadOnlyList<AuditEntryPresentation> entries,
        string? message = null,
        string? reason = null,
        IReadOnlyList<SharedActionPresentation>? actions = null,
        string? contextText = null)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown common state.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(regionLabel);
        ArgumentNullException.ThrowIfNull(entries);

        if (state != CommonState.Ready)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);
        }

        return new AuditTrailPresentation(
            state, regionLabel, entries, message, reason, actions ?? [], contextText);
    }

    /// <summary>Creates the ready history presentation.</summary>
    public static AuditTrailPresentation Ready(
        string regionLabel,
        IReadOnlyList<AuditEntryPresentation> entries,
        string? contextText = null) =>
        Create(CommonState.Ready, regionLabel, entries, contextText: contextText);

    /// <summary>Creates the honest no-history state. Explicitly not a failure.</summary>
    public static AuditTrailPresentation Empty(
        string regionLabel,
        string message,
        IReadOnlyList<SharedActionPresentation>? actions = null) =>
        Create(CommonState.Empty, regionLabel, [], message: message, actions: actions);

    /// <summary>Creates the busy labelled history region while information is obtained.</summary>
    public static AuditTrailPresentation Loading(string regionLabel, string message) =>
        Create(CommonState.Loading, regionLabel, [], message: message);

    /// <summary>Creates the lookup-failed state with its supplied retry.</summary>
    public static AuditTrailPresentation LookupFailed(
        string regionLabel,
        string message,
        SharedActionPresentation? retry = null,
        string? reason = null) =>
        Create(CommonState.LookupFailed, regionLabel, [], message: message, reason: reason,
            actions: retry is null ? null : [retry]);

    /// <summary>Creates the non-permission unavailable state with its supplied reason.</summary>
    public static AuditTrailPresentation Unavailable(
        string regionLabel,
        string message,
        string? reason = null) =>
        Create(CommonState.Unavailable, regionLabel, [], message: message, reason: reason);

    /// <summary>Creates the permission-denied state with its supplied reason.</summary>
    public static AuditTrailPresentation PermissionDenied(
        string regionLabel,
        string message,
        string? reason = null) =>
        Create(CommonState.PermissionDenied, regionLabel, [], message: message, reason: reason);

    /// <summary>Creates the stale state, retaining the supplied entries plus the P2-T01 stale warning.</summary>
    public static AuditTrailPresentation Stale(
        string regionLabel,
        string message,
        IReadOnlyList<AuditEntryPresentation> entries,
        IReadOnlyList<SharedActionPresentation>? actions = null,
        string? reason = null,
        string? contextText = null) =>
        Create(CommonState.Stale, regionLabel, entries, message: message, reason: reason,
            actions: actions, contextText: contextText);
}
