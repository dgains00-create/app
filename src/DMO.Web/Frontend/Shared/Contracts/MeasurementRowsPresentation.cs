namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The complete consumer-supplied input of the repeated-row primitive.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.3 (especially §3.3.2, §3.3.3, §3.3.4, §3.3.6).
/// <para>
/// The primitive is a <b>generic repeated-row mechanism</b>: it renders the supplied ordered rows,
/// enforces the configured minimum, exposes generic add/remove and the supplied validation
/// <b>display</b>, and delegates every non-<c>ready</c> surface to the accepted common-state
/// presentation.
/// </para>
/// <para>
/// It defines <b>no</b> field list, no field naming, no formula, no tolerance, no nominal value, no
/// capacity, no weight, no unit and no validation rule. Consumers own row contents entirely; the
/// primitive never computes a verdict and never rejects a supplied value.
/// </para>
/// <para>
/// <see cref="StructuralMutationAllowed"/> and <see cref="State"/> are two supplied views of the
/// same consumer decision and must be kept consistent by the consumer, as pinned by the contract's
/// per-state table: <c>saving</c>, <c>submitting</c>, <c>unavailable</c> and
/// <c>permission-denied</c> disable structural mutation, and the associated supplied reason is
/// always the one exposed.
/// </para>
/// </remarks>
public sealed record MeasurementRowsPresentation
{
    /// <summary>The generic A-owned visible label of the add control.</summary>
    public const string GenericAddLabel = "Adicionar linha";

    /// <summary>The generic A-owned visible label of a row's remove control.</summary>
    public const string GenericRemoveLabel = "Remover";

    private MeasurementRowsPresentation(
        CommonState state,
        string regionLabel,
        IReadOnlyList<MeasurementRowPresentation> rows,
        int minimumRowCount,
        string? minimumViolationReason,
        bool addEnabled,
        string? addDisabledReason,
        bool removeEnabled,
        string? removeDisabledReason,
        bool structuralMutationAllowed,
        string? structuralMutationReason,
        string? message,
        string? reason,
        IReadOnlyList<SharedActionPresentation> stateActions)
    {
        State = state;
        RegionLabel = regionLabel;
        Rows = rows;
        MinimumRowCount = minimumRowCount;
        MinimumViolationReason = minimumViolationReason;
        AddEnabled = addEnabled;
        AddDisabledReason = addDisabledReason;
        RemoveEnabled = removeEnabled;
        RemoveDisabledReason = removeDisabledReason;
        StructuralMutationAllowed = structuralMutationAllowed;
        StructuralMutationReason = structuralMutationReason;
        Message = message;
        Reason = reason;
        StateActions = stateActions;
    }

    /// <summary>The supplied common presentation state.</summary>
    public CommonState State { get; }

    /// <summary>The supplied accessible name of the rows region.</summary>
    public string RegionLabel { get; }

    /// <summary>The supplied controlled ordered rows; supplied order is authoritative.</summary>
    public IReadOnlyList<MeasurementRowPresentation> Rows { get; }

    /// <summary>The configurable minimum row count, including the at-least-one case.</summary>
    public int MinimumRowCount { get; }

    /// <summary>The supplied reason exposed when removal would violate the minimum.</summary>
    public string? MinimumViolationReason { get; }

    /// <summary>The supplied add availability.</summary>
    public bool AddEnabled { get; }

    /// <summary>The supplied reason exposed when add is unavailable.</summary>
    public string? AddDisabledReason { get; }

    /// <summary>The supplied per-row remove availability.</summary>
    public bool RemoveEnabled { get; }

    /// <summary>The supplied reason exposed when removal is unavailable.</summary>
    public string? RemoveDisabledReason { get; }

    /// <summary>The supplied structural-mutation availability, derived by the consumer from its state.</summary>
    public bool StructuralMutationAllowed { get; }

    /// <summary>The supplied reason exposed when structural mutation is unavailable.</summary>
    public string? StructuralMutationReason { get; }

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

    /// <summary>Whether a consumer-owned save or transition is pending.</summary>
    public bool IsPending => State is CommonState.Saving or CommonState.Submitting;

    /// <summary>
    /// Whether structural mutation is unavailable: either the consumer said so, or the supplied
    /// state is one of the four states that disable it (contract §3.3.6).
    /// </summary>
    public bool StructuralMutationBlocked =>
        !StructuralMutationAllowed ||
        State is CommonState.Saving
            or CommonState.Submitting
            or CommonState.Unavailable
            or CommonState.PermissionDenied;

    /// <summary>The supplied reason exposed while structural mutation is unavailable.</summary>
    public string? StructuralReason => StructuralMutationReason ?? Reason ?? Message;

