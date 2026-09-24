using DMO.Domain.Tools;

namespace DMO.Domain.Boquilhas;

/// <summary>
/// One before/after edit-audit entry of one movement (append-only by construction).
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §6.4 and §19.
/// <para>
/// One row per edit of a movement, written <b>in the same transaction</b> as the movement UPDATE.
/// It carries the exact before/after values of every editable field (quantity, business_date,
/// machine, repairer_id, observations) plus the <b>backend</b> editor and the <b>backend</b>
/// timestamp — never client-supplied. It is <b>audit history, not a quantity event</b>: it has no
/// quantity-balance effect and is never rendered as a movement row. There is deliberately no
/// movement-type before/after pair: <c>movement_type</c> is immutable — nothing to audit.
/// </para>
/// <para>
/// Rows are immutable after COMMIT: no UPDATE/DELETE route, repository member or SQL exists for
/// this table.
/// </para>
/// </remarks>
public sealed record MovementAuditEntry(
    Guid MovementAuditId,
    Guid MovementId,
    Guid EditedByUserId,
    DateTimeOffset EditedAt,
    int BeforeQuantity,
    int AfterQuantity,
    DateOnly BeforeBusinessDate,
    DateOnly AfterBusinessDate,
    string? BeforeMachine,
    string? AfterMachine,
    Guid? BeforeRepairerId,
    Guid? AfterRepairerId,
    string? BeforeObservations,
    string? AfterObservations,
    DateTimeOffset CreatedAt);