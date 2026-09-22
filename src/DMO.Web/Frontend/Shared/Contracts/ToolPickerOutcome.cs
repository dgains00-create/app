namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The result of one picker transition: the frozen event (if any), the focus target and whether the
/// transition was blocked.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §4.1.
/// <para>
/// A blocked transition raises <b>no</b> event — this is how the picker refuses to name an
/// unsupplied candidate and how it honours a consumer-disabled cancel affordance. The outcome never
/// carries a domain result, a resolved target or a navigation instruction.
/// </para>
/// </remarks>
public sealed record ToolPickerOutcome
{
    private ToolPickerOutcome(ToolPickerEvent? raisedEvent, ToolPickerFocusTarget focusTarget, bool blocked)
    {
        Event = raisedEvent;
        FocusTarget = focusTarget;
        Blocked = blocked;
    }

    /// <summary>The raised frozen event, or <c>null</c> when the transition raises none.</summary>
    public ToolPickerEvent? Event { get; }

    /// <summary>Where focus belongs after this transition.</summary>
    public ToolPickerFocusTarget FocusTarget { get; }

    /// <summary>Whether the transition was blocked and therefore raised no event.</summary>
    public bool Blocked { get; }

    /// <summary>Whether an event was raised.</summary>
    public bool HasEvent => Event is not null;

    /// <summary>Creates a transition outcome.</summary>
    public static ToolPickerOutcome Create(
        ToolPickerEvent? raisedEvent,
        ToolPickerFocusTarget focusTarget = ToolPickerFocusTarget.Unchanged,
        bool blocked = false) =>
        new(raisedEvent, focusTarget, blocked);

    /// <summary>Creates the outcome of a transition that raises no event and moves no focus.</summary>
    public static ToolPickerOutcome Silent() => new(null, ToolPickerFocusTarget.Unchanged, false);

    /// <summary>Creates the outcome of a refused transition: no event, no focus move.</summary>
    public static ToolPickerOutcome Refused() => new(null, ToolPickerFocusTarget.Unchanged, true);
}
