namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// A frozen generic presentation event raised by the decision bar.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.4.6.
/// <para>
/// The event carries only the opaque supplied action key. It never carries a transition result, a
/// canonical identity, an address, a route or a resolved target, and the bar never acts on it.
/// </para>
/// </remarks>
public sealed record DecisionBarEvent
{
    private DecisionBarEvent(DecisionBarEventKind kind, string actionKey)
    {
        Kind = kind;
        ActionKey = actionKey;
    }

    /// <summary>The frozen event kind.</summary>
    public DecisionBarEventKind Kind { get; }

    /// <summary>The opaque supplied action key.</summary>
    public string ActionKey { get; }

    /// <summary>Creates the generic action-invocation event.</summary>
    /// <exception cref="ArgumentException">The supplied action key is blank.</exception>
    public static DecisionBarEvent ActionInvoked(string actionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionKey);

        return new DecisionBarEvent(DecisionBarEventKind.ActionInvoked, actionKey);
    }
}
