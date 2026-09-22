namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// A supplied labelled display fact rendered by the compact summary row.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.2.2 (P2-T03 PIN).
/// <para>
/// Both the label and the value are supplied and rendered verbatim. The primitive performs no
/// lookup, no resolution, no join, no inference, no derivation, no formatting and no defaulting,
/// and an absent fact is never replaced by a placeholder.
/// </para>
/// </remarks>
public sealed record ToolSummaryFactPresentation
{
    private ToolSummaryFactPresentation(string label, string value)
    {
        Label = label;
        Value = value;
    }

    /// <summary>The supplied visible label. Never blank.</summary>
    public string Label { get; }

    /// <summary>The supplied display value, rendered verbatim. Never blank.</summary>
    public string Value { get; }

    /// <summary>Creates a supplied labelled display fact.</summary>
    /// <exception cref="ArgumentException">The label or value is blank.</exception>
    public static ToolSummaryFactPresentation Create(string label, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new ToolSummaryFactPresentation(label, value);
    }
}
