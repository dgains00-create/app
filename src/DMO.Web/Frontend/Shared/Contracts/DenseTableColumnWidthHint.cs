namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// Advisory, token-driven width hint for a <c>DenseDataTable</c> column.
/// </summary>
/// <remarks>
/// Authority: P2-T02 contract §5.2. The hint supplies an advisory minimum only. It is
/// <b>never</b> authority to hide, reorder, collapse or convert a column at any width
/// (contract §4.1.5), and it is not a pixel contract.
/// </remarks>
public enum DenseTableColumnWidthHint
{
    /// <summary>Content-driven width (default); no advisory minimum.</summary>
    Auto,

    /// <summary>Advisory compact minimum for short values.</summary>
    Compact,

    /// <summary>Advisory wide minimum for longer values.</summary>
    Wide,
}
