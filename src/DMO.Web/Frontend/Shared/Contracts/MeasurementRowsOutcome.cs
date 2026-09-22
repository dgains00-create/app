namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The result of one repeated-row mechanics transition.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §4.2.
/// <para>
/// A refused transition raises <b>no</b> event and exposes the supplied reason it was refused for —
/// this is how the frozen minimum-row rule keeps removal disabled with a visible associated reason.
/// </para>
/// </remarks>
public sealed record MeasurementRowsOutcome
{
    private MeasurementRowsOutcome(
        MeasurementRowsEvent? raisedEvent,
        MeasurementRowsFocusTarget focusTarget,
        bool refused,
        string? refusalReason,
        string? allocatedRowKey,
        int? allocatedIndex)
    {
        Event = raisedEvent;
        FocusTarget = focusTarget;
        Refused = refused;
        RefusalReason = refusalReason;
        AllocatedRowKey = allocatedRowKey;
        AllocatedIndex = allocatedIndex;
    }

    /// <summary>The raised frozen event, or <c>null</c> when the transition raises none.</summary>
    public MeasurementRowsEvent? Event { get; }

    /// <summary>Where focus belongs after this transition.</summary>
    public MeasurementRowsFocusTarget FocusTarget { get; }

    /// <summary>Whether the transition was refused.</summary>
    public bool Refused { get; }

    /// <summary>The supplied reason the transition was refused for, when refused.</summary>
    public string? RefusalReason { get; }

    /// <summary>The newly allocated opaque frontend row key, for an accepted add.</summary>
    public string? AllocatedRowKey { get; }

    /// <summary>The append index of the newly allocated row, for an accepted add.</summary>
    public int? AllocatedIndex { get; }

    /// <summary>Whether an event was raised.</summary>
    public bool HasEvent => Event is not null;

    /// <summary>Creates an accepted structural or value outcome.</summary>
    public static MeasurementRowsOutcome Accepted(
        MeasurementRowsEvent raisedEvent,
        MeasurementRowsFocusTarget focusTarget,
        string? allocatedRowKey = null,
        int? allocatedIndex = null) =>
        new(raisedEvent, focusTarget, false, null, allocatedRowKey, allocatedIndex);

    /// <summary>Creates a refused outcome carrying the supplied reason.</summary>
    /// <exception cref="ArgumentException">The supplied refusal reason is blank.</exception>
    public static MeasurementRowsOutcome RefusedWith(string? refusalReason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refusalReason);

        return new(null, MeasurementRowsFocusTarget.None, true, refusalReason, null, null);
    }
}
