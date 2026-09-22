namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// A frozen generic presentation event raised by the compact summary row.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.2.4.
/// <para>
/// The event carries only opaque keys supplied by the consumer: the item key and the action key. It
/// never carries a canonical identity, an address, a route or a resolved target, and the primitive
/// never acts on it.
/// </para>
/// </remarks>
public sealed record ToolSummaryRowEvent
{
    private ToolSummaryRowEvent(ToolSummaryRowEventKind kind, string itemKey, string actionKey)
    {
        Kind = kind;
        ItemKey = itemKey;
        ActionKey = actionKey;
    }

    /// <summary>The frozen event kind.</summary>
    public ToolSummaryRowEventKind Kind { get; }

    /// <summary>The opaque item key of the row whose action was invoked.</summary>
    public string ItemKey { get; }

    /// <summary>The opaque action key of the invoked action.</summary>
    public string ActionKey { get; }

    /// <summary>Creates the generic action-invocation event.</summary>
    /// <exception cref="ArgumentException">The item key or action key is blank.</exception>
    public static ToolSummaryRowEvent ActionInvoked(string itemKey, string actionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionKey);

        return new ToolSummaryRowEvent(ToolSummaryRowEventKind.ActionInvoked, itemKey, actionKey);
    }
}
