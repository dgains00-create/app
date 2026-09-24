namespace DMO.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence entity for the <c>boquilha_movements</c> table: one quantity movement/event on one
/// aggregate.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §6.3. The row is <b>the single quantity event</b>: editing replaces
/// the current values of the same row (version-guarded) and inserts one audit row — no second
/// movement row is ever created. The type CHECK fixes the closed set
/// <c>inicio|saida|entrada|irreparavel</c> (<c>Editar</c> is an action, never a type); quantities
/// are positive whole units; <c>recorded_at</c> is the immutable backend receipt timestamp written
/// exactly once; <c>machine</c>/<c>repairer_id</c> are required on external Saída (CHECK) and
/// historically preserved. Entrada rows carry the expected/excess facts computed by replay inside
/// the append/edit transaction (CHECK-consistent). There is no annulled/deleted column and no
/// delete path.</remarks>
public sealed class BoquilhaMovementEntity
{
    /// <summary>Primary key (backend-allocated inside the append transaction).</summary>
    public Guid MovementId { get; set; }

    /// <summary>The owning aggregate (FK RESTRICT).</summary>
    public Guid BoquilhasId { get; set; }

    /// <summary>The closed-set type token: <c>inicio</c> | <c>saida</c> | <c>entrada</c> | <c>irreparavel</c>.</summary>
    public string MovementType { get; set; } = string.Empty;

    /// <summary>Whole-unit BQ count; always positive (CHECK).</summary>
    public int Quantity { get; set; }

    /// <summary>Operator-editable operational date (never reorders the ledger).</summary>
    public DateOnly BusinessDate { get; set; }

    /// <summary>Immutable system receipt timestamp (never rewritten by any later statement).</summary>
    public DateTimeOffset RecordedAt { get; set; }

    /// <summary>Backend recording actor (FK RESTRICT; unchanged by later edits).</summary>
    public Guid RecordedByUserId { get; set; }

    /// <summary>Line context "where recorded"; required on external Saída (CHECK).</summary>
    public string? Machine { get; set; }

    /// <summary>The final selected canonical repairer; required on external Saída (CHECK); historically preserved.</summary>
    public Guid? RepairerId { get; set; }

    /// <summary>Entrada fact: <c>GREATEST(0, Em reparação before)</c>; NOT NULL iff the movement is an Entrada.</summary>
    public int? ExpectedReturnQuantity { get; set; }

    /// <summary>Entrada fact: <c>GREATEST(0, quantity - expected)</c>; NOT NULL iff the movement is an Entrada.</summary>
    public int? ExcessReceivedQuantity { get; set; }

    /// <summary>Motive/detail/observations "where recorded" (NULL or non-blank).</summary>
    public string? Observations { get; set; }

    /// <summary>Optimistic-concurrency token of the movement (default 1; +1 per committed edit).</summary>
    public int Version { get; set; }

    /// <summary>Creation instant (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Last-update instant (UTC; edited rows only).</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}