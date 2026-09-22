namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// A supplied, <b>controlled</b> filter affordance of a <c>DenseDataTable</c>.
/// </summary>
/// <remarks>
/// Authority: P2-T02 contract §5.5. The component renders exactly the supplied controlled
/// value and performs <b>no</b> filtering: it contains no filter operators, no boolean
/// expressions and no query semantics, and supplied filter state never changes the rendered
/// row set or row order.
/// <para>
/// The affordance is rendered <b>without</b> <c>action</c>/<c>method</c>/URL: the consumer owns
/// the form target and the wiring (A1 freeze §3 rule 9). <see cref="Key"/> and option values
/// are opaque consumer presentation carriers.
/// </para>
/// </remarks>
public sealed record DenseTableFilterPresentation
{
    private DenseTableFilterPresentation(
        string key,
        string label,
        string value,
        IReadOnlyList<DenseTableFilterOptionPresentation>? options)
    {
        Key = key;
        Label = label;
        Value = value;
        Options = options;
    }

    /// <summary>Opaque consumer filter key. Never a canonical identity, endpoint or URL.</summary>
    public string Key { get; }

    /// <summary>Mandatory visible filter label.</summary>
    public string Label { get; }

    /// <summary>
    /// Mandatory controlled string value supplied by the consumer. The component pre-fills the
    /// affordance with it and never derives, trims or interprets it.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Optional supplied options. When present the affordance is a select of the supplied
    /// options; when absent it is a text input. Both are pre-filled with the supplied value.
    /// </summary>
    public IReadOnlyList<DenseTableFilterOptionPresentation>? Options { get; }

    /// <summary>Whether the affordance is a select of supplied options.</summary>
    public bool HasOptions => Options is { Count: > 0 };

    /// <summary>The supplied options, or an empty list when none are supplied.</summary>
    public IReadOnlyList<DenseTableFilterOptionPresentation> SuppliedOptions => Options ?? [];

    /// <summary>Creates a controlled filter affordance.</summary>
    /// <exception cref="ArgumentException">The supplied key or label is blank.</exception>
    /// <exception cref="ArgumentNullException">The supplied controlled value is null.</exception>
    public static DenseTableFilterPresentation Create(
        string key,
        string label,
        string value,
        IReadOnlyList<DenseTableFilterOptionPresentation>? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(value);

        return new DenseTableFilterPresentation(key, label, value, options);
    }

    /// <summary>Creates a controlled select filter of the supplied options.</summary>
    public static DenseTableFilterPresentation CreateSelect(
        string key,
        string label,
        string value,
        IReadOnlyList<DenseTableFilterOptionPresentation> options) =>
        Create(key, label, value, options);
}
