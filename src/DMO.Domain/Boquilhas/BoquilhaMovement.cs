namespace DMO.Domain.Boquilhas;

/// <summary>
/// One quantity movement/event on one Boquilhas register (a ledger fact row).
/// </summary>
/// <remarks>
/// Authority: P2-T07 OWNER CLARIFICATION + the preserved §6.3 shapes.
/// <para>
/// The movement row is <b>the single quantity event</b>: editing never inserts a second row, and
/// the audit history of an edit is a separate <see cref="MovementAuditEntry"/> (never a quantity
/// event). The row carries exactly one of the closed three <see cref="MovementKind"/> values
/// (Saída / Entrada / Entrada sem reparação); quantities are whole-unit BQ counts and always
/// positive. <c>business_date</c> is the operator-editable operational date — it may be LATER than
/// the production end date (movements after production has ended are valid, the production remains
/// the historical context); <c>recorded_at</c> is the immutable backend receipt timestamp written
/// exactly once. <c>Machine</c>/<c>RepairerId</c> are required on Saída and historically preserved —
/// a later assignment change never rewrites this row. Entrada sem reparação is a NORMAL movement
/// with a distinct meaning: it returns quantity from repair and records the returned boquilhas were
/// not repaired — it never mutates the Tool identity/state. There is no delete path; edits replace
/// the current values of the same row.
/// </para>
/// <para>
/// <b>Superseded (Owner clarification):</b> the Início movement type (never manufactured by the
/// register), the Irreparável movement type/semantics and the Entrada expected/excess facts
/// (computed vs <c>Em reparação</c> before) are removed.
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
    string? Observations,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    /// <summary>Whether this row is an external repair dispatch (Saída).</summary>
    public bool IsSaida => Kind == MovementKind.Saida;

    /// <summary>Whether this row is a return from the repairer (Entrada or Entrada sem reparação).</summary>
    public bool IsReturn => Kind is MovementKind.Entrada or MovementKind.EntradaSemReparacao;
}