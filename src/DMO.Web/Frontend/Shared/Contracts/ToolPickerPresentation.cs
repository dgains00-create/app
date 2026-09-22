namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The complete consumer-supplied input of the shared picker.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.1 (especially §3.1.4, §3.1.5, §3.1.6, §3.1.7, §3.1.9).
/// <para>
/// The picker is a <b>shared presentation primitive</b>: it renders a supplied controlled search
/// state and a supplied candidate list, arbitrates only the explicit candidate-selection
/// interaction, presents the supplied origin context, exposes the consumer-gated create/cancel/retry
/// affordances, and delegates every non-<c>ready</c> surface to the accepted common-state
/// presentation.
/// </para>
/// <para>
/// It owns <b>no</b> domain semantics and performs <b>no</b> search, matching, ranking, ordering,
/// counting, lookup, association, persistence, authorization or navigation. Every key it carries is
/// opaque consumer presentation data and is never a canonical domain identity.
/// </para>
/// <para>
/// <b>Never auto-select.</b> The picker presents the controlled selection it is given and changes it
/// only when the consumer supplies a different value or the user explicitly activates one
/// candidate's select control.
/// </para>
/// </remarks>
public sealed record ToolPickerPresentation
{
    /// <summary>The generic A-owned visible label of the search affordance.</summary>
    public const string GenericSearchLabel = "Pesquisar";

    /// <summary>The generic A-owned visible qualifier of the supplied expected-type display label.</summary>
    public const string GenericExpectedTypeQualifier = "Tipo esperado";

    /// <summary>The generic A-owned visible label of a candidate's explicit select control.</summary>
    public const string GenericSelectControlLabel = "Selecionar";

    /// <summary>The generic A-owned visible label of the component-owned default cancel control.</summary>
    public const string GenericCancelLabel = "Cancelar";

    /// <summary>
    /// The component-owned opaque key used when the consumer supplies no cancel affordance, so the
    /// frozen cancel/return event keeps a stable key.
    /// </summary>
    public const string DefaultCancelActionKey = "cancel";

    private ToolPickerPresentation(
        CommonState state,
        string regionLabel,
        string query,
        IReadOnlyList<ToolPickerCandidatePresentation> candidates,
        string? selectedCandidateKey,
        string? originToken,
        IReadOnlyList<ToolPickerFactPresentation> originContext,
        string? expectedTypeLabel,
        SharedActionPresentation? createAction,
        SharedActionPresentation? cancelAction,
        SharedActionPresentation? retryAction,
        string? message,
        string? reason,
        IReadOnlyList<SharedActionPresentation> stateActions,
        string? resultSummary)
    {
        State = state;
        RegionLabel = regionLabel;
        Query = query;
        Candidates = candidates;
        SelectedCandidateKey = selectedCandidateKey;
        OriginToken = originToken;
        OriginContext = originContext;
        ExpectedTypeLabel = expectedTypeLabel;
        CreateAction = createAction;
        CancelAction = cancelAction;
        RetryAction = retryAction;
        Message = message;
        Reason = reason;
        StateActions = stateActions;
        ResultSummary = resultSummary;
    }

    /// <summary>The supplied common presentation state.</summary>
    public CommonState State { get; }

    /// <summary>The supplied accessible name of the picker surface.</summary>
    public string RegionLabel { get; }

    /// <summary>The controlled supplied query, rendered verbatim and never mutated.</summary>
    public string Query { get; }

    /// <summary>The supplied candidates, in exactly the supplied order.</summary>
    public IReadOnlyList<ToolPickerCandidatePresentation> Candidates { get; }

    /// <summary>The controlled explicitly selected opaque candidate key, or <c>null</c> for none.</summary>
    public string? SelectedCandidateKey { get; }

    /// <summary>The supplied opaque origin token, echoed verbatim on every event.</summary>
    public string? OriginToken { get; }

    /// <summary>The supplied origin-context display facts, in supplied order.</summary>
    public IReadOnlyList<ToolPickerFactPresentation> OriginContext { get; }

    /// <summary>The optional supplied expected-type display label.</summary>
    public string? ExpectedTypeLabel { get; }

    /// <summary>The optional supplied create affordance.</summary>
    public SharedActionPresentation? CreateAction { get; }

