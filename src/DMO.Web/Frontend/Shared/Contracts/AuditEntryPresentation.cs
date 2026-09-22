namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// One supplied historical/audit event rendered by <c>AuditTrail</c>.
/// </summary>
/// <remarks>
/// Authority: P2-T02 contract §6.2 and A1 freeze §12.
/// <para>
/// Only authority-backed supplied facts are carried: an opaque entry key, the supplied action
/// text, an optional supplied actor display fact, an optional explicit "attribution unavailable"
/// text, an optional human-readable timestamp display value, an optional semantic timestamp, and
/// optional detail/before/after facts, plus at most one optional generic detail action.
/// </para>
/// <para>
/// <b>There is no entry-level status.</b> Contract question Q2 is resolved <b>ABSENT</b> by the
/// Architect: A1 freeze §12 does not authorize an entry status slot, so no <c>Status</c> member
/// and no equivalent renamed field exists, and <c>AuditTrail</c> renders no entry-level
/// <see cref="RecordStatusPresentation"/>. Change meaning stays visible through the
/// authority-backed supplied action and detail facts.
/// </para>
/// <para>
/// No field is a canonical identity, and the type has <b>no</b> access to any session,
/// current-account, clock, time provider or service: a missing actor/timestamp is never filled
/// with the current user or the current time, and there is no formatting or parsing member for
/// timestamps (the supplied display text is rendered verbatim).
/// </para>
/// </remarks>
public sealed record AuditEntryPresentation
{
    private AuditEntryPresentation(
        string key,
        string actionText,
        string? actor,
        string? actorUnavailableText,
        string? timestampText,
        DateTimeOffset? timestampValue,
        string? detail,
        string? beforeDetail,
        string? afterDetail,
        SharedActionPresentation? detailAction)
    {
        Key = key;
        ActionText = actionText;
        Actor = actor;
        ActorUnavailableText = actorUnavailableText;
        TimestampText = timestampText;
        TimestampValue = timestampValue;
        Detail = detail;
        BeforeDetail = beforeDetail;
        AfterDetail = afterDetail;
        DetailAction = detailAction;
    }

    /// <summary>Opaque, stable event identity. Never a canonical identity, endpoint or URL.</summary>
    public string Key { get; }

    /// <summary>Mandatory supplied action/event text; the core supplied display text of the entry.</summary>
    public string ActionText { get; }

    /// <summary>Optional supplied actor display fact, rendered verbatim. Absent means never substituted.</summary>
    public string? Actor { get; }

    /// <summary>
    /// Optional supplied explicit attribution-unavailable text, rendered verbatim <b>only</b>
    /// when <see cref="Actor"/> is absent (A1 freeze §12).
    /// </summary>
    public string? ActorUnavailableText { get; }

    /// <summary>
    /// Optional supplied human-readable timestamp display value, rendered verbatim. The
    /// component performs no formatting, parsing or culture derivation.
    /// </summary>
    public string? TimestampText { get; }

    /// <summary>
    /// Optional supplied semantic timestamp, rendered only as the machine value alongside the
    /// supplied display text. Never used to order and never used to fill
    /// <see cref="TimestampText"/>.
    /// </summary>
    public DateTimeOffset? TimestampValue { get; }

    /// <summary>Optional supplied concise detail/description.</summary>
    public string? Detail { get; }

    /// <summary>Optional supplied before-state display detail. No diff is computed.</summary>
    public string? BeforeDetail { get; }

    /// <summary>Optional supplied after-state display detail. No diff is computed.</summary>
    public string? AfterDetail { get; }

    /// <summary>Optional single generic consumer detail action. The entry's only interactive element.</summary>
    public SharedActionPresentation? DetailAction { get; }

    /// <summary>Whether a supplied actor display fact accompanies the entry.</summary>
    public bool HasActor => !string.IsNullOrWhiteSpace(Actor);

    /// <summary>Whether a supplied attribution-unavailable text accompanies the entry.</summary>
    public bool HasActorUnavailableText => !string.IsNullOrWhiteSpace(ActorUnavailableText);

    /// <summary>
    /// The attribution text to render: the supplied actor, else the supplied explicit
    /// unavailable text, else nothing. Never a synthesized value.
    /// </summary>
    public string? AttributionText =>
        HasActor ? Actor : HasActorUnavailableText ? ActorUnavailableText : null;

    /// <summary>Whether any attribution fact is supplied for this entry.</summary>
    public bool HasAttribution => AttributionText is not null;

    /// <summary>Whether the supplied human-readable timestamp display text is present.</summary>
    public bool HasTimestampText => !string.IsNullOrWhiteSpace(TimestampText);

    /// <summary>Whether a supplied semantic timestamp is present (machine value only).</summary>
    public bool HasTimestampValue => TimestampValue.HasValue;

    /// <summary>Whether a supplied detail accompanies the entry.</summary>
    public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);

    /// <summary>Whether a supplied before-state detail accompanies the entry.</summary>
    public bool HasBeforeDetail => !string.IsNullOrWhiteSpace(BeforeDetail);

    /// <summary>Whether a supplied after-state detail accompanies the entry.</summary>
    public bool HasAfterDetail => !string.IsNullOrWhiteSpace(AfterDetail);

    /// <summary>Whether the optional supplied detail action is presented.</summary>
    public bool HasDetailAction => DetailAction is { Visible: true };

    /// <summary>
    /// Whether the entry contains a supplied action and is therefore focusable (A1 freeze §12:
    /// entries focus only when they contain a supplied action).
    /// </summary>
    public bool IsInteractive => HasDetailAction;

    /// <summary>Creates an audit entry from supplied facts.</summary>
    /// <exception cref="ArgumentException">The supplied key or action text is blank.</exception>
    public static AuditEntryPresentation Create(
        string key,
        string actionText,
        string? actor = null,
        string? actorUnavailableText = null,
        string? timestampText = null,
        DateTimeOffset? timestampValue = null,
        string? detail = null,
        string? beforeDetail = null,
        string? afterDetail = null,
        SharedActionPresentation? detailAction = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionText);

        return new AuditEntryPresentation(
            key,
            actionText,
            actor,
            actorUnavailableText,
            timestampText,
            timestampValue,
            detail,
            beforeDetail,
            afterDetail,
            detailAction);
    }
}
