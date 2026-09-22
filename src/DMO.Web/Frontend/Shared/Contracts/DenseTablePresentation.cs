namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The complete consumer-supplied input of a <c>DenseDataTable</c>.
/// </summary>
/// <remarks>
/// Authority: P2-T02 contract §5 (especially §5.5, §5.8) and A1 freeze §9.
/// <para>
/// The table is a <b>shared presentation primitive</b>: it renders the supplied, ordered column
/// set and the supplied, ordered row set exactly as given, arbitrates selection versus open as
/// two separate behaviours, and delegates every non-<c>ready</c> surface to the accepted P2-T01
/// common-state presentation.
/// </para>
/// <para>
/// It owns <b>no</b> domain semantics and performs <b>no</b> transformation: supplied
/// filter/paging/sort state is rendered and never applied, so the rendered rows are exactly the
/// supplied rows in exactly the supplied order. It fetches nothing, persists nothing,
/// authorizes nothing, counts nothing, resolves no identity and constructs no URL.
/// </para>
/// </remarks>
public sealed record DenseTablePresentation
{
    /// <summary>
    /// The generic A-owned label used as the trailing control cell's header when the consumer
    /// supplies no <see cref="ActionsColumnHeading"/>.
    /// </summary>
    public const string GenericActionsColumnHeading = "Ações";

    /// <summary>
    /// The generic A-owned visible label of the explicit open control rendered when
    /// <see cref="OpenEnabled"/> is true.
    /// </summary>
    public const string GenericOpenControlLabel = "Abrir";

    private DenseTablePresentation(
        CommonState state,
        string caption,
        string regionLabel,
        IReadOnlyList<DenseTableColumnPresentation> columns,
        IReadOnlyList<DenseTableRowPresentation> rows,
        bool selectionEnabled,
        string? selectedKey,
        bool openEnabled,
        string? message,
        string? reason,
        IReadOnlyList<SharedActionPresentation> stateActions,
        string? resultSummary,
        IReadOnlyList<DenseTableFilterPresentation> filters,
        DenseTablePagingPresentation? paging,
        DenseTableSortPresentation? sort,
        string? actionsColumnHeading)
    {
        State = state;
        Caption = caption;
        RegionLabel = regionLabel;
        Columns = columns;
        Rows = rows;
        SelectionEnabled = selectionEnabled;
        SelectedKey = selectedKey;
        OpenEnabled = openEnabled;
        Message = message;
        Reason = reason;
        StateActions = stateActions;
        ResultSummary = resultSummary;
        Filters = filters;
        Paging = paging;
        Sort = sort;
        ActionsColumnHeading = actionsColumnHeading;
    }

    /// <summary>The supplied P2-T01 presentation state.</summary>
    public CommonState State { get; }

    /// <summary>Mandatory accessible caption of the table.</summary>
    public string Caption { get; }

    /// <summary>The supplied accessible region label; defaults to the caption.</summary>
    public string RegionLabel { get; }

    /// <summary>The supplied columns, in authoritative supplied order.</summary>
    public IReadOnlyList<DenseTableColumnPresentation> Columns { get; }

    /// <summary>The supplied rows, in authoritative supplied order.</summary>
    public IReadOnlyList<DenseTableRowPresentation> Rows { get; }

    /// <summary>Whether selection is consumer-enabled. Default <c>false</c>.</summary>
    public bool SelectionEnabled { get; }

    /// <summary>The controlled selected row key; <c>null</c> when nothing is selected.</summary>
    public string? SelectedKey { get; }

    /// <summary>Whether open is consumer-enabled. Default <c>false</c>.</summary>
    public bool OpenEnabled { get; }

    /// <summary>The supplied visible message. Mandatory for every non-<c>ready</c> state.</summary>
    public string? Message { get; }

    /// <summary>Optional supplied reason, programmatically associated when present.</summary>
    public string? Reason { get; }

