namespace DMO.Web.Frontend.Shared.Contracts;

/// <summary>
/// The frozen generic presentation-event vocabulary raised by the compact summary row.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §3.2.4. The row raises only the generic action invocation; it owns no
/// selection arbitration, so it raises no selection event.
/// </remarks>
public enum ToolSummaryRowEventKind
{
    /// <summary>A supplied, visible, enabled row action was invoked.</summary>
    ActionInvoked,
}
