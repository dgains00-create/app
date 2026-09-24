namespace DMO.Application.Boquilhas;

/// <summary>
/// The Boquilhas read models: the Registo lot-grid row, the local Histórico row, the aggregate
/// ficha, the movement audit item and the consumed repairer/assignment reads.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §12.1 (result union carriers), §15.5 (ficha shapes), §24.2
/// (HistoryItemReadModel, exact) and §21 (resolution reads).
/// <para>
/// Balance buckets are always <b>derived projections</b> computed by replay at read time — they are
/// never stored and never a second balance authority (§18, AC-B1). Reference/lot facts are traversal
/// facts: frozen <c>bq_contexts</c> triple for production-linked aggregates, live <c>tools</c> facts
/// for standalone aggregates — never a reference/lot copy on the aggregate (Q-REFLOT).</para>
/// </remarks>

/// <summary>
/// One Registo lot-grid row (route 4, §25.1 R1 columns). Balance buckets are replay-derived at
/// read time.
/// </summary>
public sealed record BoquilhaListItem(
    Guid BoquilhasId,
    int Version,
    string State,
    Guid? BqId,
    Guid? ToolId,
    string? Reference,
    string? Lot,
    IReadOnlyList<string> Machines,
    DateOnly OpeningDate,
    int InitialQuantity,
    int Disponivel,
    int EmReparacao,
    int Irreparavel,
    int EntradaExcecional)
{
    /// <summary>Exactly one of the two anchors is non-null (exclusive-anchor truthfulness).</summary>
    public bool HasExclusiveAnchor =>
        (BqId is not null) != (ToolId is not null);
}

/// <summary>The route 4 result carrier item (contract §12.1).</summary>
public sealed record BoquilhaListItemReadModel(
    Guid BoquilhasId,
    int Version,
    string State,
    Guid? BqId,
    Guid? ToolId,
    string? Reference,
    string? Lot,
    IReadOnlyList<string> Machines,
    DateOnly OpeningDate,
    int InitialQuantity,
    int Disponivel,
    int EmReparacao,
    int Irreparavel,
    int EntradaExcecional);

/// <summary>
/// One local Histórico row (route 18, §24.2 exact row shape): the aggregate facts plus the
/// traversal facts and the replay-derived balance of the exact aggregate.
/// </summary>
public sealed record HistoryItem(
    Guid BoquilhasId,
    int Version,
    string State,
    Guid? BqId,
    Guid? ToolId,
    string? Reference,
    string? Lot,
    IReadOnlyList<string> Machines,
    DateOnly OpeningDate,
    int InitialQuantity,
    int Disponivel,
    int EmReparacao,
    int Irreparavel,
    int EntradaExcecional,
    int MovementCount,
    DateTimeOffset? ClosedAt,
    Guid? ClosedByUserId,
    DateTimeOffset? LastMovementAt);

/// <summary>The route 18 result carrier item (contract §12.1 + §24.2).</summary>
public sealed record HistoryItemReadModel(
    Guid BoquilhasId,
    int Version,
    string State,
    Guid? BqId,
    Guid? ToolId,
    string? Reference,
    string? Lot,
    IReadOnlyList<string> Machines,
    DateOnly OpeningDate,
    int InitialQuantity,
    int Disponivel,
    int EmReparacao,
    int Irreparavel,
    int EntradaExcecional,
    int MovementCount,
    DateTimeOffset? ClosedAt,
    Guid? ClosedByUserId,
    DateTimeOffset? LastMovementAt);

/// <summary>
/// The aggregate ficha read model (route 5, §25.1 R2/R5/R6 content): anchor context, machines,
/// opening facts, status/version, derived balance buckets, the movement ledger and the
/// close-snapshot/reopen presence.
/// </summary>
public sealed record BoquilhasFichaReadModel(
    Guid BoquilhasId,
    int Version,
    string State,
    Guid? BqId,
    Guid? ToolId,
    string? Reference,
    string? Lot,
    IReadOnlyList<string> Machines,
    DateOnly OpeningDate,
    decimal? UtilisationPercent,
    string? Observations,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    AnchorContextReadModel? Anchor,
    ProductionContextReadModel? Production,
    BalanceReadModel Balance,
    IReadOnlyList<MovementReadModel> Movements,
    CloseSnapshotReadModel? LastClose,
    ReopeningReadModel? LastReopen)
{
    /// <summary>Whether the aggregate is production-linked (real <c>bq_contexts</c> anchor).</summary>
    public bool IsProductionLinked => BqId is not null && ToolId is null;

    /// <summary>Whether the aggregate is standalone (real canonical BQ Tool anchor).</summary>
    public bool IsStandalone => ToolId is not null && BqId is null;

    /// <summary>The single Início of the ledger, or <c>null</c>.</summary>
    public MovementReadModel? Inicio =>
        Movements.FirstOrDefault(movement => movement.MovementType == "inicio");
}

/// <summary>
/// The anchor context of a ficha: the frozen BQ triple (production-linked, presented as
/// historical/production fact) or the live canonical Tool projection (standalone) — never both,
/// never a copy on the aggregate.
/// </summary>
public sealed record AnchorContextReadModel(
    Guid ToolId,
    string FrozenToolType,
    string FrozenToolReference,
    string FrozenToolLot,
    string? LiveToolReference,
    string? LiveToolLot,
    string? Processo,
    int? ToolQuantity,
    IReadOnlyList<string> CompatibleMachines);

/// <summary>
/// The production-line contextual facts of a production-linked ficha (read-only real Job On
/// context via <c>bq_id → job_ons</c>; null for standalone aggregates — no simulated state).
/// </summary>
public sealed record ProductionContextReadModel(
    string Reference,
    string ProductionNumber,
    string Machine,
    DateOnly? ProductionDate);

/// <summary>The derived balance buckets of a ficha (never stored; §18).</summary>
public sealed record BalanceReadModel(
    int Disponivel,
    int EmReparacao,
    int Irreparavel,
    int EntradaExcecional);

/// <summary>One movement of the ficha ledger, with the display-only per-movement Saldo projection.</summary>
public sealed record MovementReadModel(
    Guid MovementId,
    string MovementType,
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
    int Saldo);

/// <summary>The last close of a ficha, when closed (undefined for an active aggregate).</summary>
public sealed record CloseSnapshotReadModel(
    Guid CloseSnapshotId,
    Guid ClosedByUserId,
    DateTimeOffset ClosedAt,
    int InitialQuantity,
    DateOnly OpeningDate,
    int Disponivel,
    int EmReparacao,
    int Irreparavel,
    int EntradaExcecional,
    decimal? UtilisationPercent);

/// <summary>The last reopen record of a ficha, when present.</summary>
public sealed record ReopeningReadModel(
    Guid ReopenId,
    Guid CloseSnapshotId,
    Guid ReopenedByUserId,
    DateTimeOffset ReopenedAt,
    string Reason);

/// <summary>
/// One movement edit/audit item (route 6 / R5 AuditTrail): the exact before/after values of every
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
/// One current machine → repairer assignment read (route 16, §21.3): an absent assignment row is
/// the explicit <c>assignmentUnavailable</c> state — never an error, never a default repairer.
/// </summary>
public sealed record MachineRepairerAssignmentReadModel(
    string Machine,
    Guid? RepairerId,
    string? RepairerName,
    bool AssignmentUnavailable);

/// <summary>One repairer-register read (route 17; consumed, never administered).</summary>
public sealed record RepairerReadModel(Guid RepairerId, string Name);