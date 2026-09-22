namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The result of one decision-bar invocation.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §4.3. While a supplied action is pending, its invocation is blocked
/// and raises no event; no other suppression exists.
/// </remarks>
public sealed record DecisionBarOutcome
{
    private DecisionBarOutcome(DecisionBarEvent? raisedEvent, bool duplicateBlocked)
    {
        Event = raisedEvent;
        DuplicateBlocked = duplicateBlocked;
    }

    /// <summary>The raised frozen event, or <c>null</c> when the invocation was blocked.</summary>
    public DecisionBarEvent? Event { get; }

    /// <summary>Whether the invocation was blocked because the action is pending.</summary>
    public bool DuplicateBlocked { get; }

    /// <summary>Whether an event was raised.</summary>
    public bool HasEvent => Event is not null;

    /// <summary>Creates an accepted invocation outcome.</summary>
    public static DecisionBarOutcome Invoked(DecisionBarEvent raisedEvent) =>
        new(raisedEvent, false);

    /// <summary>Creates the blocked outcome of a repeated invocation while the action is pending.</summary>
    public static DecisionBarOutcome BlockedAsDuplicate() => new(null, true);
}
