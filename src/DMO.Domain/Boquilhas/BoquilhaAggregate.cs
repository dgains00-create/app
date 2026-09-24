using DMO.Domain.Tools;

namespace DMO.Domain.Boquilhas;

/// <summary>
/// One collective BQ external-repair aggregate (one repair trace) with its child facts.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §6.1 and §15.
/// <para>
/// The aggregate anchors to <b>exactly one</b> of the two real relations — <c>BqId</c>
/// (production-linked: a real frozen <c>bq_contexts</c> row, through which <c>jobon_id</c> and the
/// canonical <c>tool_id</c> are reachable) or <c>ToolId</c> (standalone: a real canonical BQ
/// <c>tools</c> row) — DB-enforced by <c>boquilhas_anchor_exclusive_check</c>. It deliberately
/// stores <b>no</b> reference/lot copy (reached through the anchor), no <c>jobon_id</c> column, no
/// machine/linha column (the machine set is the child <c>boquilha_machines</c> table), no
/// <c>linha_atual</c>, no balance columns (never stored) and no close/reopen columns (child rows).
/// </para>
/// <para>
/// <c>UtilisationPercent</c> is the one manual <c>% utilização</c> still: operator-entered at
/// opening, editable via the opening-facts route, 0–100 or NULL, never derived from movements,
/// never sync'd to/from the Ferramentas live reading, never a progress bar. <c>CreatedByUserId</c>
/// is the backend opening actor. The row carries the optimistic-concurrency version token; the
/// child rows (machines/audit/snapshot/reopen) carry none — the parent's version protects their
/// writes.
/// </para>
/// </remarks>
public sealed record BoquilhaAggregate(
    BoquilhasId BoquilhasId,
    Guid? BqId,
    Guid? ToolId,
    BoquilhaStatus Status,
    DateOnly OpeningDate,
    decimal? UtilisationPercent,
    string? Observations,
    Guid CreatedByUserId,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<MachineCode> Machines,
    IReadOnlyList<BoquilhaMovement> Movements,
    IReadOnlyList<CloseSnapshot> CloseSnapshots,
    IReadOnlyList<ReopeningRecord> Reopenings)
{
    /// <summary>Whether this aggregate is production-linked (anchors a real <c>bq_contexts</c> row).</summary>
    public bool IsProductionLinked => BqId is not null && ToolId is null;

    /// <summary>Whether this aggregate is standalone (anchors a real canonical BQ Tool).</summary>
    public bool IsStandalone => ToolId is not null && BqId is null;

    /// <summary>Whether the trace currently accepts movements/edits/opening-fact updates.</summary>
    public bool IsActive => Status == BoquilhaStatus.Active;

    /// <summary>Whether the trace has a committed immutable close snapshot.</summary>
    public bool IsClosed => Status == BoquilhaStatus.Closed;

    /// <summary>The ledger in the contract's deterministic replay order (physical receipt order).</summary>
    public IReadOnlyList<BoquilhaMovement> Ledger =>
        Movements.OrderBy(movement => movement.RecordedAt)
            .ThenBy(movement => movement.MovementId.Value)
            .ToList();

    /// <summary>The derived balance buckets of this aggregate (never stored; §18).</summary>
    public BalanceProjection Balance => BalanceProjection.Replay(Movements);

    /// <summary>The single Início movement of the aggregate, or <c>null</c>.</summary>
    public BoquilhaMovement? Inicio =>
        Movements.FirstOrDefault(movement => movement.Kind == MovementKind.Inicio);

    /// <summary>The most recent close snapshot of this aggregate, or <c>null</c>.</summary>
    public CloseSnapshot? LastClose =>
        CloseSnapshots.OrderByDescending(snapshot => snapshot.ClosedAt)
            .ThenByDescending(snapshot => snapshot.CloseSnapshotId)
            .FirstOrDefault();

    /// <summary>The most recent reopen record of this aggregate, or <c>null</c>.</summary>
    public ReopeningRecord? LastReopen =>
        Reopenings.OrderByDescending(reopen => reopen.ReopenedAt)
            .ThenByDescending(reopen => reopen.ReopenId)
            .FirstOrDefault();
}