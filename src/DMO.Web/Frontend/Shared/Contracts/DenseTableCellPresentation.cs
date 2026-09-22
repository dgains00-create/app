namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// A single supplied cell of a <c>DenseDataTable</c> row.
/// </summary>
/// <remarks>
/// Authority: P2-T02 contract §5.3 (Q3 accepted default). The cell carries <b>plain supplied
/// text only</b>, rendered Razor-encoded as visible text. The component never accepts
/// consumer-rendered markup, never parses and never interprets the text, and display text is
/// never treated as identity.
/// </remarks>
public sealed record DenseTableCellPresentation
{
    private DenseTableCellPresentation(string text)
    {
        Text = text;
    }

    /// <summary>The supplied plain cell text. Always present and never blank.</summary>
    public string Text { get; }

    /// <summary>Creates a cell from supplied plain text.</summary>
    /// <exception cref="ArgumentException">The supplied text is blank.</exception>
    public static DenseTableCellPresentation Create(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return new DenseTableCellPresentation(text);
    }
}