    /// <summary>The optional supplied cancel/return affordance.</summary>
    public SharedActionPresentation? CancelAction { get; }

    /// <summary>The optional supplied retry affordance for a failed lookup.</summary>
    public SharedActionPresentation? RetryAction { get; }

    /// <summary>The supplied visible message; mandatory for every non-<c>ready</c> state.</summary>
    public string? Message { get; }

    /// <summary>Optional supplied reason, programmatically associated when present.</summary>
    public string? Reason { get; }

    /// <summary>Additional supplied state actions rendered by the accepted state region.</summary>
    public IReadOnlyList<SharedActionPresentation> StateActions { get; }

    /// <summary>Optional supplied result/count text, rendered verbatim and announced politely.</summary>
    public string? ResultSummary { get; }

    /// <summary>The stable common-state CSS token.</summary>
    public string StateToken => CommonStateTraits.CssToken(State);

    /// <summary>Whether the supplied state is <c>ready</c>.</summary>
    public bool IsReady => State == CommonState.Ready;

    /// <summary>Whether a consumer-owned create/association subflow is pending.</summary>
    public bool IsPending => State is CommonState.Saving or CommonState.Submitting;

    /// <summary>Whether the region is busy in the accepted common-state vocabulary.</summary>
    public bool IsBusy => CommonStateTraits.IsBusy(State);

    /// <summary>The search region is presented in these states (contract §3.1.9).</summary>
    public bool ShowsSearch =>
        State is CommonState.Ready
            or CommonState.Empty
            or CommonState.LookupFailed
            or CommonState.Stale
            or CommonState.Conflict;

    /// <summary>The candidate region is presented in these states (contract §3.1.9).</summary>
    public bool ShowsCandidates =>
        State is CommonState.Ready
            or CommonState.Stale
            or CommonState.Conflict
            or CommonState.Saving
            or CommonState.Submitting;

    /// <summary>Whether the explicit no-results state region carries the empty outcome.</summary>
    public bool ShowsNoResults => State == CommonState.Empty;

    /// <summary>The explicit candidate select controls are enabled except while a subflow is pending.</summary>
    public bool SelectControlsEnabled => ShowsCandidates && !IsPending;

    /// <summary>
    /// The supplied reason exposed with the temporarily unavailable select controls while a
    /// consumer-owned subflow is pending; never invented.
    /// </summary>
    public string? SelectUnavailableReason => IsPending ? Reason ?? Message : null;

    /// <summary>
    /// The create affordance exists if and only if the consumer supplies it visible <b>and</b>
    /// enabled <b>and</b> the state is <c>ready</c> or <c>empty</c> (contract §3.1.6).
    /// </summary>
    public bool ShowsCreate =>
        CreateAction is { Visible: true, Enabled: true } &&
        State is CommonState.Ready or CommonState.Empty;

    /// <summary>Whether a cancel/return affordance is presented in this state.</summary>
    public bool ShowsCancel => CancelAction is null || CancelAction.Visible;

    /// <summary>Whether the cancel/return affordance can be invoked.</summary>
    public bool CancelEnabled => CancelAction is null || CancelAction.Enabled;

    /// <summary>The visible cancel/return label (supplied, else the generic A-owned label).</summary>
    public string CancelLabel => CancelAction?.Label ?? GenericCancelLabel;

    /// <summary>The opaque cancel/return action key (supplied, else the component-owned default).</summary>
    public string CancelActionKey => CancelAction?.Key ?? DefaultCancelActionKey;

    /// <summary>The supplied cancel/return disabled reason, when provided.</summary>
    public string? CancelDisabledReason => CancelAction?.DisabledReason;

    /// <summary>Every non-<c>ready</c> surface is rendered by the accepted state region.</summary>
    public bool ShowsStateSurface => !IsReady;

    /// <summary>Whether any candidate is supplied.</summary>
    public bool HasCandidates => Candidates.Count > 0;

    /// <summary>Whether a supplied result summary is presented.</summary>
    public bool HasResultSummary => !string.IsNullOrWhiteSpace(ResultSummary);

    /// <summary>Whether a supplied expected-type label is presented.</summary>
    public bool HasExpectedTypeLabel => !string.IsNullOrWhiteSpace(ExpectedTypeLabel);

    /// <summary>Whether supplied origin-context facts are presented.</summary>
    public bool HasOriginContext => OriginContext.Count > 0;

