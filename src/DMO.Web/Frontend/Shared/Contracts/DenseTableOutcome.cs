namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The result of one <c>DenseDataTable</c> interaction: what changed and which generic
/// presentation event was raised.
/// </summary>
/// <remarks>
/// Authority: P2-T02 contract §5.7. Presentation only: it carries no URL, no route, no target,
/// no service and no domain vocabulary, and the interaction that produced it never mutates
/// domain state.
/// </remarks>
/// <param name="Selected">Whether the interacted row is the selected row after the interaction.</param>
/// <param name="OpenRequested">Whether this interaction requested an open.</param>
/// <param name="Event">The single generic event raised by this interaction, or <c>null</c>.</param>
public sealed record DenseTableOutcome(bool Selected, bool OpenRequested, DenseTableEvent? Event)
{
    /// <summary>An interaction that changed nothing and raised nothing.</summary>
    public static DenseTableOutcome None { get; } = new(false, false, null);

    /// <summary>Whether the interaction raised a generic presentation event.</summary>
    public bool HasEvent => Event is not null;

    /// <summary>The stable token of the raised event kind, or <c>null</c> when none was raised.</summary>
    public string? EventKindToken => Event?.KindToken;
}
