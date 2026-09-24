namespace DMO.Domain.Boquilhas;

/// <summary>
/// One reopen record of one Boquilhas aggregate (append-only lifecycle history).
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §6.6 and §23.3.
/// <para>
/// Reopen returns the <b>same</b> <c>boquilhas_id</c> to <c>active</c>; no replacement aggregate is
/// ever created, and prior movements, audits, snapshots and reopen records are never rewritten. The
/// record references the exact close being reopened (<c>CloseSnapshotId</c>) and carries the
/// backend actor (<c>ReopenedByUserId</c>), the backend clock (<c>ReopenedAt</c>) and the required
/// non-blank human <c>reason</c>. Reopen is a lifecycle fact, <b>never</b> a movement type.
/// </para>
/// <para>
/// Rows are immutable after COMMIT: no UPDATE/DELETE route, repository member or SQL exists for
/// this table.
/// </para>
/// </remarks>
public sealed record ReopeningRecord(
    Guid ReopenId,
    Guid BoquilhasId,
    Guid CloseSnapshotId,
    Guid ReopenedByUserId,
    DateTimeOffset ReopenedAt,
    string Reason,
    DateTimeOffset CreatedAt);