using JobOnFindProductionsQuery = DMO.Application.JobOn.FindProductionsQuery;

namespace DMO.Application.Boquilhas;

/// <summary>
/// The single Boquilhas service contract (the OWNER CLARIFICATION register model).
/// </summary>
/// <remarks>
/// The service composes:
/// <list type="bullet">
/// <item><c>IBoquilhasRepository</c> — all P2-T07 persistence (register identity + movement
/// history);</item>
/// <item><c>IJobOnService</c> — productions, ficha, <c>UpdateAsync</c> (BQ-slot Set) — the
/// <b>only</b> paths to Job On/<c>bq_contexts</c>;</item>
/// <item><c>IToolService</c>/<c>IBoquilhasContextRead</c> (read) — the production association
/// facts;</item>
/// <item><c>IMachineRepairerAssignmentRepository</c> (read) + <c>IRepairerRepository</c> (read) —
/// the consumed repairer resolution;</item>
/// <item><c>ICurrentAccountContext</c> — every backend actor;</item>
/// <item>backend clock — every backend timestamp.</item>
/// </list>
/// There is NO lifecycle: no close/reopen/opening-facts member exists, and the register creation
/// manufactures no quantity event. The balance-relative rules are gone — the outstanding is
/// derived by replay and never validated ("movement facts remain the sole authority").</remarks>
public interface IBoquilhasService
{
    /// <summary>Route 1 — the register list (production context + derived outstanding).</summary>
    Task<BoquilhasResult> GetListAsync(BoquilhasListQuery query, CancellationToken cancellationToken);

    /// <summary>Route 12 — the local Histórico (movement-level chronological history).</summary>
    Task<BoquilhasResult> GetHistoryAsync(BoquilhasHistoryQuery query, CancellationToken cancellationToken);

    /// <summary>Route 2 — the register ficha (production context, ledger, outstanding).</summary>
    Task<BoquilhasResult> GetAsync(Guid boquilhasId, CancellationToken cancellationToken);

    /// <summary>Route 3 — the per-movement edit/audit trail of one movement.</summary>
    Task<BoquilhasResult> GetMovementAuditAsync(
        Guid boquilhasId,
        Guid movementId,
        CancellationToken cancellationToken);

    /// <summary>Route 4 — create the register IDENTITY of a REAL production BQ context (no quantity).</summary>
    Task<BoquilhasResult> CreateAsync(CreateBoquilhaRegisterCommand command, CancellationToken cancellationToken);

    /// <summary>Route 5 — append one movement (Saída / Entrada / Entrada sem reparação).</summary>
    Task<BoquilhasResult> AppendMovementAsync(AppendMovementCommand command, CancellationToken cancellationToken);

    /// <summary>Route 6 — edit the existing movement + audit (same row; no second quantity event).</summary>
    Task<BoquilhasResult> EditMovementAsync(EditMovementCommand command, CancellationToken cancellationToken);

    /// <summary>Route 10 — the six current machine → repairer assignments (consumed read).</summary>
    Task<BoquilhasResult> GetMachineAssignmentsAsync(CancellationToken cancellationToken);

    /// <summary>Route 11 — the repairer register (consumed read).</summary>
    Task<BoquilhasResult> GetRepairersAsync(CancellationToken cancellationToken);

    /// <summary>Route 7 — reference → productions for the association flow (<c>IJobOnService</c>).</summary>
    Task<BoquilhasResult> FindProductionsAsync(
        JobOnFindProductionsQuery query,
        CancellationToken cancellationToken);

    /// <summary>Route 8 — Job On ficha read (<c>IJobOnService</c>).</summary>
    Task<BoquilhasResult> GetJobOnAsync(Guid jobOnId, CancellationToken cancellationToken);

    /// <summary>Route 9 — create the missing BQ context through <c>IJobOnService.UpdateAsync</c> (BQ-slot Set).</summary>
    Task<BoquilhasResult> AssociateBqAsync(AssociateBqCommand command, CancellationToken cancellationToken);
}