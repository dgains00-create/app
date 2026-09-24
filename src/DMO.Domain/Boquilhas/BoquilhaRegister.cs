namespace DMO.Domain.Boquilhas;

/// <summary>
/// One Boquilhas production movement register: the identity carrier of the movement history of one
/// REAL production/BQ context — or, transitionally, of one canonical BQ Tool while no Job On exists
/// yet (the pré-JobOn anchor of P2-T07 §34).
/// </summary>
/// <remarks>
/// Authority: P2-T07 OWNER CLARIFICATION, §34 (the pré-JobOn register + association at the matching
/// <c>bq_id</c>).
/// <para>
/// A Boquilhas register groups the movement history belonging to that production/BQ context
/// (<c>boquilhas_id → bq_id → jobon_id + tool_id</c>; <c>bq_id</c> is a REAL frozen
/// <c>bq_contexts</c> row — one register per BQ context, DB-enforced by the <c>bq_id</c> unique
/// key). While no Job On exists (<see cref="IsPending"/>), the register is anchored provisionally
/// on the canonical BQ <c>tool_id</c> (a real <c>tools</c> row — never a fake Job On, never a fake
/// <c>bq_id</c>): <c>boquilhas_id → tool_id</c>, with <c>bq_id</c> NULL. The association point is
/// the matching <c>bq_id</c> whose <c>bq_contexts.tool_id</c> equals the pending <c>tool_id</c>:
/// the operator confirms and the SAME <c>boquilhas_id</c> passes to <c>bq_id → jobon_id</c>, the
/// provisional anchor is cleared and the version bumps once. Movements recorded while pending keep
/// their facts after the association — nothing is migrated, rewritten or replayed (§34.1 rule 3).
/// </para>
/// <para>
/// <c>boquilhas_id</c> is the technical identity of the register ONLY — it does NOT imply
/// active/closed/reopened state: there is no lifecycle state machine at all. Creating the register
/// never manufactures a quantity movement (no Início); movements may be recorded while the
/// production runs AND after the production has ended — the movement's own
/// <c>business_date</c>/<c>recorded_at</c> record when the movement happened.
/// </para>
/// <para>
/// <b>Superseded (Owner clarification):</b> the open/closed aggregate workflow (status
/// active/closed, close/reopen actions, close snapshots, reopening history,
/// one-active-aggregate-per-anchor uniqueness and its race machinery) and the PERMANENT standalone
/// (<c>tool_id</c>) model are removed — the provisional <c>tool_id</c> anchor of §34 is the ONLY
/// standalone-like state and it is transitional by contract.</para>
/// </remarks>
public sealed record BoquilhaRegister(
    BoquilhasId BoquilhasId,
    Guid? BqId,
    Guid? ToolId,
    int Version,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<BoquilhaMovement> Movements)
{
    /// <summary>
    /// Whether the register is still in the transitional pré-JobOn state (no production/BQ
    /// association yet; the provisional <c>tool_id</c> anchor is the active relation).
    /// </summary>
    public bool IsPending => BqId is null;

    /// <summary>The derived outstanding-repair quantity of this register (never stored).</summary>
    public int Outstanding => OutstandingProjection.Replay(Movements);

    /// <summary>The ledger in the deterministic physical receipt order.</summary>
    public IReadOnlyList<BoquilhaMovement> Ledger =>
        Movements.OrderBy(movement => movement.RecordedAt)
            .ThenBy(movement => movement.MovementId.Value)
            .ToList();
}