namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The supplied, <b>controlled</b> current sort presentation of a <c>DenseDataTable</c>.
/// </summary>
/// <remarks>
/// Authority: P2-T02 contract §5.5. This is controlled presentation only: the component renders
/// the affordance and the current direction programmatically, and <b>never</b> reorders rows and
/// never sorts. Sorting is owned by the consumer.
/// </remarks>
public sealed record DenseTableSortPresentation
{
    private DenseTableSortPresentation(string columnKey, DenseTableSortDirection direction)
    {
        ColumnKey = columnKey;
        Direction = direction;
    }

    /// <summary>
    /// Opaque consumer column key. Must reference a supplied sortable column; otherwise the
    /// presentation fails before rendering.
    /// </summary>
    public string ColumnKey { get; }

    /// <summary>The supplied current sort direction. Rendered programmatically only.</summary>
    public DenseTableSortDirection Direction { get; }

    /// <summary>The stable <c>aria-sort</c> token for the supplied direction.</summary>
    public string AriaSortToken => Direction switch
    {
        DenseTableSortDirection.Ascending => "ascending",
        DenseTableSortDirection.Descending => "descending",
        _ => "ascending",
    };

    /// <summary>The stable CSS/event token for the supplied direction (kebab-case).</summary>
    public string DirectionToken => Direction switch
    {
        DenseTableSortDirection.Ascending => "ascending",
        DenseTableSortDirection.Descending => "descending",
        _ => "ascending",
    };

    /// <summary>Creates the supplied controlled sort presentation.</summary>
    /// <exception cref="ArgumentException">The supplied column key is blank.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The supplied direction is not defined.</exception>
    public static DenseTableSortPresentation Create(
        string columnKey,
        DenseTableSortDirection direction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnKey);
        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown sort direction.");
        }

        return new DenseTableSortPresentation(columnKey, direction);
    }

    /// <summary>Creates an ascending supplied sort presentation.</summary>
    public static DenseTableSortPresentation Ascending(string columnKey) =>
        Create(columnKey, DenseTableSortDirection.Ascending);

    /// <summary>Creates a descending supplied sort presentation.</summary>
    public static DenseTableSortPresentation Descending(string columnKey) =>
        Create(columnKey, DenseTableSortDirection.Descending);
}
