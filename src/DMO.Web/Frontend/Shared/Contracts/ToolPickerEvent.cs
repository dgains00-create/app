namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// A frozen generic presentation event raised by the shared picker.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.1.10.
/// <para>
/// <see cref="OriginToken"/> is carried <b>verbatim</b> on every kind, so the consumer's
/// origin-state carrier survives the roundtrip unchanged (including whitespace and non-ASCII). It
/// is opaque presentation data: never parsed, trimmed, normalized or interpreted, and never used to
/// build an address, route or endpoint.
/// </para>
/// <para>
/// Every other payload member is present only for the kind it belongs to; no member carries a
/// canonical identity or a resolved target.
/// </para>
/// </remarks>
public sealed record ToolPickerEvent
{
    private ToolPickerEvent(
        ToolPickerEventKind kind,
        string? originToken,
        string? candidateKey,
        string? query,
        string? actionKey)
    {
        Kind = kind;
        OriginToken = originToken;
        CandidateKey = candidateKey;
        Query = query;
        ActionKey = actionKey;
    }

    /// <summary>The frozen event kind.</summary>
    public ToolPickerEventKind Kind { get; }

    /// <summary>The supplied opaque origin token, echoed verbatim.</summary>
    public string? OriginToken { get; }

    /// <summary>The opaque candidate key; present only for <see cref="ToolPickerEventKind.CandidateSelected"/>.</summary>
    public string? CandidateKey { get; }

    /// <summary>The controlled query; present only for <see cref="ToolPickerEventKind.SearchRequested"/>.</summary>
    public string? Query { get; }

    /// <summary>The opaque action key; present for create, cancel and retry requests.</summary>
    public string? ActionKey { get; }

    /// <summary>Whether the event carries an opaque candidate key.</summary>
    public bool HasCandidateKey => CandidateKey is not null;

    /// <summary>Whether the event carries the controlled query.</summary>
    public bool HasQuery => Query is not null;

    /// <summary>Whether the event carries an opaque action key.</summary>
    public bool HasActionKey => ActionKey is not null;

    /// <summary>Creates the search request carrying the current controlled query.</summary>
    public static ToolPickerEvent SearchRequested(string? originToken, string? query) =>
        new(ToolPickerEventKind.SearchRequested, originToken, null, query ?? string.Empty, null);

    /// <summary>Creates the candidate-selection event carrying only the opaque candidate key.</summary>
    /// <exception cref="ArgumentException">The supplied candidate key is blank.</exception>
    public static ToolPickerEvent CandidateSelected(string? originToken, string candidateKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(candidateKey);

        return new(ToolPickerEventKind.CandidateSelected, originToken, candidateKey, null, null);
    }

    /// <summary>Creates the create request carrying only the opaque action key.</summary>
    /// <exception cref="ArgumentException">The supplied action key is blank.</exception>
    public static ToolPickerEvent CreateRequested(string? originToken, string actionKey) =>
        WithAction(ToolPickerEventKind.CreateRequested, originToken, actionKey);

    /// <summary>Creates the cancel request carrying only the opaque action key.</summary>
    /// <exception cref="ArgumentException">The supplied action key is blank.</exception>
    public static ToolPickerEvent CancelRequested(string? originToken, string actionKey) =>
        WithAction(ToolPickerEventKind.CancelRequested, originToken, actionKey);

    /// <summary>Creates the retry request carrying only the opaque action key.</summary>
    /// <exception cref="ArgumentException">The supplied action key is blank.</exception>
    public static ToolPickerEvent RetryRequested(string? originToken, string actionKey) =>
        WithAction(ToolPickerEventKind.RetryRequested, originToken, actionKey);

    /// <summary>Creates the return request, which carries no further payload.</summary>
    public static ToolPickerEvent ReturnRequested(string? originToken) =>
        new(ToolPickerEventKind.ReturnRequested, originToken, null, null, null);

    private static ToolPickerEvent WithAction(
        ToolPickerEventKind kind,
        string? originToken,
        string actionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionKey);

        return new(kind, originToken, null, null, actionKey);
    }
}
