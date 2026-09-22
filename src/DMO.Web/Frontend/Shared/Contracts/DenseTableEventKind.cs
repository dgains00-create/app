namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The frozen generic <c>DenseDataTable</c> presentation-event vocabulary.
/// </summary>
/// <remarks>
/// Authority: P2-T02 contract §5.5 and A1 freeze §9 "Outputs/events". The component
/// <b>raises</b> these events and <b>never acts</b> on them: no event carries a URL, a route or
/// a resolved target, and no event triggers a domain mutation.
/// </remarks>
public enum DenseTableEventKind
{
    /// <summary>A row became the selected row. Selection state only.</summary>
    RowSelected,

    /// <summary>An open was requested for a row. The consumer resolves the target.</summary>
    OpenRequested,

    /// <summary>A supplied row action was invoked, carrying opaque row/action keys.</summary>
    ActionInvoked,

    /// <summary>A supplied controlled filter changed.</summary>
    FilterChanged,

    /// <summary>A supplied paging control changed.</summary>
    PageChanged,

    /// <summary>A sort was requested for a supplied sortable column.</summary>
    SortRequested,
}
