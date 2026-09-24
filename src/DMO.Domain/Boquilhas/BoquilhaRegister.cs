namespace DMO.Domain.Boquilhas;

/// <summary>
/// One Boquilhas production movement register: the identity carrier of the movement history of one
/// REAL production/BQ context.
/// </summary>
/// <remarks>
/// Authority: P2-T07 OWNER CLARIFICATION.
/// <para>
/// A Boquilhas register groups the movement history belonging to that production/BQ context
/// (<c>boquilhas_id → bq_id → jobon_id + tool_id</c>; <c>bq_id</c> is a REAL frozen
/// <c>bq_contexts</c> row — one register per BQ context, DB-enforced by the <c>bq_id</c> unique
/// key). <c>boquilhas_id</c> is the technical identity of the register ONLY — it does NOT imply
/// active/closed/reopened state: there is no lifecycle state machine at all. Creating the register
/// never manufactures a quantity movement (no Início); movements may be recorded while the
/// production runs AND after the production has ended — the movement's own
/// <c>business_date</c>/<c>recorded_at</c> record when the movement happened.
/// </para>
/// <para>
/// <b>Superseded (Owner clarification):</b> the open/closed aggregate workflow (status
/// active/closed, close/reopen actions, close snapshots, reopening history,
/// one-active-aggregate-per-anchor uniqueness and its race machinery) and the standalone
/// (<c>tool_id</c>) anchor are removed — Boquilhas is a historical movement register associated
/// with a REAL production, not an open/closed workflow.
/// </para>
/// </remarks>
public sealed record BoquilhaRegister(
    BoquilhasId BoquilhasId,
    Guid BqId,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<BoquilhaMovement> Movements)
{
    /// <summary>The derived outstanding-repair quantity of this register (never stored).</summary>
    public int Outstanding => OutstandingProjection.Replay(Movements);

    /// <summary>The ledger in the deterministic physical receipt order.</summary>
    public IReadOnlyList<BoquilhaMovement> Ledger =>
        Movements.OrderBy(movement => movement.RecordedAt)
            .ThenBy(movement => movement.MovementId.Value)
            .ToList();
}