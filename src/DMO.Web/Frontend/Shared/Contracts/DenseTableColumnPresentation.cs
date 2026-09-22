namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// A supplied column descriptor of a <c>DenseDataTable</c>.
/// </summary>
/// <remarks>
/// Authority: P2-T02 contract §5.2 and A1 freeze §9 / §1A.
/// <para>
/// The rendered column set and order are exactly the supplied list order at every desktop
/// width. <see cref="Priority"/> is presentation metadata only and is <b>never</b> authority
/// to hide, reorder, collapse or convert a column; <see cref="WidthHint"/> is an advisory
/// token-driven minimum only. P2-T02 hides no column at any width, so the carrier has no
/// required/optional flag and no hidden-column behaviour.
/// </para>
/// <para>
/// <see cref="Key"/> is an opaque consumer key: never a canonical identity, never an endpoint
/// and never a URL. It is used for header association, sort hooks and event carriers only.
/// </para>
/// </remarks>
public sealed record DenseTableColumnPresentation
{
    private DenseTableColumnPresentation(
        string key,
        string heading,
        string? accessibleHeading,
        DenseTableColumnAlignment alignment,
        int? priority,
        bool sortable,
        DenseTableColumnWidthHint widthHint)
    {
        Key = key;
        Heading = heading;
        AccessibleHeading = accessibleHeading;
        Alignment = alignment;
        Priority = priority;
        Sortable = sortable;
        WidthHint = widthHint;
    }

    /// <summary>Opaque consumer column key. Never a canonical identity, endpoint or URL.</summary>
    public string Key { get; }

    /// <summary>Mandatory visible header text.</summary>
    public string Heading { get; }

    /// <summary>Optional supplied accessible header name; the <see cref="Heading"/> is used when absent.</summary>
    public string? AccessibleHeading { get; }

    /// <summary>Supplied display alignment. The component invents none.</summary>
    public DenseTableColumnAlignment Alignment { get; }

    /// <summary>
    /// Optional presentation metadata, rendered for consumers as <c>data-dmo-column-priority</c>.
    /// Never authority to hide, reorder, collapse or convert the column.
    /// </summary>
    public int? Priority { get; }

    /// <summary>Whether the component renders a sort affordance for this column. It never sorts.</summary>
    public bool Sortable { get; }

    /// <summary>Advisory token-driven width hint. Never authority to hide or reorder.</summary>
    public DenseTableColumnWidthHint WidthHint { get; }

    /// <summary>Whether a supplied accessible header name accompanies the visible heading.</summary>
    public bool HasAccessibleHeading => !string.IsNullOrWhiteSpace(AccessibleHeading);

    /// <summary>
    /// The accessible name of the header: the supplied accessible heading, else the visible
    /// heading (contract §5.2). The visible heading text is never replaced by it.
    /// </summary>
    public string AccessibleHeaderName => HasAccessibleHeading ? AccessibleHeading! : Heading;

    /// <summary>Whether supplied presentation priority metadata is present.</summary>
    public bool HasPriority => Priority.HasValue;

    /// <summary>The stable CSS token for the supplied alignment (kebab-case).</summary>
    public string AlignmentToken => Alignment switch
    {
        DenseTableColumnAlignment.Start => "start",
        DenseTableColumnAlignment.Center => "center",
        DenseTableColumnAlignment.End => "end",
        _ => "start",
    };

    /// <summary>The stable CSS token for the supplied width hint (kebab-case).</summary>
    public string WidthHintToken => WidthHint switch
    {
        DenseTableColumnWidthHint.Auto => "auto",
        DenseTableColumnWidthHint.Compact => "compact",
        DenseTableColumnWidthHint.Wide => "wide",
        _ => "auto",
    };

    /// <summary>Creates a column descriptor.</summary>
    /// <exception cref="ArgumentException">The supplied key or heading is blank.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A supplied enum value is not defined.</exception>
    public static DenseTableColumnPresentation Create(
        string key,
        string heading,
        string? accessibleHeading = null,
        DenseTableColumnAlignment alignment = DenseTableColumnAlignment.Start,
        int? priority = null,
        bool sortable = false,
        DenseTableColumnWidthHint widthHint = DenseTableColumnWidthHint.Auto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(heading);

        if (!Enum.IsDefined(alignment))
        {
            throw new ArgumentOutOfRangeException(
                nameof(alignment), alignment, "Unknown column alignment.");
        }

        if (!Enum.IsDefined(widthHint))
        {
            throw new ArgumentOutOfRangeException(
                nameof(widthHint), widthHint, "Unknown column width hint.");
        }

        return new DenseTableColumnPresentation(
            key, heading, accessibleHeading, alignment, priority, sortable, widthHint);
    }

    /// <summary>Creates a sortable column descriptor.</summary>
    public static DenseTableColumnPresentation CreateSortable(
        string key,
        string heading,
        string? accessibleHeading = null,
        DenseTableColumnAlignment alignment = DenseTableColumnAlignment.Start,
        int? priority = null,
        DenseTableColumnWidthHint widthHint = DenseTableColumnWidthHint.Auto) =>
        Create(key, heading, accessibleHeading, alignment, priority, sortable: true, widthHint);
}
