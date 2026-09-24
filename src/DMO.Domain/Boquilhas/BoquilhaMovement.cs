using DMO.Domain.Tools;

namespace DMO.Domain.Boquilhas;

/// <summary>
/// One quantity movement/event on one Boquilhas aggregate (a ledger fact row).
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §6.3 and §17.
/// <para>
/// The movement row is <b>the single quantity event</b>: editing never inserts a second row, and
/// the audit history of an edit is a separate <see cref="MovementAuditEntry"/> (never a quantity
/// event). The row carries exactly one of the closed <see cref="MovementKind"/> values; quantities
/// are whole-unit BQ counts and always positive. <c>business_date</c> is the operator-editable
/// operational date; <c>recorded_at</c> is the immutable backend receipt timestamp written exactly
/// once at insertion and never rewritten by any later statement. <c>Machine</c>/<c>RepairerId</c>
/// are required on external Saída and historically preserved — a later assignment change never
/// rewrites this row. For Entrada rows, <c>ExpectedReturnQuantity</c>/<c>ExcessReceivedQuantity</c>
/// are the persisted per-row facts computed by replay inside the append (or edit) transaction.
/// There is no annulled/deleted column and no delete path: the ledger is append-only as a fact set;
/// edits replace the current values of the same row.
/// </para>
/// </remarks>
public sealed record BoquilhaMovement(
    MovementId MovementId,
    Guid BoquilhasId,
    MovementKind Kind,
    int Quantity,
    DateOnly BusinessDate,
    DateTimeOffset RecordedAt,
    Guid RecordedByUserId,
    string? Machine,
    Guid? RepairerId,
    int? ExpectedReturnQuantity,
    int? ExcessReceivedQuantity,
    string? Observations,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>Whether this row is the single Início of its aggregate.</summary>
    public bool IsInicio => Kind == MovementKind.Inicio;

    /// <summary>Whether this row is an Entrada (the only type carrying the expected/excess facts).</summary>
    public bool IsEntrada => Kind == MovementKind.Entrada;
}