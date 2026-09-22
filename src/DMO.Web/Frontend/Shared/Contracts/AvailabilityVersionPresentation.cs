namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// An opaque, consumer-supplied version item presented by <c>AvailabilityState</c>.
/// </summary>
/// <remarks>
/// Authority: freeze §11 ("optional versions as opaque presentation items") and §2 (opaque keys
/// are consumer-controlled frontend carrier data and must never be declared a canonical
/// identity). The key is a frontend-only selection key; the component defines no version
/// semantics.
/// </remarks>
public sealed record AvailabilityVersionPresentation
{
    private AvailabilityVersionPresentation(string key, string label, bool selected)
    {
        Key = key;
        Label = label;
        Selected = selected;
    }

    /// <summary>Opaque frontend-only version key. Never a canonical identity.</summary>
    public string Key { get; }

    /// <summary>Visible version label.</summary>
    public string Label { get; }

    /// <summary>Whether the consumer marked this version as currently selected.</summary>
    public bool Selected { get; }

    /// <summary>Creates a version presentation item.</summary>
    /// <exception cref="ArgumentException">The key or label is blank.</exception>
    public static AvailabilityVersionPresentation Create(string key, string label, bool selected = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        return new AvailabilityVersionPresentation(key, label, selected);
    }
}
