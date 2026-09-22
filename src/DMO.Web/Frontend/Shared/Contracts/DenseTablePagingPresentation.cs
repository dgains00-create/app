namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// Supplied, <b>consumer-owned</b> paging presentation for a <c>DenseDataTable</c>.
/// </summary>
/// <remarks>
/// Authority: P2-T02 contract §5.5. The component performs <b>no</b> page arithmetic, no
/// result counting and no row transformation: it renders the supplied page label verbatim and
/// the supplied previous/next actions.
/// <para>
/// Previous/next are accepted P2-T01 <see cref="SharedActionPresentation"/> carriers, so a
/// disabled one already requires a supplied reason.
/// </para>
/// </remarks>
public sealed record DenseTablePagingPresentation
{
    private DenseTablePagingPresentation(
        string pageLabel,
        SharedActionPresentation? previous,
        SharedActionPresentation? next)
    {
        PageLabel = pageLabel;
        Previous = previous;
        Next = next;
    }

    /// <summary>
    /// Mandatory supplied page display text, computed by the consumer. Rendered verbatim; the
    /// component never derives a page indicator.
    /// </summary>
    public string PageLabel { get; }

    /// <summary>Optional supplied previous-page action.</summary>
    public SharedActionPresentation? Previous { get; }

    /// <summary>Optional supplied next-page action.</summary>
    public SharedActionPresentation? Next { get; }

    /// <summary>Whether a previous-page action is supplied.</summary>
    public bool HasPrevious => Previous is not null;

    /// <summary>Whether a next-page action is supplied.</summary>
    public bool HasNext => Next is not null;

    /// <summary>Whether any supplied paging control is presented.</summary>
    public bool HasControls => IsPresented(Previous) || IsPresented(Next);

    /// <summary>Creates the supplied paging presentation.</summary>
    /// <exception cref="ArgumentException">The supplied page label is blank.</exception>
    public static DenseTablePagingPresentation Create(
        string pageLabel,
        SharedActionPresentation? previous = null,
        SharedActionPresentation? next = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageLabel);

        return new DenseTablePagingPresentation(pageLabel, previous, next);
    }

    private static bool IsPresented(SharedActionPresentation? action) => action is { Visible: true };
}
