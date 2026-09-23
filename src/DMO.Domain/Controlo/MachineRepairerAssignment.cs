using DMO.Domain.Tools;

namespace DMO.Domain.Controlo;

/// <summary>
/// One independent current machine → repairer assignment.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §11 and §16.4. Each of the six machines
/// <c>B1 B2 B3 C1 C2 C3</c> holds its <b>own independent</b> assignment: there is no grouping rule,
/// no shared "Linha B"/"Linha C" repairer, no inheritance and no cascade — changing one machine
/// affects only that machine (MAC2–MAC4, AC-E2). An absent row is the explicit "no repairer
/// assigned" state, never a default and never an error. The table holds current assignment only; no
/// assignment-history table exists (Q-HIST, §11.4).
/// </remarks>
public sealed record MachineRepairerAssignment(
    Guid MachineRepairerAssignmentId,
    MachineCode Machine,
    RepairerId RepairerId,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);