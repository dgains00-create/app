namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The complete consumer-supplied input of the compact summary row.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.2 (especially §3.2.2, §3.2.3, §3.2.5).
/// <para>
/// The row renders <b>exactly</b> the supplied facts, in a fixed presentation slot order, with the
/// supplied status rendered by the accepted shared status component and the supplied actions
/// rendered by the accepted shared action carrier. It performs <b>no</b> lookup and <b>no</b>
/// inference: nothing is resolved, computed, aggregated, reformatted or defaulted, and an absent
/// optional slot renders nothing.
/// </para>
/// <para>
/// The slots are A-owned <b>presentation positions with no domain meaning</b>: the primitive assigns
/// no meaning to which slot carries which fact, defines no required fact and defines no controlled
/// field list. Both the label and the value are supplied by the consumer.
/// </para>
/// <para>
/// <see cref="Selected"/> is a <b>controlled presentation fact</b> of the enclosing selectable
/// consumer surface. The row performs no selection arbitration and raises no selection event.
/// </para>
/// </remarks>
public sealed record ToolSummaryRowPresentation
{
    private ToolSummaryRowPresentation(
        CommonState state,
        string itemKey,
        string? accessibleContext,
        ToolSummaryFactPresentation? type,
        ToolSummaryFactPresentation? reference,
        ToolSummaryFactPresentation? lot,
        ToolSummaryFactPresentation? machines,
        ToolSummaryFactPresentation? quantity,
        ToolSummaryFactPresentation? process,
        ToolSummaryFactPresentation? context,
        IReadOnlyList<ToolSummaryFactPresentation> additionalFacts,
        RecordStatusPresentation? status,
        IReadOnlyList<SharedActionPresentation> actions,
        bool selected,
        string? message,
        string? reason,
        IReadOnlyList<SharedActionPresentation> stateActions)
    {
        State = state;
        ItemKey = itemKey;
        AccessibleContext = accessibleContext;
        Type = type;
        Reference = reference;
        Lot = lot;
        Machines = machines;
        Quantity = quantity;
        Process = process;
        Context = context;
        AdditionalFacts = additionalFacts;
        Status = status;
        Actions = actions;
        Selected = selected;
        Message = message;
        Reason = reason;
        StateActions = stateActions;
    }

    /// <summary>The supplied common presentation state.</summary>
    public CommonState State { get; }

    /// <summary>The opaque item key. Never a canonical identity.</summary>
    public string ItemKey { get; }

    /// <summary>Optional supplied context qualifying supplied action accessible names.</summary>
    public string? AccessibleContext { get; }

    /// <summary>The optional supplied type slot.</summary>
    public ToolSummaryFactPresentation? Type { get; }

    /// <summary>The optional supplied reference slot.</summary>
    public ToolSummaryFactPresentation? Reference { get; }

    /// <summary>The optional supplied lot slot.</summary>
    public ToolSummaryFactPresentation? Lot { get; }

    /// <summary>The optional supplied machines/lines slot.</summary>
    public ToolSummaryFactPresentation? Machines { get; }

    /// <summary>The optional supplied quantity slot.</summary>
    public ToolSummaryFactPresentation? Quantity { get; }

    /// <summary>The optional supplied process slot.</summary>
    public ToolSummaryFactPresentation? Process { get; }

    /// <summary>The optional supplied concise-context slot.</summary>
    public ToolSummaryFactPresentation? Context { get; }

    /// <summary>Optional additional supplied facts, appended after the slots.</summary>
    public IReadOnlyList<ToolSummaryFactPresentation> AdditionalFacts { get; }

    /// <summary>The optional supplied status rendered by the accepted shared status component.</summary>
    public RecordStatusPresentation? Status { get; }

    /// <summary>The supplied row-scoped actions.</summary>
    public IReadOnlyList<SharedActionPresentation> Actions { get; }

    /// <summary>The controlled selected presentation fact of the enclosing consumer surface.</summary>
    public bool Selected { get; }

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

    /// <summary>Whether a supplied status is presented.</summary>
    public bool HasStatus => Status is not null;

