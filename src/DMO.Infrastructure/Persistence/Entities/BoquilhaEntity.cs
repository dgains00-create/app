namespace DMO.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence entity for the <c>boquilhas</c> table: one collective BQ external-repair aggregate.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §6.1. The exclusive anchor (<c>bq_id</c> production-linked |
/// <c>tool_id</c> standalone) is DB-enforced by <c>boquilhas_anchor_exclusive_check</c>; the two
/// ACTIVE partial unique indexes (<c>IX_boquilhas_active_bq_id</c> /
/// <c>IX_boquilhas_active_tool_id</c>) are the race-safe one-active-per-anchor backstop (§7.2).
/// Status is aggregate lifecycle ('active'|'closed'), never a movement type. There is deliberately
/// no reference/lot copy, no <c>jobon_id</c>, no machine/linha column, no balance column and no
/// close/reopen column: those facts live on the anchor/child tables and are composed at read time.
/// </remarks>
public sealed class BoquilhaEntity
{
    /// <summary>Primary key (backend-allocated inside the create transaction).</summary>
    public Guid BoquilhasId { get; set; }

    /// <summary>Production-linked anchor: a real frozen <c>bq_contexts</c> row (FK RESTRICT).</summary>
    public Guid? BqId { get; set; }

    /// <summary>Standalone anchor: a real canonical BQ <c>tools</c> row (FK RESTRICT).</summary>
    public Guid? ToolId { get; set; }

    /// <summary>Aggregate lifecycle status: <c>active</c> | <c>closed</c>.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Operator-editable opening business date (default today at creation).</summary>
    public DateOnly OpeningDate { get; set; }

    /// <summary>The manual <c>% utilização</c> still (0–100 or NULL; never derived).</summary>
    public decimal? UtilisationPercent { get; set; }

    /// <summary>Compact opening observations (NULL or non-blank).</summary>
    public string? Observations { get; set; }

    /// <summary>Backend opening actor (FK RESTRICT).</summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>Optimistic-concurrency token (default 1; +1 per committed guarded mutation).</summary>
    public int Version { get; set; }

    /// <summary>Creation instant (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Last-update instant (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}