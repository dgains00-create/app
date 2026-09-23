namespace DMO.Domain.Tools;

/// <summary>
/// One canonical Tool: the single Tool registry entry of the system.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §2.4 (domain types) and §5.2 (the six Beta minimum Tool-owned facts).
/// <para>
/// Identity tuple: (<see cref="Type"/>, <see cref="Reference"/>, <see cref="Lot"/>). A different
/// lot is a different Tool, so it is a different <c>tool_id</c>.
/// </para>
/// <para>
/// <see cref="Quantity"/> is <c>int?</c>: <c>null</c> means not established/not applicable and is
/// never rendered as <c>0</c>; <c>0</c> is a real value. <see cref="Processo"/> is Tool-owned and
/// nullable "where applicable to that Tool".
/// </para>
/// <para>
/// Deliberately absent: any version (nothing updates a Tool in P2-T04), technical condition,
/// operational note, utilisation, drawing/revision, change-request state, and every reverse-ID
/// array. No Tool update and no Tool delete path exists in this workstream.
/// </para>
/// </remarks>
public sealed record Tool(
    ToolId ToolId,
    ToolType Type,
    string Reference,
    string Lot,
    Processo? Processo,
    int? Quantity,
    IReadOnlyList<MachineCode> CompatibleMachines);