    /// <summary>Whether a supplied accessible context accompanies the row.</summary>
    public bool HasAccessibleContext => !string.IsNullOrWhiteSpace(AccessibleContext);

    /// <summary>Whether any supplied action is presented.</summary>
    public bool HasActions => VisibleActions.Count > 0;

    /// <summary>Whether a supplied reason accompanies the delegated state surface.</summary>
    public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

    /// <summary>The supplied actions that are presented, in exactly the supplied order.</summary>
    public IReadOnlyList<SharedActionPresentation> VisibleActions =>
        Actions.Where(action => action.Visible).ToList();

    /// <summary>
    /// The supplied facts in the fixed presentation slot order, then the additional supplied facts
    /// in supplied order. Absent slots contribute nothing.
    /// </summary>
    public IReadOnlyList<ToolSummaryFactPresentation> VisibleFacts
    {
        get
        {
            var facts = new List<ToolSummaryFactPresentation>(7 + AdditionalFacts.Count);
            AddIfPresent(facts, Type);
            AddIfPresent(facts, Reference);
            AddIfPresent(facts, Lot);
            AddIfPresent(facts, Machines);
            AddIfPresent(facts, Quantity);
            AddIfPresent(facts, Process);
            AddIfPresent(facts, Context);
            facts.AddRange(AdditionalFacts);

            return facts;
        }
    }

    /// <summary>Whether any supplied fact is presented.</summary>
    public bool HasFacts => VisibleFacts.Count > 0;

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
                "The ready state renders the summary row, not a common-state region.");
        }

        return CommonStateRegionPresentation.Create(
            State, Message!, AccessibleContext ?? ItemKey, Reason, StateActions);
    }

    /// <summary>
    /// Creates the summary-row input, failing closed on every supplied inconsistency before
    /// rendering.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The item key is blank; a supplied fact label or value is blank; the supplied message is
    /// missing for a non-<c>ready</c> state.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">The supplied state is not a defined value.</exception>
    public static ToolSummaryRowPresentation Create(
        CommonState state,
        string itemKey,
        string? accessibleContext = null,
        ToolSummaryFactPresentation? type = null,
        ToolSummaryFactPresentation? reference = null,
        ToolSummaryFactPresentation? lot = null,
        ToolSummaryFactPresentation? machines = null,
        ToolSummaryFactPresentation? quantity = null,
        ToolSummaryFactPresentation? process = null,
        ToolSummaryFactPresentation? context = null,
        IReadOnlyList<ToolSummaryFactPresentation>? additionalFacts = null,
        RecordStatusPresentation? status = null,
        IReadOnlyList<SharedActionPresentation>? actions = null,
        bool selected = false,
        string? message = null,
        string? reason = null,
        IReadOnlyList<SharedActionPresentation>? stateActions = null)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown common state.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(itemKey);

        ValidateFact(type);
        ValidateFact(reference);
        ValidateFact(lot);
        ValidateFact(machines);
        ValidateFact(quantity);
        ValidateFact(process);
        ValidateFact(context);

        foreach (var fact in additionalFacts ?? [])
        {
            ValidateFact(fact);
        }

        if (state != CommonState.Ready)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);
        }

        return new ToolSummaryRowPresentation(
            state,
            itemKey,
            string.IsNullOrWhiteSpace(accessibleContext) ? null : accessibleContext,
            type,
            reference,
            lot,
            machines,
            quantity,
            process,
            context,
            additionalFacts ?? [],
            status,
            actions ?? [],
            selected,
            message,
            reason,
            stateActions ?? []);
    }

    private static void AddIfPresent(
        List<ToolSummaryFactPresentation> facts,
        ToolSummaryFactPresentation? fact)
    {
        if (fact is not null)
        {
            facts.Add(fact);
        }
    }

    private static void ValidateFact(ToolSummaryFactPresentation? fact)
    {
        if (fact is null)
        {
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(fact.Label);
        ArgumentException.ThrowIfNullOrWhiteSpace(fact.Value);
    }
}
