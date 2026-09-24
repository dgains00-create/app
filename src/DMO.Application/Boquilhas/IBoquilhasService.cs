using JobOnFindProductionsQuery = DMO.Application.JobOn.FindProductionsQuery;

namespace DMO.Application.Boquilhas;

/// <summary>
/// The single Boquilhas service contract (P2-T07 contract §9.1, exact).
/// </summary>
/// <remarks>
/// The service composes:
/// <list type="bullet">
/// <item><c>IBoquilhasRepository</c> — all P2-T07 persistence;</item>
/// <item><c>IJobOnService</c> — productions, ficha, <c>UpdateAsync</c> (BQ-slot Set) — the <b>only</b>
/// paths to Job On/<c>bq_contexts</c> (AC-I3/I5);</item>
/// <item><c>IToolService</c> (read) — Tool existence/type for the standalone anchor (the shared
/// orchestration backend validation); Tool search/create stays on the <c>ferramentas</c>-gated
/// P2-T04 routes (§16, AC-T4);</item>
/// <item><c>IMachineRepairerAssignmentRepository</c> (read) + <c>IRepairerRepository</c> (read) —
/// resolution + register consumption (§21);</item>
/// <item><c>ICurrentAccountContext</c> — every backend actor
/// (created/recorded/edited/closed/reopened);</item>
/// <item><c>IClock</c>/<c>DateTimeOffset.UtcNow</c> — every backend timestamp.</item>
/// </list>
/// The Web layer never queries the database directly; the service never calls a P2-T05/P2-T04/
/// P2-T06-gated HTTP route (no cross-module HTTP calls — P2-T06 §14 discipline).</remarks>
public interface IBoquilhasService
{
    /// <summary>Route 4 — the Registo lot grid (active default; backend filters; paging).</summary>
    Task<BoquilhasResult> GetListAsync(BoquilhasListQuery query, CancellationToken cancellationToken);

    /// <summary>Route 18 — the local Histórico (full backend filter set; paging).</summary>
    Task<BoquilhasResult> GetHistoryAsync(BoquilhasHistoryQuery query, CancellationToken cancellationToken);

    /// <summary>Route 5 — the aggregate ficha (anchor projection, machines, opening facts, balance, ledger, close/reopen).</summary>
    Task<BoquilhasResult> GetAsync(Guid boquilhasId, CancellationToken cancellationToken);

    /// <summary>Route 6 — the per-movement edit/audit trail of one movement.</summary>
    Task<BoquilhasResult> GetMovementAuditAsync(
        Guid boquilhasId,
        Guid movementId,
        CancellationToken cancellationToken);

    /// <summary>Route 7 — create the aggregate (+ machine set + Início) transactionally.</summary>
    Task<BoquilhasResult> CreateAsync(CreateBoquilhasCommand command, CancellationToken cancellationToken);

    /// <summary>Route 8 — append one movement (four types; balance-relative validation).</summary>
    Task<BoquilhasResult> AppendMovementAsync(AppendMovementCommand command, CancellationToken cancellationToken);

    /// <summary>Route 9 — edit the existing movement + audit (same row; no second quantity event).</summary>
    Task<BoquilhasResult> EditMovementAsync(EditMovementCommand command, CancellationToken cancellationToken);

    /// <summary>Route 10 — close (status + immutable snapshot), same <c>boquilhas_id</c>.</summary>
    Task<BoquilhasResult> CloseAsync(CloseBoquilhasCommand command, CancellationToken cancellationToken);

    /// <summary>Route 11 — reopen (reason required; eligibility), same <c>boquilhas_id</c>.</summary>
    Task<BoquilhasResult> ReopenAsync(ReopenBoquilhasCommand command, CancellationToken cancellationToken);

    /// <summary>Route 12 — update opening facts (opening date / utilisation / observations / machine set).</summary>
    Task<BoquilhasResult> UpdateOpeningFactsAsync(
        UpdateOpeningFactsCommand command,
        CancellationToken cancellationToken);

    /// <summary>Route 16 — the six current machine → repairer assignments (consumed read).</summary>
    Task<BoquilhasResult> GetMachineAssignmentsAsync(CancellationToken cancellationToken);

    /// <summary>Route 17 — the repairer register (consumed read).</summary>
    Task<BoquilhasResult> GetRepairersAsync(CancellationToken cancellationToken);

    /// <summary>Route 13 — reference → productions for the opening flow (<c>IJobOnService</c>).</summary>
    Task<BoquilhasResult> FindProductionsAsync(
        JobOnFindProductionsQuery query,
        CancellationToken cancellationToken);

    /// <summary>Route 14 — Job On ficha read (<c>IJobOnService</c>).</summary>
    Task<BoquilhasResult> GetJobOnAsync(Guid jobOnId, CancellationToken cancellationToken);

    /// <summary>Route 15 — create the missing BQ context through <c>IJobOnService.UpdateAsync</c> (BQ-slot Set).</summary>
    Task<BoquilhasResult> AssociateBqAsync(AssociateBqCommand command, CancellationToken cancellationToken);
}