using DMO.Application.Boquilhas;
using DMO.Domain.Boquilhas;

namespace DMO.Application.Repositories;

/// <summary>
/// The single Boquilhas repository contract (the OWNER CLARIFICATION register model, §34).
/// </summary>
/// <remarks>
/// <para>
/// Binding rules (same discipline as P2-T04 §12.2 / P2-T05 §20.2 / P2-T06 §8.2):
/// <c>CancellationToken</c> mandatory last; single reads return <c>Task&lt;T?&gt;</c>; lists return
/// <c>Task&lt;IReadOnlyList&lt;T&gt;&gt;</c>; every multi-row write opens its own transaction and is
/// all-or-nothing.
/// </para>
/// <para>
/// Constraint violations are mapped via <c>PostgresException.SqlState</c> + constraint name: 23514
/// → the same validator token, never a 500; 23505 on the register's <c>bq_id</c> unique key →
/// <c>Refused(RegisterExists)</c>; 23503 on the anchor/reference FKs → the typed anchor tokens.
/// <b>Superseded (Owner clarification):</b> the two ACTIVE partial unique indexes and their scoped
/// 23505 → <c>Refused(ActiveAggregateExists)</c> mapping are REMOVED — the
/// one-active-aggregate-per-anchor invariant no longer exists (no lifecycle state at all); the
/// repository owns no active-aggregate pre-check and no close/reopen/opening-facts members.
/// </para>
/// <para>
/// The register and its ledger are a historical fact set: appends carry no version guard (the
/// derived outstanding is a sum, races cannot corrupt it); edits use the per-movement
/// optimistic-concurrency token. The ONLY register-row update is the §34 association write
/// (<see cref="AssociatedAsync"/> — version-guarded; sets the REAL <c>bq_id</c>, clears the
/// provisional <c>tool_id</c> anchor, version + 1). The History/ficha traversal follows the
/// accepted read-only entity-set composition pattern over <c>bq_contexts</c>/<c>tools</c>/
/// <c>job_ons</c>.</para>
/// </remarks>
public interface IBoquilhasRepository
{
    /// <summary>The register with its movement ledger (physical receipt order), tracked reads.</summary>
    Task<BoquilhaRegister?> GetByIdAsync(Guid boquilhasId, CancellationToken cancellationToken);

    /// <summary>Register list: production context traversal + derived outstanding, one page.</summary>
    Task<IReadOnlyList<RegisterListItem>> ListAsync(
        BoquilhasListQuery query,
        CancellationToken cancellationToken);

    /// <summary>Backend-counted register rows for the SAME predicate + filters as
    /// <see cref="ListAsync"/> (the <c>Total</c> fact; additive read-only member, disclosed in the
    /// implementation response).</summary>
    Task<int> CountListAsync(BoquilhasListQuery query, CancellationToken cancellationToken);

    /// <summary>Local Histórico: movement-level rows with the register's production context,
    /// filters applied in SQL, one page (chronological movement history).</summary>
    Task<IReadOnlyList<HistoryMovementItem>> GetHistoryAsync(
        BoquilhasHistoryQuery query,
        CancellationToken cancellationToken);

    /// <summary>Backend-counted history rows for the SAME predicate + filters (the <c>Total</c>
    /// fact; additive read-only member, disclosed in the implementation response).</summary>
    Task<int> CountHistoryAsync(BoquilhasHistoryQuery query, CancellationToken cancellationToken);

    /// <summary>The per-movement edit/audit trail, <c>edited_at ASC</c>.</summary>
    Task<IReadOnlyList<MovementAuditEntry>> GetMovementAuditAsync(
        Guid movementId,
        CancellationToken cancellationToken);

    /// <summary>Create the register IDENTITY (one row, no quantity event), transactionally.
    /// EXACTLY one anchor: the REAL <c>bq_contexts</c> row XOR the provisional canonical BQ
    /// <c>tool_id</c> (§34.1).</summary>
    Task<BoquilhaRegister> CreatedAsync(BoqCreateUnit unit, CancellationToken cancellationToken);

    /// <summary>Append: ONE transaction — the single movement row (three closed types).</summary>
    Task<BoquilhaMovement> AppendMovementAsync(BoqAppendUnit unit, CancellationToken cancellationToken);

    /// <summary>Edit: ONE transaction — guarded UPDATE of the same row + the audit row.</summary>
    Task<BoqEditResult> EditMovementAsync(BoqEditUnit unit, CancellationToken cancellationToken);

    /// <summary>
    /// The §34 association write: ONE transaction — version-guarded UPDATE of the SAME register
    /// row (sets the REAL <c>bq_id</c>, clears the provisional <c>tool_id</c> anchor, version + 1).
    /// The candidate <c>bq_id</c> must resolve to a REAL <c>bq_contexts</c> row. A register that is
    /// already production-linked is refused (<c>AlreadyAssociated</c>); the <c>bq_id</c> unique key
    /// maps to <c>RegisterExists</c>; a version race maps to <see cref="ConcurrencyConflictException"/>.
    /// </summary>
    Task<BoquilhaRegister> AssociatedAsync(
        Guid boquilhasId,
        Guid bqId,
        int expectedVersion,
        CancellationToken cancellationToken);

    /// <summary>
    /// The §34.1 rule-2 candidate read (register side): ONE context-specific statement keyed by the
    /// register's own provisional <c>tool_id</c> — every REAL <c>bq_contexts</c> row whose
    /// <c>tool_id</c> equals it, with the REAL Job On production facts. No global scan, no
    /// load-then-filter.
    /// </summary>
    Task<IReadOnlyList<BqAssociationCandidate>> ListBqAssociationCandidatesAsync(
        Guid toolId,
        CancellationToken cancellationToken);

    /// <summary>
    /// The §34.1 rule-2 read (Job-On-incoming direction): the pending (pré-JobOn) registers whose
    /// provisional <c>tool_id</c> equals the supplied canonical UUID, with the canonical Tool's
    /// current reference/lot and the derived movement facts (light packet — history only when
    /// opened). No global scan.
    /// </summary>
    Task<IReadOnlyList<PendingRegisterCandidate>> ListPendingRegistersAsync(
        Guid toolId,
        CancellationToken cancellationToken);
}