using DMO.Application.Boquilhas;
using DMO.Domain.Boquilhas;

namespace DMO.Application.Repositories;

/// <summary>
/// The single Boquilhas repository contract (P2-T07 contract §8.2, exact).
/// </summary>
/// <remarks>
/// <para>
/// Binding rules (same as P2-T04 §12.2 / P2-T05 §20.2 / P2-T06 §8.2): <c>CancellationToken</c>
/// mandatory last; single reads return <c>Task&lt;T?&gt;</c>; lists return
/// <c>Task&lt;IReadOnlyList&lt;T&gt;&gt;</c>; every multi-row write opens its own transaction and is
/// all-or-nothing (a forced mid-transaction failure leaves zero rows of the operation).
/// </para>
/// <para>
/// Constraint violations are mapped via <c>PostgresException.SqlState</c> + constraint name: 23514
/// → the same validator token, never a 500; <b>23505 on <c>IX_boquilhas_active_bq_id</c> or
/// <c>IX_boquilhas_active_tool_id</c> → <c>Refused(ActiveAggregateExists)</c></b> — the exact scoped
/// mapping of §7.2, with <b>no other</b> 23505 source mapped to this domain result.
/// </para>
/// <para>
/// The repositories own no domain rule beyond the accepted unit shapes; the balance-relative
/// validation is computed by replay over the ledger loaded <b>inside</b> each write transaction (the
/// single pure <see cref="BalanceProjection.Replay"/> helper, §18) and the refusals are surfaced as
/// <see cref="BoquilhasPersistenceException"/> (mapped by the service to the same typed results as
/// its own pre-checks). No other module's table is ever touched: the consumed P2-T05/P2-T04 reads go
/// through the closed application repositories; the History/ficha traversal follows the accepted
/// read-only entity-set composition pattern (P2-T06 §2/.3, review ACCEPT 4d88dbe) over
/// <c>bq_contexts</c>/<c>tools</c>/<c>job_ons</c>.
/// </para>
/// </remarks>
public interface IBoquilhasRepository
{
    /// <summary>
    /// Aggregate + machines + movements (ledger order) + close snapshots + reopenings, tracked.
    /// </summary>
    Task<BoquilhaAggregate?> GetByIdAsync(Guid boquilhasId, CancellationToken cancellationToken);

    /// <summary>Registo lot grid: predicate + filters applied in SQL, deterministic ordering, one page (§23/§24).</summary>
    Task<IReadOnlyList<BoquilhaListItem>> ListAsync(
        BoquilhasListQuery query,
        CancellationToken cancellationToken);

    /// <summary>Local Histórico: predicates + filters applied in SQL, one page (§24).</summary>
    Task<IReadOnlyList<HistoryItem>> GetHistoryAsync(
        BoquilhasHistoryQuery query,
        CancellationToken cancellationToken);

    /// <summary>
    /// Backend-counted Registo rows for the SAME predicate + filters as <see cref="ListAsync"/>
    /// (the §24.2/AC-H5 <c>Total</c> fact; additive read-only member, disclosed in the
    /// implementation response — the printed §8.2 carrier carries no cross-page total).
    /// </summary>
    Task<int> CountListAsync(BoquilhasListQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Backend-counted Histórico rows for the SAME predicate + filters as
    /// <see cref="GetHistoryAsync"/> (the §24.2/AC-H5 <c>Total</c> fact; additive read-only member,
    /// disclosed in the implementation response).
    /// </summary>
    Task<int> CountHistoryAsync(BoquilhasHistoryQuery query, CancellationToken cancellationToken);

    /// <summary>Reopen eligibility anchor scan: does another ACTIVE aggregate share this anchor?</summary>
    Task<bool> HasActiveAggregateForAnchorAsync(
        Guid? bqId,
        Guid? toolId,
        Guid excludeBoquilhasId,
        CancellationToken cancellationToken);

    /// <summary>The most recent close snapshot across all aggregates sharing this anchor (reopen eligibility).</summary>
    Task<Guid?> GetLastCloseSnapshotIdForAnchorAsync(
        Guid? bqId,
        Guid? toolId,
        CancellationToken cancellationToken);

    /// <summary>The per-movement edit/audit trail, <c>edited_at ASC</c>.</summary>
    Task<IReadOnlyList<MovementAuditEntry>> GetMovementAuditAsync(
        Guid movementId,
        CancellationToken cancellationToken);

    /// <summary>Create: ONE transaction — aggregate + machine rows + the Início movement (§10).</summary>
    Task<BoquilhaAggregate> CreatedAsync(BoqCreateUnit unit, CancellationToken cancellationToken);

    /// <summary>Append: ONE transaction — replay validation + the single movement row (§10/§17).</summary>
    Task<BoquilhaMovement> AppendMovementAsync(BoqAppendUnit unit, CancellationToken cancellationToken);

    /// <summary>Edit: ONE transaction — guarded UPDATE of the same row + the audit row (§10/§19).</summary>
    Task<BoqEditResult> EditMovementAsync(BoqEditUnit unit, CancellationToken cancellationToken);

    /// <summary>Close: ONE transaction — status + version + immutable snapshot row (§10/§23).</summary>
    Task<BoqCloseResult> CloseAsync(BoqCloseUnit unit, CancellationToken cancellationToken);

    /// <summary>Reopen: ONE transaction — status + version + reopen row (§10/§23).</summary>
    Task<BoqReopenResult> ReopenAsync(BoqReopenUnit unit, CancellationToken cancellationToken);

    /// <summary>Opening-facts: ONE transaction — aggregate row + machine-set replace (§10/§23).</summary>
    Task<BoquilhaAggregate> UpdateOpeningFactsAsync(BoqOpeningFactsUnit unit, CancellationToken cancellationToken);
}