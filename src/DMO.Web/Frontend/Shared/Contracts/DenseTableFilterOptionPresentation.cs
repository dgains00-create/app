namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// A supplied option of a controlled <c>DenseDataTable</c> filter.
/// </summary>
/// <remarks>
/// Authority: P2-T02 contract §5.5. The value is a consumer-controlled, <b>opaque</b>
/// presentation carrier: never a canonical identity, never an endpoint, never a URL.
/// </remarks>
public sealed record DenseTableFilterOptionPresentation
{
    private DenseTableFilterOptionPresentation(string value, string label)
    {
        Value = value;
        Label = label;
    }

    /// <summary>Opaque consumer option value.</summary>
    public string Value { get; }

    /// <summary>Visible option label.</summary>
    public string Label { get; }

    /// <summary>Creates a filter option.</summary>
    /// <exception cref="ArgumentException">The supplied value or label is blank.</exception>
    public static DenseTableFilterOptionPresentation Create(string value, string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        return new DenseTableFilterOptionPresentation(value, label);
    }
}
