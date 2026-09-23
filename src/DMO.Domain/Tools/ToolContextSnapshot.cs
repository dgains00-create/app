namespace DMO.Domain.Tools;

/// <summary>
/// The frozen Tool values of a production context, exactly as used at that occurrence.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §2.4, §3.5 and §7.2 — the frozen set is exactly
/// <c>tool_type</c> / <c>tool_reference</c> / <c>tool_lot</c> and <b>nothing else</b>.
/// <para>
/// The snapshot is captured at the moment of the explicit human Tool selection for that context and
/// is never rewritten by a later change to the canonical Tool. It is not a second Tool authority: it
/// is never queried to answer "what is this Tool now", never used as a join key and never rendered
/// as if it were current Tool state.
/// </para>
/// </remarks>
public sealed record ToolContextSnapshot(
    ToolType Type,
    string Reference,
    string Lot);
