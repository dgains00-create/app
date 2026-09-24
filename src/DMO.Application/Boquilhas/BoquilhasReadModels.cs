using DMO.Domain.Boquilhas;

namespace DMO.Application.Boquilhas;

/// <summary>
/// The Boquilhas read models: the register list row, the local Histórico movement row, the
/// register ficha, the movement audit item and the consumed repairer/assignment reads.
/// </summary>
/// <remarks>
/// Authority: P2-T07 OWNER CLARIFICATION (the production movement register).
/// <para>
/// The outstanding repair quantity is always a <b>derived projection</b> computed by replay at
/// read time — never stored and never a second balance authority. Reference/lot/production facts
/// are traversal facts through the REAL <c>bq_contexts → job_ons</c> chain: the frozen BQ triple
/// plus the real production facts — never a copy on the register.</para>
/// <para>
/// <b>Superseded (Owner clarification):</b> the balance buckets (Disponível / Em reparação /
/// Irreparável / Entrada excecional), the lifecycle state token and the machine set read shapes
/// are removed; the single derived value is the outstanding repair quantity.</para>
/// </remarks>

/// <summary>One register-list row (route 1): the production context + the derived outstanding.</summary>
public sealed record RegisterListItem(
    Guid BoquilhasId,
    Guid BqId,
    string? Reference,
    string? Lot,
    string? ProductionNumber,
    string? ProductionMachine,
    DateOnly? ProductionDate,
    int Outstanding,
    int MovementCount,
    DateTimeOffset? LastMovementAt)
{
    /// <summary>Whether the row carries the REAL production context (always true — every register
    /// belongs to a real Job On/BQ context).</summary>
    public bool HasProductionContext => ProductionNumber is not null;
}

/// <summary>The route 1 result carrier item.</summary>
public sealed record RegisterListItemReadModel(
    Guid BoquilhasId,
    Guid BqId,
    string? Reference,
    string? Lot,
    string? ProductionNumber,
    string? ProductionMachine,
    DateOnly? ProductionDate,
    int Outstanding,
    int MovementCount,
    DateTimeOffset? LastMovementAt);

/// <summary>
/// One local Histórico row (route 12): ONE MOVEMENT with its register's production context —
/// chronological movement history (production/BQ context → movement history).
/// </summary>
public sealed record HistoryMovementItem(
    Guid MovementId,
    Guid BoquilhasId,
    string MovementType,
    int Quantity,
    DateOnly BusinessDate,
    DateTimeOffset RecordedAt,
    Guid RecordedByUserId,
    string? Machine,
    Guid? RepairerId,
    string? Observations,
    string? Reference,
    string? Lot,
    string? ProductionNumber,
    string? ProductionMachine)
{
    /// <summary>Whether this movement is an Entrada sem reparação (remains explicitly identifiable).</summary>
    public bool IsEntradaSemReparacao => string.Equals(MovementType, "entrada_sem_reparacao", StringComparison.Ordinal);
}

/// <summary>The route 12 result carrier item.</summary>
public sealed record HistoryMovementItemReadModel(
    Guid MovementId,
    Guid BoquilhasId,
    string MovementType,
    int Quantity,
    DateOnly BusinessDate,
    DateTimeOffset RecordedAt,
    Guid RecordedByUserId,
    string? Machine,
    Guid? RepairerId,
    string? Observations,
    string? Reference,
    string? Lot,
    string? ProductionNumber,
    string? ProductionMachine);

/// <summary>
/// The register ficha read model (route 2): the production context, the movement ledger and the
/// derived outstanding — NO lifecycle state.
/// </summary>
public sealed record RegisterFichaReadModel(
    Guid BoquilhasId,
    Guid BqId,
    string? Reference,
    string? Lot,
    int Outstanding,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    AnchorContextReadModel? Anchor,
    ProductionContextReadModel? Production,
    IReadOnlyList<MovementReadModel> Movements)
{
    /// <summary>The movement-type label of one ledger row (presentation only).</summary>
    public string Label(string movementType) =>
        MovementKindTokens.Parse(movementType) is { } kind
            ? MovementKindTokens.ToLabel(kind)
            : movementType;
}

/// <summary>
/// The anchor context of a register: the frozen BQ triple (presented as historical/production
/// fact) with the direct canonical Tool relation — never a copy on the register.
/// </summary>
public sealed record AnchorContextReadModel(
    Guid ToolId,
    string FrozenToolType,
    string FrozenToolReference,
    string FrozenToolLot);

/// <summary>
/// The real production context of the register (via <c>bq_id → job_ons</c>): applying a movement
/// AFTER the production end date stays valid — the production remains the historical context.
/// </summary>
public sealed record ProductionContextReadModel(
    string Reference,
    string ProductionNumber,
    string Machine,
    DateOnly? ProductionDate);

/// <summary>One movement of the register ledger, with the display-only cumulative saldo projection.</summary>
public sealed record MovementReadModel(
    Guid MovementId,
    string MovementType,
    int Quantity,
    DateOnly BusinessDate,
    DateTimeOffset RecordedAt,
    Guid RecordedByUserId,
    string? Machine,
    Guid? RepairerId,
    string? Observations,
    int Version,
    int Saldo);

/// <summary>
/// One movement edit/audit item (route 3 / R5 AuditTrail): the exact before/after values of every
/// editable field plus the backend editor/time. Audit entries never render as movements.
/// </summary>
public sealed record MovementAuditItemReadModel(
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
    string? AfterObservations);

/// <summary>
/// One current machine → repairer assignment read (route 10): an absent assignment row is the
/// explicit <c>assignmentUnavailable</c> state — never an error, never a default repairer.
/// </summary>
public sealed record MachineRepairerAssignmentReadModel(
    string Machine,
    Guid? RepairerId,
    string? RepairerName,
    bool AssignmentUnavailable);

/// <summary>One repairer-register read (route 11; consumed, never administered).</summary>
public sealed record RepairerReadModel(Guid RepairerId, string Name);