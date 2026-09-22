namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The complete consumer-supplied input of the decision bar.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.4 (especially §3.4.2, §3.4.3, §3.4.4).
/// <para>
/// The bar presents a supplied, ordered list of consumer actions, each in its supplied
/// classification, with supplied visible/enabled/disabled-reason/pending presentation. It encodes
/// <b>no</b> transition: no approval, rejection, reopening, submission, closing, movement, release,
/// repair or warehouse meaning exists in this type, in its members or in its supplied defaults.
/// </para>
/// <para>
/// Supplied order is authoritative: the group is a classification that never reorders, relocates or
/// filters. Availability is exactly the supplied availability — the bar disables nothing because of
/// the region state — with one frozen exception: the action named by the controlled
/// <see cref="PendingActionKey"/> cannot be invoked again until the consumer releases it.
/// </para>
/// </remarks>
public sealed record DecisionBarPresentation
{
    /// <summary>The generic A-owned accessible name of the action region.</summary>
    public const string GenericRegionLabel = "Ações";

    /// <summary>
    /// The generic A-owned textual qualifier presented for a risk-classified action, so that
    /// classification is never conveyed by colour alone.
    /// </summary>
    public const string RiskGroupQualifier = "Ação de risco";

    private DecisionBarPresentation(
        CommonState state,
        IReadOnlyList<DecisionBarActionPresentation> actions,
        string? pendingActionKey,
        string? regionLabel,
        string? statusText,
        string? message,
        string? reason,
        IReadOnlyList<SharedActionPresentation> stateActions)
    {
        State = state;
        Actions = actions;
        PendingActionKey = pendingActionKey;
        RegionLabel = regionLabel;
        StatusText = statusText;
        Message = message;
        Reason = reason;
        StateActions = stateActions;
    }

    /// <summary>The supplied common presentation state.</summary>
    public CommonState State { get; }

    /// <summary>The supplied actions, in exactly the supplied order.</summary>
    public IReadOnlyList<DecisionBarActionPresentation> Actions { get; }

    /// <summary>The controlled pending action key, or <c>null</c> when nothing is pending.</summary>
    public string? PendingActionKey { get; }

    /// <summary>The optional supplied accessible name of the region.</summary>
    public string? RegionLabel { get; }

    /// <summary>Optional supplied help/status text, rendered verbatim.</summary>
    public string? StatusText { get; }

    /// <summary>The supplied visible message; mandatory for every non-<c>ready</c> state.</summary>
    public string? Message { get; }

    /// <summary>Optional supplied reason, programmatically associated when present.</summary>
    public string? Reason { get; }

    /// <summary>Supplied state actions rendered by the accepted state region.</summary>
    public IReadOnlyList<SharedActionPresentation> StateActions { get; }

    /// <summary>The stable common-state CSS token.</summary>
    public string StateToken => CommonStateTraits.CssToken(State);

    /// <summary>Whether the supplied state is <c>ready</c>.</summary>
    public bool IsReady => State == CommonState.Ready;

    /// <summary>Whether the region is busy in the accepted common-state vocabulary.</summary>
    public bool IsBusy => CommonStateTraits.IsBusy(State);

    /// <summary>Every non-<c>ready</c> surface is rendered by the accepted state region.</summary>
    public bool ShowsStateSurface => !IsReady;

    /// <summary>The supplied actions that are presented, in exactly the supplied order.</summary>
    public IReadOnlyList<DecisionBarActionPresentation> VisibleActions =>
        Actions.Where(action => action.Action.Visible).ToList();

    /// <summary>Whether any supplied action is presented.</summary>
    public bool HasActions => VisibleActions.Count > 0;

    /// <summary>Whether a supplied help/status text is presented.</summary>
    public bool HasStatusText => !string.IsNullOrWhiteSpace(StatusText);

    /// <summary>Whether a supplied reason accompanies the delegated state surface.</summary>
    public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

    /// <summary>Whether a supplied action is currently pending.</summary>
    public bool HasPendingAction => PendingActionKey is not null;

    /// <summary>
    /// Whether the region exposes the busy fact: a pending supplied action, or a supplied state that
    /// is busy in the accepted common-state vocabulary.
    /// </summary>
    public bool IsRegionBusy => IsBusy || HasPendingAction;

    /// <summary>The accessible name of the region (supplied, else the generic A-owned label).</summary>
    public string RegionLabelOrDefault =>
        string.IsNullOrWhiteSpace(RegionLabel) ? GenericRegionLabel : RegionLabel!;

    /// <summary>Whether the supplied action is the controlled pending action.</summary>
    public bool IsActionPending(DecisionBarActionPresentation action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return PendingActionKey is not null &&
            string.Equals(PendingActionKey, action.Key, StringComparison.Ordinal);
    }

    /// <summary>Whether an action's supplied pending label is presented while it is pending.</summary>
    public bool ShowsPendingText(DecisionBarActionPresentation action) =>
        IsActionPending(action) && !string.IsNullOrWhiteSpace(action.Action.PendingLabel);

    /// <summary>
    /// Whether the action is presented disabled: supplied disabled, or pending (the frozen
    /// duplicate-invocation block).
    /// </summary>
    public bool IsActionUnavailable(DecisionBarActionPresentation action) =>
        !action.Action.Enabled || IsActionPending(action);

    /// <summary>The supplied reason exposed with an unavailable action; never invented.</summary>
    public string? UnavailableReason(DecisionBarActionPresentation action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return !action.Action.Enabled ? action.Action.DisabledReason : null;
    }

    /// <summary>
    /// Projects the supplied non-<c>ready</c> state onto the accepted common-state carrier so every
    /// state surface is rendered by the accepted shared partial.
    /// </summary>
    /// <exception cref="InvalidOperationException">The supplied state is <c>ready</c>.</exception>
    public CommonStateRegionPresentation ToStateRegion()
    {
        if (IsReady)
        {
            throw new InvalidOperationException(
                "The ready state renders the action region, not a common-state region.");
        }

        return CommonStateRegionPresentation.Create(
            State, Message!, RegionLabelOrDefault, Reason, StateActions);
    }

    /// <summary>
    /// Creates the bar input, failing closed on every supplied inconsistency before rendering.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// A supplied action key is duplicated; the controlled pending key does not reference a supplied
    /// action; or the message is missing for a non-<c>ready</c> state.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">The supplied state is not a defined value.</exception>
    public static DecisionBarPresentation Create(
        CommonState state,
        IReadOnlyList<DecisionBarActionPresentation>? actions = null,
        string? pendingActionKey = null,
        string? regionLabel = null,
        string? statusText = null,
        string? message = null,
        string? reason = null,
        IReadOnlyList<SharedActionPresentation>? stateActions = null)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown common state.");
        }

        var suppliedActions = actions ?? [];
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var action in suppliedActions)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(action.Key);

            if (!seen.Add(action.Key))
            {
                throw new ArgumentException("Supplied action keys must be unique.", nameof(actions));
            }
        }

        if (pendingActionKey is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pendingActionKey);
            if (!seen.Contains(pendingActionKey))
            {
                throw new ArgumentException(
                    "A controlled pending action key must reference a supplied action.",
                    nameof(pendingActionKey));
            }
        }

        if (state != CommonState.Ready)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);
        }

        return new DecisionBarPresentation(
            state,
            suppliedActions,
            pendingActionKey,
            string.IsNullOrWhiteSpace(regionLabel) ? null : regionLabel,
            statusText,
            message,
            reason,
            stateActions ?? []);
    }
}
