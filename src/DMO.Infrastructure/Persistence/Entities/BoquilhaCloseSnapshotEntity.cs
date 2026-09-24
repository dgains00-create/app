namespace DMO.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence entity for the <c>boquilha_close_snapshots</c> table: the immutable close snapshot
/// of one aggregate (one row per close event).
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §6.5. Written in the close transaction on the SAME
/// <c>boquilhas_id</c> (no replacement identity) with the backend closing user/clock and the
/// replay-computed buckets + the manual utilisation still as-is. Immutable after COMMIT: no
/// UPDATE/DELETE path exists; a reopen → close cycle writes a NEW snapshot row. The snapshot is a
/// frozen factual summary at close, NEVER a balance authority — it never feeds any derivation.</remarks>
public sealed class BoquilhaCloseSnapshotEntity
{
    /// <summary>Primary key.</summary>
    public Guid CloseSnapshotId { get; set; }

    /// <summary>The SAME aggregate (FK RESTRICT).</summary>
    public Guid BoquilhasId { get; set; }

    /// <summary>Backend closing user (FK RESTRICT).</summary>
    public Guid ClosedByUserId { get; set; }

    /// <summary>Backend closing date/time.</summary>
    public DateTimeOffset ClosedAt { get; set; }

    /// <summary>The aggregate's Início quantity at close (Σ(início)).</summary>
    public int InitialQuantity { get; set; }

    /// <summary>The aggregate's opening business date at close.</summary>
    public DateOnly OpeningDate { get; set; }

    /// <summary>Derived bucket at close (may be any integer).</summary>
    public int Disponivel { get; set; }

    /// <summary>Derived bucket at close (may be negative — valid projection).</summary>
    public int EmReparacao { get; set; }

    /// <summary>Derived bucket at close.</summary>
    public int Irreparavel { get; set; }

    /// <summary>Accumulated excess at close (CHECK >= 0).</summary>
    public int EntradaExcecional { get; set; }

    /// <summary>The manual utilisation still as of close (CHECK range; NULL allowed).</summary>
    public decimal? UtilisationPercent { get; set; }

    /// <summary>Creation instant (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }
}