    /// <summary>Whether the supplied rows are rendered in this state (contract §3.3.6).</summary>
    public bool RendersRows =>
        State is CommonState.Ready
            or CommonState.Saving
            or CommonState.Submitting
            or CommonState.Unavailable
            or CommonState.PermissionDenied
            or CommonState.Stale
            or CommonState.Conflict;

    /// <summary>Every non-<c>ready</c> surface is rendered by the accepted state region.</summary>
    public bool ShowsStateSurface => !IsReady;

    /// <summary>Whether the add control is presented in this state (contract §3.3.6).</summary>
    public bool ShowsAddControl => State is not (CommonState.Loading or CommonState.LookupFailed);

    /// <summary>Whether the add control can be invoked.</summary>
    public bool AddControlEnabled => AddEnabled && !StructuralMutationBlocked;

    /// <summary>The supplied reason exposed with a disabled add control; never invented.</summary>
    public string? AddControlDisabledReason =>
        !AddEnabled ? AddDisabledReason : StructuralMutationBlocked ? StructuralReason : null;

    /// <summary>Whether the supplied rows and their values are rendered read-only.</summary>
    public bool FieldsReadOnly => StructuralMutationBlocked;

    /// <summary>Whether any supplied row is presented.</summary>
    public bool HasRows => Rows.Count > 0;

    /// <summary>Whether a supplied reason accompanies the delegated state surface.</summary>
    public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

    /// <summary>
    /// Whether the row's remove control can be invoked: supplied availability, structural
    /// availability and the minimum must all permit the removal.
    /// </summary>
    public bool RemoveControlEnabled =>
        RemoveEnabled && !StructuralMutationBlocked && Rows.Count > MinimumRowCount;

    /// <summary>
    /// The visible reason exposed with a disabled remove control, with the pinned precedence:
    /// structural unavailability, then a violated minimum, then supplied per-row availability.
    /// Never invented.
    /// </summary>
    public string? RemovalDisabledReason
    {
        get
        {
            if (StructuralMutationBlocked)
            {
                return StructuralReason;
            }

            return Rows.Count <= MinimumRowCount ? MinimumViolationReason : RemoveDisabledReason;
        }
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
                "The ready state renders the rows, not a common-state region.");
        }

        return CommonStateRegionPresentation.Create(
            State, Message!, RegionLabel, Reason, StateActions);
    }

    /// <summary>
    /// Creates the rows input, failing closed on every supplied inconsistency before rendering.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The region label is blank; the minimum is negative; fewer rows than the minimum are supplied;
    /// the minimum violation reason is missing while the minimum is at least one; a row key is blank
    /// or duplicated; a row context is blank; a supplied availability reason is missing; or the
    /// message is missing for a non-<c>ready</c> state.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">The supplied state is not a defined value.</exception>
    public static MeasurementRowsPresentation Create(
        CommonState state,
        string regionLabel,
        int minimumRowCount,
        IReadOnlyList<MeasurementRowPresentation>? rows = null,
        string? minimumViolationReason = null,
        bool addEnabled = true,
        string? addDisabledReason = null,
        bool removeEnabled = true,
        string? removeDisabledReason = null,
        bool structuralMutationAllowed = true,
        string? structuralMutationReason = null,
        string? message = null,
        string? reason = null,
        IReadOnlyList<SharedActionPresentation>? stateActions = null)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown common state.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(regionLabel);

        if (minimumRowCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumRowCount), minimumRowCount, "The minimum row count cannot be negative.");
        }

        var suppliedRows = rows ?? [];

        if (suppliedRows.Count < minimumRowCount)
        {
            throw new ArgumentException(
                "Fewer rows were supplied than the configured minimum.", nameof(rows));
        }

        if (minimumRowCount >= 1)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(minimumViolationReason);
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in suppliedRows)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(row.Key);
            ArgumentException.ThrowIfNullOrWhiteSpace(row.AccessibleContext);

            if (!seen.Add(row.Key))
            {
                throw new ArgumentException("Supplied row keys must be unique.", nameof(rows));
            }
        }

        if (!addEnabled)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(addDisabledReason);
        }

        if (!removeEnabled)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(removeDisabledReason);
        }

        if (!structuralMutationAllowed)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(structuralMutationReason);
        }

        if (state != CommonState.Ready)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);
        }

        return new MeasurementRowsPresentation(
            state,
            regionLabel,
            suppliedRows,
            minimumRowCount,
            minimumViolationReason,
            addEnabled,
            addDisabledReason,
            removeEnabled,
            removeDisabledReason,
            structuralMutationAllowed,
            structuralMutationReason,
            message,
            reason,
            stateActions ?? []);
    }
}
