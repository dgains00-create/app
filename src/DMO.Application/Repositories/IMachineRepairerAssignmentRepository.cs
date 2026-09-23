using DMO.Domain.Controlo;

namespace DMO.Application.Repositories;

/// <summary>
/// The single machine-repairer-assignment repository contract (P2-T05 contract §20.2, exact).
/// </summary>
/// <remarks>
/// One row per machine; an absent row is the explicit "no repairer assigned" state. Every operation
/// touches exactly the one machine's row — never another machine's (MAC2–MAC4, AC-E2). The FK to
/// <c>repairers</c> is <c>RESTRICT</c>, and a machine outside the six settled codes is refused by
/// the CHECK.
/// </remarks>
public interface IMachineRepairerAssignmentRepository
{
    /// <summary>Reads the assignment of one machine, or <c>null</c> (none assigned).</summary>
    Task<MachineRepairerAssignment?> GetByMachineAsync(
        string machine,
        CancellationToken cancellationToken);

    /// <summary>Lists every current assignment.</summary>
    Task<IReadOnlyList<MachineRepairerAssignment>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Upserts the assignment of ONE machine (version = 1 on first set, else version += 1).</summary>
    Task<MachineRepairerAssignment> SetAsync(
        MachineRepairerAssignment assignment,
        CancellationToken cancellationToken);

    /// <summary>Removes the assignment of ONE machine (version-guarded; other machines untouched).</summary>
    Task ClearedAsync(string machine, int expectedVersion, CancellationToken cancellationToken);
}