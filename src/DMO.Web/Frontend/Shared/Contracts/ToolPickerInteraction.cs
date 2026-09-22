namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The deterministic, presentation-only transition model of the shared picker.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §4.1 (normative transition table) with the frozen picker rules of
/// §3.1.5–§3.1.8.
/// <para>
/// This type is the <b>normative statement</b> of the picker's interaction rules; the picker
/// JavaScript asset is a thin DOM adapter over the same rules, so the rules are unit-testable
/// without a browser.
/// </para>
/// <para>
/// Frozen semantics:
/// </para>
/// <list type="bullet">
/// <item><b>never auto-selects</b>: no transition other than an explicit candidate activation
/// selects, including when exactly one candidate is supplied;</item>
/// <item>the search activation and <c>Enter</c> raise only the search request and never select the
/// first (or only) supplied candidate;</item>
/// <item>the selection can never name an unsupplied candidate, and a refused transition raises no
/// event;</item>
/// <item>cancel and <c>Escape</c> raise exactly the same cancel request, change no selection and
/// clear no query — there is no discard path at all;</item>
/// <item>close raises only the return request;</item>
/// <item>the opaque origin token is carried verbatim on every event.</item>
/// </list>
/// <para>
/// The model holds no service, clock, session, identity, address, route, endpoint, ranking or domain
/// vocabulary, and performs no matching, filtering, ordering, scoring or counting.
/// </para>
/// </remarks>
public sealed class ToolPickerInteraction
{
    private readonly List<string> _candidateKeys = [];

    /// <summary>Creates the interaction model for the supplied consumer gating.</summary>
    /// <param name="cancelEnabled">
    /// Whether the cancel/return affordance is currently available; when it is not, cancel and
    /// <c>Escape</c> are refused and raise no event.
    /// </param>
    /// <param name="searchPresented">
    /// Whether the search region is presented. It exists only so the frozen opening focus rule can
    /// be expressed deterministically: the search input is the opening focus target when the search
    /// region is presented, and the region heading otherwise. It changes no other behaviour.
    /// </param>
    public ToolPickerInteraction(bool cancelEnabled, bool searchPresented = true)
    {
        CancelEnabled = cancelEnabled;
        SearchPresented = searchPresented;
    }

    /// <summary>Whether the cancel/return affordance is available.</summary>
    public bool CancelEnabled { get; }

    /// <summary>Whether the search region is presented.</summary>
    public bool SearchPresented { get; }

    /// <summary>The supplied opaque origin token; never parsed.</summary>
    public string? OriginToken { get; private set; }

    /// <summary>The controlled query, rendered verbatim and never searched by this model.</summary>
    public string Query { get; private set; } = string.Empty;

    /// <summary>The controlled selected candidate key, or <c>null</c> when nothing is selected.</summary>
    public string? SelectedCandidateKey { get; private set; }

    /// <summary>The supplied candidate keys, in supplied order.</summary>
    public IReadOnlyList<string> CandidateKeys => _candidateKeys;

    /// <summary>Whether a candidate is selected.</summary>
    public bool HasSelection => SelectedCandidateKey is not null;

    /// <summary>The frozen opening focus target for the supplied search presentation.</summary>
    public ToolPickerFocusTarget OpeningFocusTarget =>
        SearchPresented ? ToolPickerFocusTarget.SearchInput : ToolPickerFocusTarget.RegionHeading;

    /// <summary>Opening the picker raises no event and focuses the accepted opening target.</summary>
    public ToolPickerOutcome Open() =>
        ToolPickerOutcome.Create(null, OpeningFocusTarget);

    /// <summary>
    /// A controlled query change: adopts the new query, raises no event, never searches and
    /// <b>never</b> selects.
    /// </summary>
    public ToolPickerOutcome QueryChanged(string? query)
    {
        Query = query ?? string.Empty;

        return ToolPickerOutcome.Silent();
    }

