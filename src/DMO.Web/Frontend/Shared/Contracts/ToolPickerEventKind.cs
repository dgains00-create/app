namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The frozen generic presentation-event vocabulary raised by the shared picker.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.1.10 (the complete output vocabulary of the frozen picker
/// contract). The primitive <b>raises</b> these events and never acts on them: no event creates,
/// associates or persists anything, opens a lookup, decides access or navigates, and no event
/// carries an address, endpoint, canonical identity or resolved target.
/// </remarks>
public enum ToolPickerEventKind
{
    /// <summary>The consumer is asked to run its own search with the current controlled query.</summary>
    SearchRequested,

    /// <summary>An explicit candidate activation selected exactly one supplied candidate.</summary>
    CandidateSelected,

    /// <summary>The consumer is asked to start its own create subflow.</summary>
    CreateRequested,

    /// <summary>The user cancelled: the consumer returns to the origin without a selection.</summary>
    CancelRequested,

    /// <summary>The consumer is asked to retry its own failing lookup.</summary>
    RetryRequested,

    /// <summary>The consumer's own subflow completed and the picker should return.</summary>
    ReturnRequested,
}
