namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// A generic <c>DenseDataTable</c> presentation event carrying only opaque keys/carriers.
/// </summary>
/// <remarks>
/// Authority: P2-T02 contract §5.5. Distinct from the accepted P2-T01
/// <see cref="SharedActionPresentation"/>, which is the action <i>carrier</i>; this type is the
/// frozen <i>event</i> raised when a supplied action or interaction is invoked.
/// <para>
/// No member is a canonical identity, an endpoint or a URL, and no member returns a
/// <see cref="Uri"/>, route or navigation target. The component raises the event and never
/// invokes a domain transition (A1 freeze §3 rule 9).
/// </para>
/// </remarks>
public sealed record DenseTableEvent
{
    private DenseTableEvent(
        DenseTableEventKind kind,
        string? rowKey,
        string? actionKey,
        string? filterKey,
        string? filterValue,
        string? pageCarrier,
        string? sortColumnKey,
        DenseTableSortDirection? sortDirection)
    {
        Kind = kind;
        RowKey = rowKey;
        ActionKey = actionKey;
        FilterKey = filterKey;
        FilterValue = filterValue;
        PageCarrier = pageCarrier;
        SortColumnKey = sortColumnKey;
        SortDirection = sortDirection;
    }

    /// <summary>The frozen event kind.</summary>
    public DenseTableEventKind Kind { get; }

    /// <summary>Opaque row key for row-scoped events; otherwise <c>null</c>.</summary>
    public string? RowKey { get; }

    /// <summary>Opaque action key for <see cref="DenseTableEventKind.ActionInvoked"/>; otherwise <c>null</c>.</summary>
    public string? ActionKey { get; }

    /// <summary>Opaque filter key for <see cref="DenseTableEventKind.FilterChanged"/>; otherwise <c>null</c>.</summary>
    public string? FilterKey { get; }

    /// <summary>Controlled filter value carrier for <see cref="DenseTableEventKind.FilterChanged"/>.</summary>
    public string? FilterValue { get; }

    /// <summary>Opaque page carrier for <see cref="DenseTableEventKind.PageChanged"/>; otherwise <c>null</c>.</summary>
    public string? PageCarrier { get; }

    /// <summary>Opaque sort column key for <see cref="DenseTableEventKind.SortRequested"/>; otherwise <c>null</c>.</summary>
    public string? SortColumnKey { get; }

    /// <summary>Supplied sort direction for <see cref="DenseTableEventKind.SortRequested"/>; otherwise <c>null</c>.</summary>
    public DenseTableSortDirection? SortDirection { get; }

    /// <summary>The stable kebab-case token for the event kind (used as the DOM hook name).</summary>
    public string KindToken => Kind switch
    {
        DenseTableEventKind.RowSelected => "row-selected",
        DenseTableEventKind.OpenRequested => "open-requested",
        DenseTableEventKind.ActionInvoked => "action-invoked",
        DenseTableEventKind.FilterChanged => "filter-changed",
        DenseTableEventKind.PageChanged => "page-changed",
        DenseTableEventKind.SortRequested => "sort-requested",
        _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Unknown event kind."),
    };

    /// <summary>Raises the selection event for an opaque row key.</summary>
    /// <exception cref="ArgumentException">The supplied row key is blank.</exception>
    public static DenseTableEvent RowSelected(string rowKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rowKey);

        return new DenseTableEvent(DenseTableEventKind.RowSelected, rowKey, null, null, null, null, null, null);
    }

    /// <summary>Raises the open-requested event for an opaque row key.</summary>
    /// <exception cref="ArgumentException">The supplied row key is blank.</exception>
    public static DenseTableEvent OpenRequested(string rowKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rowKey);

        return new DenseTableEvent(DenseTableEventKind.OpenRequested, rowKey, null, null, null, null, null, null);
    }

    /// <summary>Raises the action-invoked event for opaque row/action keys.</summary>
    /// <exception cref="ArgumentException">The supplied row or action key is blank.</exception>
    public static DenseTableEvent ActionInvoked(string rowKey, string actionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rowKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionKey);

        return new DenseTableEvent(DenseTableEventKind.ActionInvoked, rowKey, actionKey, null, null, null, null, null);
    }

    /// <summary>Raises the controlled filter-changed event.</summary>
    /// <exception cref="ArgumentException">The supplied filter key is blank.</exception>
    /// <exception cref="ArgumentNullException">The supplied filter value is null.</exception>
    public static DenseTableEvent FilterChanged(string filterKey, string filterValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filterKey);
        ArgumentNullException.ThrowIfNull(filterValue);

        return new DenseTableEvent(DenseTableEventKind.FilterChanged, null, null, filterKey, filterValue, null, null, null);
    }

    /// <summary>Raises the paging event with an opaque page carrier.</summary>
    /// <exception cref="ArgumentException">The supplied page carrier is blank.</exception>
    public static DenseTableEvent PageChanged(string pageCarrier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageCarrier);

        return new DenseTableEvent(DenseTableEventKind.PageChanged, null, null, null, null, pageCarrier, null, null);
    }

    /// <summary>Raises the sort-requested event for a supplied sortable column.</summary>
    /// <exception cref="ArgumentException">The supplied column key is blank.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The supplied direction is not defined.</exception>
    public static DenseTableEvent SortRequested(string sortColumnKey, DenseTableSortDirection direction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sortColumnKey);
        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown sort direction.");
        }

        return new DenseTableEvent(
            DenseTableEventKind.SortRequested, null, null, null, null, null, sortColumnKey, direction);
    }

    /// <summary>Whether the event is row-scoped.</summary>
    public bool IsRowScoped => RowKey is not null;
}