    /// <summary>Supplied state actions (for example a retry or refresh action).</summary>
    public IReadOnlyList<SharedActionPresentation> StateActions { get; }

    /// <summary>Optional supplied result/metadata text, rendered verbatim and announced politely.</summary>
    public string? ResultSummary { get; }

    /// <summary>Supplied controlled filter affordances, rendered and never applied.</summary>
    public IReadOnlyList<DenseTableFilterPresentation> Filters { get; }

    /// <summary>Supplied consumer-owned paging presentation, rendered and never applied.</summary>
    public DenseTablePagingPresentation? Paging { get; }

    /// <summary>Supplied controlled current sort presentation, rendered and never applied.</summary>
    public DenseTableSortPresentation? Sort { get; }

    /// <summary>
    /// Optional supplied heading of the component-owned trailing control cell. When absent the
    /// generic A-owned label is used.
    /// </summary>
    public string? ActionsColumnHeading { get; }

    /// <summary>The stable P2-T01 CSS token for the supplied state.</summary>
    public string StateToken => CommonStateTraits.CssToken(State);

    /// <summary>Whether the supplied state is <see cref="CommonState.Ready"/>.</summary>
    public bool IsReady => State == CommonState.Ready;

    /// <summary>Whether the supplied state retains the column/header context.</summary>
    public bool RetainsHeaderContext => State == CommonState.Empty;

    /// <summary>Whether the table body is rendered (ready, or a state retaining rows).</summary>
    public bool RendersTable => State is CommonState.Ready or CommonState.Loading or CommonState.Stale;

    /// <summary>
    /// Whether only the accepted P2-T01 state surface is rendered, with no table and therefore
    /// no data body (so a failure can never look like an empty table).
    /// </summary>
    public bool RendersStateSurfaceOnly => !RendersTable && !RetainsHeaderContext;

    /// <summary>Whether the region is busy.</summary>
    public bool IsBusy => CommonStateTraits.IsBusy(State);

    /// <summary>Whether the delegated state surface announces assertively.</summary>
    public bool IsAssertive => CommonStateTraits.IsAssertive(State);

    /// <summary>Whether supplied rows are retained while loading, and labelled stale.</summary>
    public bool RetainsStaleRows => State == CommonState.Loading && HasRows;

    /// <summary>Whether the retained rows accompany the accepted P2-T01 stale surface.</summary>
    public bool ShowsStaleSurface => State == CommonState.Stale;

    /// <summary>
    /// Whether the accepted P2-T01 state surface is rendered: every non-<c>ready</c> state
    /// except the loading state that retains supplied rows, which marks the region busy and
    /// labels each retained row stale instead (contract §7.1).
    /// </summary>
    public bool ShowsStateSurface => !IsReady && !RetainsStaleRows;

    /// <summary>Whether any column is supplied.</summary>
    public bool HasColumns => Columns.Count > 0;

    /// <summary>Whether any row is supplied.</summary>
    public bool HasRows => Rows.Count > 0;

    /// <summary>Whether any supplied row carries a presented action.</summary>
    public bool HasRowActions => Rows.Any(row => row.HasActions);

    /// <summary>Whether any supplied row carries a status.</summary>
    public bool HasRowStatus => Rows.Any(row => row.HasStatus);

    /// <summary>
    /// Whether the component-owned trailing control cell is rendered. Its position is fixed and
    /// independent of width, priority, width hint and row content.
    /// </summary>
    public bool HasControlColumn => HasRowActions || OpenEnabled;

    /// <summary>The heading of the trailing control cell (supplied, else the generic A-owned label).</summary>
    public string ControlColumnHeading =>
        string.IsNullOrWhiteSpace(ActionsColumnHeading) ? GenericActionsColumnHeading : ActionsColumnHeading!;

    /// <summary>Whether supplied controlled filters are presented.</summary>
    public bool HasFilters => Filters.Count > 0;

