namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// Supplied display alignment for a <c>DenseDataTable</c> column.
/// </summary>
/// <remarks>
/// Authority: P2-T02 contract §5.2. Presentation only: the component renders exactly the
/// supplied alignment and invents none.
/// </remarks>
public enum DenseTableColumnAlignment
{
    /// <summary>Start-aligned (default).</summary>
    Start,

    /// <summary>Centre-aligned.</summary>
    Center,

    /// <summary>End-aligned.</summary>
    End,
}
