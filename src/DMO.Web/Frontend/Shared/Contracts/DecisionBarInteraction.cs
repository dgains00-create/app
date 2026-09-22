namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The deterministic, presentation-only invocation model of the decision bar.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §4.3 (normative) with §3.4.4.
/// <para>
/// Frozen semantics:
/// </para>
/// <list type="bullet">
/// <item>an invocation of the action named by the controlled pending key is blocked and raises no
/// event — the frozen duplicate-invocation rule;</item>
/// <item>nothing else suppresses an invocation: an enabled, non-pending action invoked twice raises
/// two events, because no timing, locking or collapse window is invented;</item>
/// <item>completion/reset is the consumer's act: it adopts the new controlled pending value and
/// re-enables the previously pending action;</item>
/// <item>the model encodes no transition, transition legality, confirmation or retry policy, and
/// holds no service, clock, session, persistence or domain vocabulary.</item>
/// </list>
/// </remarks>
public sealed class DecisionBarInteraction
{
    /// <summary>Creates the invocation model for the supplied controlled pending value.</summary>
    /// <exception cref="ArgumentException">The supplied pending key is blank.</exception>
    public DecisionBarInteraction(string? pendingActionKey = null)
    {
        Reset(pendingActionKey);
    }

    /// <summary>The controlled pending action key, or <c>null</c> when nothing is pending.</summary>
    public string? PendingActionKey { get; private set; }

    /// <summary>Whether a supplied action is currently pending.</summary>
    public bool HasPendingAction => PendingActionKey is not null;

    /// <summary>
    /// Invokes one supplied action: it raises exactly one generic action event, unless the action is
    /// the pending one, in which case it is blocked and raises nothing.
    /// </summary>
    /// <exception cref="ArgumentException">The supplied action key is blank.</exception>
    public DecisionBarOutcome Invoke(string actionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionKey);

        if (PendingActionKey is not null &&
            string.Equals(PendingActionKey, actionKey, StringComparison.Ordinal))
        {
            return DecisionBarOutcome.BlockedAsDuplicate();
        }

        return DecisionBarOutcome.Invoked(DecisionBarEvent.ActionInvoked(actionKey));
    }

    /// <summary>
    /// Adopts the consumer's controlled pending value. It raises no event; completion or failure is
    /// the consumer's act and releases the previously pending action.
    /// </summary>
    /// <exception cref="ArgumentException">The supplied pending key is blank.</exception>
    public void Reset(string? pendingActionKey = null)
    {
        if (pendingActionKey is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pendingActionKey);
        }

        PendingActionKey = pendingActionKey;
    }

    /// <summary>Adopts a re-supplied controlled pending value without raising any event.</summary>
    /// <exception cref="ArgumentException">The supplied pending key is blank.</exception>
    public void Sync(string? pendingActionKey) => Reset(pendingActionKey);
}