    /// <summary>Whether a supplied paging presentation is presented.</summary>
    public bool HasPaging => Paging is not null;

    /// <summary>Whether a supplied controlled sort presentation is presented.</summary>
    public bool HasSort => Sort is not null;

    /// <summary>Whether a supplied result summary is presented.</summary>
    public bool HasResultSummary => !string.IsNullOrWhiteSpace(ResultSummary);

    /// <summary>Whether supplied state actions are presented.</summary>
    public bool HasStateActions => StateActions.Count > 0;

    /// <summary>Whether a supplied reason accompanies the delegated state surface.</summary>
    public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

    /// <summary>Whether a supplied message accompanies the state.</summary>
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    /// <summary>
    /// Projects the supplied state onto the accepted P2-T01 common-state carrier so every
    /// non-<c>ready</c> surface is rendered by the accepted P2-T01 partial rather than by any
    /// table-specific state markup.
    /// </summary>
    /// <exception cref="InvalidOperationException">The supplied state is <see cref="CommonState.Ready"/>.</exception>
    public CommonStateRegionPresentation ToStateRegion()
    {
        if (IsReady)
        {
            throw new InvalidOperationException(
                "The ready state renders the table, not a common-state region.");
        }

        return CommonStateRegionPresentation.Create(
            State, Message!, RegionLabel, Reason, StateActions);
    }

    /// <summary>
    /// Creates the table input, failing closed on every supplied inconsistency before rendering.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The caption is blank, a supplied row's cell count differs from the supplied column count,
    /// <paramref name="selectedKey"/> does not reference a supplied row, or
    /// <paramref name="message"/> is missing for a non-<c>ready</c> state.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">The supplied state is not a defined vocabulary value.</exception>
    public static DenseTablePresentation Create(
        CommonState state,
        string caption,
        IReadOnlyList<DenseTableColumnPresentation> columns,
        IReadOnlyList<DenseTableRowPresentation> rows,
        bool selectionEnabled = false,
        string? selectedKey = null,
        bool openEnabled = false,
        string? message = null,
        string? reason = null,
        IReadOnlyList<SharedActionPresentation>? stateActions = null,
        string? resultSummary = null,
        IReadOnlyList<DenseTableFilterPresentation>? filters = null,
        DenseTablePagingPresentation? paging = null,
        DenseTableSortPresentation? sort = null,
        string? actionsColumnHeading = null,
        string? regionLabel = null)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown common state.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(caption);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        foreach (var row in rows)
        {
            if (row.Cells.Count != columns.Count)
            {
                throw new ArgumentException(
                    $"Row '{row.Key}' supplies {row.Cells.Count} cells but {columns.Count} columns were supplied.",
                    nameof(rows));
            }
        }

        if (selectedKey is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(selectedKey);
            if (!selectionEnabled)
            {
                throw new ArgumentException(
                    "A selected key requires selection to be enabled.", nameof(selectedKey));
            }

            if (!rows.Any(row => row.Key == selectedKey))
            {
                throw new ArgumentException(
                    "A selected key must reference a supplied row key.", nameof(selectedKey));
            }
        }

