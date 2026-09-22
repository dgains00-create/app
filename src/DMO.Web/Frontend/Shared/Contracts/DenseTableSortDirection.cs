namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The supplied, consumer-owned current sort direction of a <c>DenseDataTable</c> column.
/// </summary>
/// <remarks>
/// Authority: P2-T02 contract §5.5. Controlled presentation only: the component renders the
/// supplied direction as an affordance and never reorders rows.
/// </remarks>
public enum DenseTableSortDirection
{
    /// <summary>Ascending presentation.</summary>
    Ascending,

    /// <summary>Descending presentation.</summary>
    Descending,
}
