namespace DMO.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence entity for the <c>boquilhas</c> table: the register IDENTITY carrier of the
/// movement history of one REAL production/BQ context.
/// </summary>
/// <remarks>
/// Authority: P2-T07 OWNER CLARIFICATION. The row simply identifies the register associated with
/// that production/BQ context (<c>bq_id</c> NOT NULL and UNIQUE — one register per BQ context,
/// DB-enforced; the plain unique key, NO lifecycle/active machinery). There is NO
/// <c>status</c>/<c>tool_id</c>/<c>opening_date</c>/<c>utilisation_percent</c>/
/// <c>observations</c>/<c>version</c>: no lifecycle state machine exists, the standalone anchor and
/// the opening facts are removed, and the register row is never updated (no version token).
/// Creating the register never manufactures a quantity movement — existence is the row itself.
/// </remarks>
public sealed class BoquilhaEntity
{
    /// <summary>Primary key (backend-allocated inside the create transaction).</summary>
    public Guid BoquilhasId { get; set; }

    /// <summary>
    /// The REAL production anchored <c>bq_contexts</c> row (FK RESTRICT; UNIQUE — one register per
    /// BQ context). Never a fake Job On / fake bq id / <c>production_id</c>.
    /// </summary>
    public Guid BqId { get; set; }

    /// <summary>Backend opening actor (FK RESTRICT).</summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>Creation instant (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }
}