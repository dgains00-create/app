namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// A supplied candidate entry presented by the shared picker.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.1.3 (P2-T03 PIN).
/// <para>
/// One candidate is one separately presented entry: supplied candidates are never merged,
/// deduplicated, grouped, collapsed, ranked or compared. The key is <b>opaque</b> consumer
/// presentation data and is never a canonical domain identity, endpoint or address; it is echoed
/// back verbatim on the selection event and is never parsed.
/// </para>
/// <para>
/// The carrier deliberately has <b>no</b> status slot, no icon semantics, no availability flag and
/// no per-candidate action carrier: a candidate's condition is expressed by the consumer as a
/// supplied fact. A candidate the consumer does not want selectable is simply not supplied.
/// </para>
/// </remarks>
public sealed record ToolPickerCandidatePresentation
{
    private ToolPickerCandidatePresentation(
        string key,
        string accessibleContext,
        IReadOnlyList<ToolPickerFactPresentation> facts)
    {
        Key = key;
        AccessibleContext = accessibleContext;
        Facts = facts;
    }

    /// <summary>Opaque candidate key. Never a canonical identity, endpoint or address.</summary>
    public string Key { get; }

    /// <summary>
    /// Supplied human context used to disambiguate the candidate for assistive technology and to
    /// qualify the select control's accessible name. Never derived from supplied fact values.
    /// </summary>
    public string AccessibleContext { get; }

    /// <summary>The supplied labelled facts, in exactly the supplied order.</summary>
    public IReadOnlyList<ToolPickerFactPresentation> Facts { get; }

    /// <summary>Whether any supplied fact accompanies the candidate.</summary>
    public bool HasFacts => Facts.Count > 0;

    /// <summary>Creates a candidate entry.</summary>
    /// <exception cref="ArgumentException">The key or the accessible context is blank.</exception>
    public static ToolPickerCandidatePresentation Create(
        string key,
        string accessibleContext,
        IReadOnlyList<ToolPickerFactPresentation>? facts = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessibleContext);

        return new ToolPickerCandidatePresentation(key, accessibleContext, facts ?? []);
    }
}
