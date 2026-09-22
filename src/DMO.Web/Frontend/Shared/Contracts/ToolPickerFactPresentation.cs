namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// A supplied labelled display fact carried by a shared presentation primitive.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.1.2 (P2-T03 PIN).
/// <para>
/// The fact is <b>presentation only</b>: both the label and the value are supplied by the consumer
/// and rendered verbatim. The primitive never parses, compares, reformats or derives anything from
/// them, and an omitted fact is simply not supplied — it is never replaced by a placeholder.
/// </para>
/// </remarks>
public sealed record ToolPickerFactPresentation
{
    private ToolPickerFactPresentation(string label, string value)
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
    public static ToolPickerFactPresentation Create(string label, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new ToolPickerFactPresentation(label, value);
    }
}