    /// <summary>
    /// Supplies or replaces the candidate list. Raises no event and <b>never</b> selects — not even
    /// when exactly one candidate is supplied.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// A candidate key is blank, candidate keys are duplicated, or the current controlled selection
    /// would be orphaned by the replacement.
    /// </exception>
    public ToolPickerOutcome CandidatesReceived(IReadOnlyList<string> candidateKeys)
    {
        ArgumentNullException.ThrowIfNull(candidateKeys);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in candidateKeys)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            if (!seen.Add(key))
            {
                throw new ArgumentException(
                    "Supplied candidate keys must be unique.", nameof(candidateKeys));
            }
        }

        if (SelectedCandidateKey is not null && !seen.Contains(SelectedCandidateKey))
        {
            throw new ArgumentException(
                "The controlled selection must remain among the supplied candidates.",
                nameof(candidateKeys));
        }

        _candidateKeys.Clear();
        _candidateKeys.AddRange(candidateKeys);

        return ToolPickerOutcome.Silent();
    }

    /// <summary>
    /// <c>Enter</c> in the search input: raises the search request with the current controlled query
    /// and <b>nothing else</b>. It never selects a candidate and never opens a create subflow.
    /// </summary>
    public ToolPickerOutcome EnterFromSearch() =>
        ToolPickerOutcome.Create(ToolPickerEvent.SearchRequested(OriginToken, Query));

    /// <summary>
    /// An explicit activation of one candidate's select control: the only transition that selects.
    /// </summary>
    /// <exception cref="ArgumentException">The supplied candidate key is blank.</exception>
    public ToolPickerOutcome SelectCandidate(string candidateKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(candidateKey);

        if (!_candidateKeys.Contains(candidateKey, StringComparer.Ordinal))
        {
            return ToolPickerOutcome.Refused();
        }

        SelectedCandidateKey = candidateKey;

        return ToolPickerOutcome.Create(
            ToolPickerEvent.CandidateSelected(OriginToken, candidateKey));
    }

    /// <summary>Requests the consumer's own create subflow. Selects nothing.</summary>
    /// <exception cref="ArgumentException">The supplied action key is blank.</exception>
    public ToolPickerOutcome RequestCreate(string actionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionKey);

        return ToolPickerOutcome.Create(ToolPickerEvent.CreateRequested(OriginToken, actionKey));
    }

    /// <summary>
    /// Cancel: raises one cancel request, changes no selection, clears no query, mutates no origin
    /// state and performs no discard.
    /// </summary>
    /// <exception cref="ArgumentException">The supplied action key is blank.</exception>
    public ToolPickerOutcome Cancel(string? actionKey = null)
    {
        if (!CancelEnabled)
        {
            return ToolPickerOutcome.Refused();
        }

        var key = actionKey ?? ToolPickerPresentation.DefaultCancelActionKey;
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return ToolPickerOutcome.Create(
            ToolPickerEvent.CancelRequested(OriginToken, key),
            ToolPickerFocusTarget.InvokingControl);
    }

    /// <summary>
    /// <c>Escape</c> is exactly <see cref="Cancel"/>: same event, same gate, same focus target and
    /// the same absence of discard. There is no other <c>Escape</c> behaviour.
    /// </summary>
    public ToolPickerOutcome Escape() => Cancel();

    /// <summary>Requests a retry of the consumer's own failing lookup. Selects nothing.</summary>
    /// <exception cref="ArgumentException">The supplied action key is blank.</exception>
    public ToolPickerOutcome RequestRetry(string actionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionKey);

        return ToolPickerOutcome.Create(ToolPickerEvent.RetryRequested(OriginToken, actionKey));
    }

    /// <summary>
    /// The consumer's subflow completed: raises only the return request and restores the invoking
    /// control's focus. It selects nothing and discards nothing.
    /// </summary>
    public ToolPickerOutcome Close() =>
        ToolPickerOutcome.Create(
            ToolPickerEvent.ReturnRequested(OriginToken),
            ToolPickerFocusTarget.InvokingControl);

    /// <summary>
    /// Re-syncs the controlled inputs without raising any event and without ever selecting: a
    /// candidate arriving by sync cannot become selected.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// A candidate key is blank, candidate keys are duplicated, or the supplied selection does not
    /// reference a supplied candidate.
    /// </exception>
    public void Sync(
        string? query,
        IReadOnlyList<string> candidateKeys,
        string? selectedCandidateKey,
        string? originToken)
    {
        ArgumentNullException.ThrowIfNull(candidateKeys);
        Query = query ?? string.Empty;
        OriginToken = originToken;

        CandidatesReceived(candidateKeys);

        if (selectedCandidateKey is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(selectedCandidateKey);
            if (!_candidateKeys.Contains(selectedCandidateKey, StringComparer.Ordinal))
            {
                throw new ArgumentException(
                    "A controlled selected candidate key must reference a supplied candidate.",
                    nameof(selectedCandidateKey));
            }
        }

        SelectedCandidateKey = selectedCandidateKey;
    }
}
