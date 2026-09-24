using JobOnFindProductionsQuery = DMO.Application.JobOn.FindProductionsQuery;

namespace DMO.Application.Boquilhas;

/// <summary>
/// The single Boquilhas service contract (the OWNER CLARIFICATION register model, §34).
/// </summary>
/// <remarks>
/// The service composes:
/// <list type="bullet">
/// <item><c>IBoquilhasRepository</c> — all P2-T07 persistence (register identity + movement
/// history + the §34 association write and reads);</item>
/// <item><c>IJobOnService</c> — productions, ficha, <c>UpdateAsync</c> (BQ-slot Set) — the
/// <b>only</b> paths to Job On/<c>bq_contexts</c>;</item>
/// <item><c>IToolRepository</c> (read) — the canonical Tool existence/type of the pré-JobOn
/// anchor and the pending ficha facts;</item>
/// <item><c>IBoquilhasContextRead</c> (read) — the <c>bq_id → tool_id</c> chain of the
/// association;</item>
/// <item><c>IMachineRepairerAssignmentRepository</c> (read) + <c>IRepairerRepository</c> (read) —
/// the consumed repairer resolution;</item>
/// <item><c>ICurrentAccountContext</c> — every backend actor;</item>
/// <item>backend clock — every backend timestamp.</item>
/// </list>
/// There is NO lifecycle: no close/reopen/opening-facts member exists, and the register creation
/// manufactures no quantity event. The balance-relative rules are gone — the outstanding is
/// derived by replay and never validated ("movement facts remain the sole authority"). The §34
/// association members are the ONLY standalone-like surface: offered strictly while the register
/// is pending, proven by the SAME canonical <c>tool_id</c> UUID, and executed only after human
/// confirmation (mirroring the accepted Peso associate of P2-T05 §4.4).</remarks>
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

    /// <summary>
    /// Route 4 — create the register IDENTITY (no quantity): a REAL production BQ context XOR the
    /// transitional pré-JobOn canonical BQ <c>tool_id</c> (§34.1 rule 1). The carrier members are
    /// <c>bq_id</c> / <c>pending_tool_id</c>.
    /// </summary>
    Task<BoquilhasResult> CreateAsync(CreateBoquilhaRegisterCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Route 5 — append one movement (Saída / Entrada / Entrada sem reparação); equally valid on a
    /// pending (§34) register.
    /// </summary>
    Task<BoquilhasResult> AppendMovementAsync(AppendMovementCommand command, CancellationToken cancellationToken);

    /// <summary>Route 6 — edit the existing movement + audit (same row; no second quantity event).</summary>
    Task<BoquilhasResult> EditMovementAsync(EditMovementCommand command, CancellationToken cancellationToken);

    /// <summary>Route 10 — the six current machine → repairer assignments (consumed read).</summary>
    Task<BoquilhasResult> GetMachineAssignmentsAsync(CancellationToken cancellationToken);

    /// <summary>Route 11 — the repairer register (consumed read).</summary>
    Task<BoquilhasResult> GetRepairersAsync(CancellationToken cancellationToken);

    /// <summary>
    /// §34.1 rule 2 — the association candidates of a PENDING register: every REAL
    /// <c>bq_contexts</c> row whose <c>tool_id</c> equals the register's provisional anchor, with
    /// the REAL Job On production facts (register-side context; never auto-selected).
    /// </summary>
    Task<BoquilhasResult> GetAssociationCandidatesAsync(
        Guid boquilhasId,
        CancellationToken cancellationToken);

    /// <summary>
    /// §34.1 rule 2 — the pending registers of a production BQ context (Job-On-incoming direction,
    /// keyed by <c>bq_id → tool_id</c>; never a global scan): the pré-JobOn registers of the SAME
    /// canonical Tool, presented for a possible human-confirmed association.
    /// </summary>
    Task<BoquilhasResult> GetPendingRegistersAsync(Guid bqId, CancellationToken cancellationToken);

    /// <summary>
    /// §34.1 rules 2–3 — the human-confirmed association: the SAME <c>boquilhas_id</c> binds to the
    /// explicit candidate <c>bq_id</c> (whose <c>bq_contexts.tool_id</c> must equal the pending
    /// anchor — the same-UUID proof, no inference rule). Typed refusals: <c>already-associated</c>
    /// (only while pending), <c>association-mismatch</c> (different Tool), <c>stale-version</c>.
    /// On success the register becomes <c>bq_id → jobon_id</c>; history is untouched.
    /// </summary>
    Task<BoquilhasResult> AssociateAsync(
        AssociateBoquilhasCommand command,
        CancellationToken cancellationToken);

    /// <summary>Route 7 — reference → productions for the association flow (<c>IJobOnService</c>).</summary>
    Task<BoquilhasResult> FindProductionsAsync(
        JobOnFindProductionsQuery query,
        CancellationToken cancellationToken);

    /// <summary>Route 8 — Job On ficha read (<c>IJobOnService</c>).</summary>
    Task<BoquilhasResult> GetJobOnAsync(Guid jobOnId, CancellationToken cancellationToken);

    /// <summary>Route 9 — create the missing BQ context through <c>IJobOnService.UpdateAsync</c> (BQ-slot Set).</summary>
    Task<BoquilhasResult> AssociateBqAsync(AssociateBqCommand command, CancellationToken cancellationToken);
}