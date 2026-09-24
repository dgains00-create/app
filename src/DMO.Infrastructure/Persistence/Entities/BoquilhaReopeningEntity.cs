namespace DMO.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence entity for the <c>boquilha_reopenings</c> table: one append-only reopen record of
/// one aggregate.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §6.6. Reopen returns the SAME <c>boquilhas_id</c> to <c>active</c>;
/// the record references the exact close being reopened (<c>close_snapshot_id</c> FK RESTRICT), the
/// backend actor/time and the required non-blank human reason. Reopen is a lifecycle fact, never a
/// movement type. Append-only by construction: no UPDATE/DELETE route, repository member or SQL
/// exists for this table.</remarks>
public sealed class BoquilhaReopeningEntity
{
    /// <summary>Primary key.</summary>
    public Guid ReopenId { get; set; }

    /// <summary>The SAME aggregate (FK RESTRICT).</summary>
    public Guid BoquilhasId { get; set; }

    /// <summary>The exact close being reopened (the last close; FK RESTRICT).</summary>
    public Guid CloseSnapshotId { get; set; }

    /// <summary>Backend reopening actor (FK RESTRICT).</summary>
    public Guid ReopenedByUserId { get; set; }

    /// <summary>Backend reopening date/time.</summary>
    public DateTimeOffset ReopenedAt { get; set; }

    /// <summary>Required non-blank reopen reason (CHECK).</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Creation instant (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }
}