using DMO.Domain.Controlo;

namespace DMO.Application.Repositories;

/// <summary>
/// The single repairer-register repository contract (P2-T05 contract §20.2, exact).
/// </summary>
/// <remarks>
/// There is no delete path in P2-T05 (REP3/AC-D3): name-only register entries that are no longer
/// used simply stay in the register; a repairer referenced by an assignment is DB-protected by the
/// <c>RESTRICT</c> foreign key (REP5).
/// </remarks>
public interface IRepairerRepository
{
    /// <summary>Reads one repairer, or <c>null</c>.</summary>
    Task<Repairer?> GetByIdAsync(Guid repairerId, CancellationToken cancellationToken);

    /// <summary>Lists the register in deterministic name order.</summary>
    Task<IReadOnlyList<Repairer>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Inserts the repairer (version = 1).</summary>
    Task<Repairer> CreatedAsync(Repairer repairer, CancellationToken cancellationToken);

    /// <summary>Renames the repairer on the same row (version += 1, version-guarded).</summary>
    Task<Repairer> RenamedAsync(Repairer repairer, CancellationToken cancellationToken);
}