        if (state != CommonState.Ready)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(message);
        }

        if (sort is not null &&
            !columns.Any(column => column.Key == sort.ColumnKey && column.Sortable))
        {
            throw new ArgumentException(
                "A supplied sort must reference a supplied sortable column.", nameof(sort));
        }

        return new DenseTablePresentation(
            state,
            caption,
            string.IsNullOrWhiteSpace(regionLabel) ? caption : regionLabel,
            columns,
            rows,
            selectionEnabled,
            selectedKey,
            openEnabled,
            message,
            reason,
            stateActions ?? [],
            resultSummary,
            filters ?? [],
            paging,
            sort,
            string.IsNullOrWhiteSpace(actionsColumnHeading) ? null : actionsColumnHeading);
    }

    /// <summary>Creates the ready table presentation.</summary>
    public static DenseTablePresentation Ready(
        string caption,
        IReadOnlyList<DenseTableColumnPresentation> columns,
        IReadOnlyList<DenseTableRowPresentation> rows,
        bool selectionEnabled = false,
        string? selectedKey = null,
        bool openEnabled = false,
        string? resultSummary = null,
        IReadOnlyList<DenseTableFilterPresentation>? filters = null,
        DenseTablePagingPresentation? paging = null,
        DenseTableSortPresentation? sort = null,
        string? actionsColumnHeading = null,
        string? regionLabel = null) =>
        Create(state: CommonState.Ready, caption, columns, rows,
            selectionEnabled, selectedKey, openEnabled, message: null, reason: null, stateActions: null,
            resultSummary, filters, paging, sort, actionsColumnHeading, regionLabel);

    /// <summary>Creates the honest empty state, retaining the supplied column/header context.</summary>
    public static DenseTablePresentation Empty(
        string caption,
        string message,
        IReadOnlyList<DenseTableColumnPresentation> columns,
        IReadOnlyList<SharedActionPresentation>? stateActions = null,
        string? regionLabel = null) =>
        Create(CommonState.Empty, caption, columns, [], message: message,
            stateActions: stateActions, regionLabel: regionLabel);

    /// <summary>Creates the loading state, optionally retaining supplied rows (each labelled stale).</summary>
    public static DenseTablePresentation Loading(
        string caption,
        string message,
        IReadOnlyList<DenseTableColumnPresentation> columns,
        IReadOnlyList<DenseTableRowPresentation>? retainedRows = null,
        string? regionLabel = null) =>
        Create(CommonState.Loading, caption, columns, retainedRows ?? [], message: message,
            regionLabel: regionLabel);

    /// <summary>Creates the lookup-failed state with its supplied retry.</summary>
    public static DenseTablePresentation LookupFailed(
        string caption,
        string message,
        SharedActionPresentation? retry = null,
        string? reason = null,
        string? regionLabel = null) =>
        Create(CommonState.LookupFailed, caption, [], [], message: message, reason: reason,
            stateActions: retry is null ? null : [retry], regionLabel: regionLabel);

    /// <summary>Creates the non-permission unavailable state with its supplied reason.</summary>
    public static DenseTablePresentation Unavailable(
        string caption,
        string message,
        string? reason = null,
        IReadOnlyList<SharedActionPresentation>? stateActions = null,
        string? regionLabel = null) =>
        Create(CommonState.Unavailable, caption, [], [], message: message, reason: reason,
            stateActions: stateActions, regionLabel: regionLabel);

    /// <summary>Creates the permission-denied state with its supplied reason.</summary>
    public static DenseTablePresentation PermissionDenied(
        string caption,
        string message,
        string? reason = null,
        string? regionLabel = null) =>
        Create(CommonState.PermissionDenied, caption, [], [], message: message, reason: reason,
            regionLabel: regionLabel);

    /// <summary>Creates the stale state, retaining supplied rows plus the P2-T01 stale surface.</summary>
    public static DenseTablePresentation Stale(
        string caption,
        string message,
        IReadOnlyList<DenseTableColumnPresentation> columns,
        IReadOnlyList<DenseTableRowPresentation> rows,
        IReadOnlyList<SharedActionPresentation>? stateActions = null,
        string? reason = null,
        string? regionLabel = null) =>
        Create(CommonState.Stale, caption, columns, rows, message: message, reason: reason,
            stateActions: stateActions, regionLabel: regionLabel);

    /// <summary>Creates the conflict state with its supplied summary/recovery.</summary>
    public static DenseTablePresentation Conflict(
        string caption,
        string message,
        IReadOnlyList<SharedActionPresentation>? recovery = null,
        string? reason = null,
        string? regionLabel = null) =>
        Create(CommonState.Conflict, caption, [], [], message: message, reason: reason,
            stateActions: recovery, regionLabel: regionLabel);
}
