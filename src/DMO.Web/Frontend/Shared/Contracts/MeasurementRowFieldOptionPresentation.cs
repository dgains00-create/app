namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// One supplied option of a choice field.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.3.2 (P2-T03 PIN).
/// <para>
/// The option is render-only: the primitive never filters, matches, auto-corrects or reorders
/// supplied options, and the value is opaque consumer presentation data.
/// </para>
/// </remarks>
public sealed record MeasurementRowFieldOptionPresentation
{
    private MeasurementRowFieldOptionPresentation(string value, string label)
    {
        Value = value;
        Label = label;
    }

    /// <summary>The opaque option value, rendered verbatim. Never a canonical identity.</summary>
    public string Value { get; }

    /// <summary>The supplied visible option label.</summary>
    public string Label { get; }

    /// <summary>Creates a supplied option.</summary>
    /// <exception cref="ArgumentException">The value or label is blank.</exception>
    public static MeasurementRowFieldOptionPresentation Create(string value, string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        return new MeasurementRowFieldOptionPresentation(value, label);
    }
}