    /// <summary>Whether the consumer supplied a create affordance at all.</summary>
    public bool HasCreateAction => CreateAction is not null;

    /// <summary>Whether a supplied reason accompanies the delegated state surface.</summary>
    public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

    /// <summary>
    /// The actions passed to the accepted state region: the supplied retry accompanies only a
    /// failed lookup, followed by any additional supplied state actions.
    /// </summary>
    public IReadOnlyList<SharedActionPresentation> StateRegionActions
    {
        get
        {
            if (State != CommonState.LookupFailed || RetryAction is null)
            {
                return StateActions;
            }

            var actions = new List<SharedActionPresentation>(StateActions.Count + 1) { RetryAction };
            actions.AddRange(StateActions);

            return actions;
        }
    }

    /// <summary>Whether the supplied candidate is the controlled selected candidate.</summary>
    public bool IsCandidateSelected(string candidateKey) =>
        SelectedCandidateKey is not null &&
        string.Equals(SelectedCandidateKey, candidateKey, StringComparison.Ordinal);

    /// <summary>
    /// Projects the supplied non-<c>ready</c> state onto the accepted common-state carrier so every
    /// state surface is rendered by the accepted shared partial rather than re-implemented here.
    /// </summary>
    /// <exception cref="InvalidOperationException">The supplied state is <c>ready</c>.</exception>
    public CommonStateRegionPresentation ToStateRegion()
    {
        if (IsReady)
        {
            throw new InvalidOperationException(
                "The ready state renders the picker, not a common-state region.");
        }

        return CommonStateRegionPresentation.Create(
            State, Message!, RegionLabel, Reason, StateRegionActions);
    }

    /// <summary>
    /// Creates the picker input, failing closed on every supplied inconsistency before rendering.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The region label is blank; a candidate key or its accessible context is blank; candidate keys
    /// are duplicated; a fact label or value is blank; the supplied expected-type label is blank;
    /// the controlled selected key is blank or does not reference a supplied candidate; or the
    /// message is missing for a non-<c>ready</c> state.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">The supplied state is not a defined value.</exception>
    public static ToolPickerPresentation Create(
        CommonState state,
        string regionLabel,
        string? query = null,
        IReadOnlyList<ToolPickerCandidatePresentation>? candidates = null,
        string? selectedCandidateKey = null,
        string? originToken = null,
        IReadOnlyList<ToolPickerFactPresentation>? originContext = null,
        string? expectedTypeLabel = null,
        SharedActionPresentation? createAction = null,
        SharedActionPresentation? cancelAction = null,
        SharedActionPresentation? retryAction = null,
        string? message = null,
        string? reason = null,
        IReadOnlyList<SharedActionPresentation>? stateActions = null,
        string? resultSummary = null)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown common state.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(regionLabel);
        ArgumentNullException.ThrowIfNull(candidates);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in candidates)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(candidate.Key);
            ArgumentException.ThrowIfNullOrWhiteSpace(candidate.AccessibleContext);

            if (!seen.Add(candidate.Key))
            {
                throw new ArgumentException(
                    "Supplied candidate keys must be unique.", nameof(candidates));
            }

            foreach (var fact in candidate.Facts)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(fact.Label);
                ArgumentException.ThrowIfNullOrWhiteSpace(fact.Value);
            }
        }

        foreach (var fact in originContext ?? [])
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fact.Label);
            ArgumentException.ThrowIfNullOrWhiteSpace(fact.Value);
        }

        if (expectedTypeLabel is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(expectedTypeLabel);
        }

        if (selectedCandidateKey is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(selectedCandidateKey);
            if (!seen.Contains(selectedCandidateKey))
            {
                throw new ArgumentException(
                    "A controlled selected candidate key must reference a supplied candidate.",
                    nameof(selectedCandidateKey));
            }
        }

        if (state != CommonState.Ready)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);
        }

        return new ToolPickerPresentation(
            state,
            regionLabel,
            query ?? string.Empty,
            candidates,
            selectedCandidateKey,
            originToken,
            originContext ?? [],
            string.IsNullOrWhiteSpace(expectedTypeLabel) ? null : expectedTypeLabel,
            createAction,
            cancelAction,
            retryAction,
            message,
            reason,
            stateActions ?? [],
            resultSummary);
    }
}
