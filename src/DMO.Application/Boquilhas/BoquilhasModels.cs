using DMO.Domain.Boquilhas;

namespace DMO.Application.Boquilhas;

/// <summary>
/// The Boquilhas command/query carriers and the closed result set.
/// </summary>
/// <remarks>
/// Authority: P2-T07 OWNER CLARIFICATION (the production movement register), preserving the useful
/// edit/audit/dates/repairer behaviors.
/// <para>
/// Identity discipline: <c>boquilhas_id</c> and <c>movement_id</c> never appear in any carrier as a
/// client-supplied value <b>for creation</b> — the backend allocates both inside the write
/// transactions. Every register anchors the REAL production association (<c>bq_id</c>, never a fake
/// Job On / fake bq id / <c>production_id</c>). Movement type and <c>recorded_at</c> are immutable
/// and appear in no edit carrier. There is no lifecycle carrier: no close/reopen/opening-facts
/// commands exist.</para>
/// <para>
/// <b>Superseded (Owner clarification):</b> the open/closed lifecycle commands (close/reopen/
/// opening-facts), the standalone anchor, the initial-quantity Início and the balance-relative
/// carriers (Saída ≤ Disponível / Irreparável ≤ Em reparação / expected-excess facts) are removed.
/// The register creation carries NO quantity.</para>
/// </remarks>

// ------------------------------------------------------------------ queries

/// <summary>
/// The register list query (route 1): all Boquilhas registers with their production context and
/// the derived outstanding; optional reference/lot filters; 1-based paging.
/// </summary>
public sealed record BoquilhasListQuery(
    string? Reference,
    string? Lot,
    int Page = 1,
    int PageSize = 50);

/// <summary>
/// The local Histórico query (route 12): the movement-level chronological history with the
/// production context; filters are backend SQL predicates. Unknown/ill-formed values →
/// 400 <c>FILTER_INVALID</c> (never a silent full list).
/// </summary>
public sealed record BoquilhasHistoryQuery(
    string? Reference,
    string? Lot,
    string? Machine,
    DateOnly? BusinessDateFrom,
    DateOnly? BusinessDateTo,
    string? MovementType,
    Guid? RepairerId,
    int Page = 1,
    int PageSize = 50);

// ------------------------------------------------------------------ commands

/// <summary>
/// The register creation command (route 4): creates the register IDENTITY of one REAL production
/// BQ context — no quantity movement is ever manufactured to establish existence.
/// </summary>
public sealed record CreateBoquilhaRegisterCommand(
    Guid BqId,
    Guid CreatedByUserId);

/// <summary>
/// The movement-append command (route 5): one of the closed three type tokens, a positive
/// whole-unit quantity, the operator-editable business date (may be later than the production end
/// date), the optional machine line context and the repairer (both required on Saída).
/// </summary>
public sealed record AppendMovementCommand(
    Guid BoquilhasId,
    string MovementType,
    int Quantity,
    DateOnly BusinessDate,
    string? Machine,
    Guid? RepairerId,
    string? Observations);

/// <summary>
/// The movement-edit command (route 6): the SAME <c>movement_id</c>, the observed movement version
/// and the new values of the editable fields only. Movement type and <c>recorded_at</c> are
/// immutable and not carried.
/// </summary>
public sealed record EditMovementCommand(
    Guid BoquilhasId,
    Guid MovementId,
    int ExpectedMovementVersion,
    int Quantity,
    DateOnly BusinessDate,
    string? Machine,
    Guid? RepairerId,
    string? Observations);

/// <summary>
/// The BQ-context association command (route 9): creates the missing <c>bq_contexts</c> row
/// exclusively through <c>IJobOnService.UpdateAsync</c> (Keep every fact + Set the BQ slot).
/// </summary>
public sealed record AssociateBqCommand(
    Guid JobOnId,
    Guid ToolId,
    int ExpectedJobOnVersion);

// ---------------------------------------------------------------- repository write units

/// <summary>
/// The register-creation unit of <c>IBoquilhasRepository.CreatedAsync</c>: the production anchor
/// (a REAL <c>bq_contexts</c> row) and the backend actor; the register identity row carries NO
/// quantity event.
/// </summary>
public sealed record BoqCreateUnit(
    Guid BqId,
    Guid CreatedByUserId);

/// <summary>
/// The append unit of <c>IBoquilhasRepository.AppendMovementAsync</c>: the movement facts; the
/// repository allocates <c>movement_id</c> and <c>recorded_at</c> inside the transaction (the
/// register version machinery is removed — the ledger is a historical fact set).
/// </summary>
public sealed record BoqAppendUnit(
    Guid BoquilhasId,
    string MovementType,
    int Quantity,
    DateOnly BusinessDate,
    string? Machine,
    Guid? RepairerId,
    string? Observations,
    Guid RecordedByUserId);

/// <summary>
/// The edit unit of <c>IBoquilhasRepository.EditMovementAsync</c>: the observed movement version
/// and the new values. The repository performs the guarded UPDATE of the SAME row + the audit row
/// in one transaction — no second movement row, no second quantity event.
/// </summary>
public sealed record BoqEditUnit(
    Guid BoquilhasId,
    Guid MovementId,
    int ExpectedMovementVersion,
    int Quantity,
    DateOnly BusinessDate,
    string? Machine,
    Guid? RepairerId,
    string? Observations,
    Guid EditedByUserId);

/// <summary>The edit outcome: the edited SAME movement row.</summary>
public sealed record BoqEditResult(BoquilhaMovement Movement);

/// <summary>The typed reason of a Boquilhas refusal (the exact transport tokens in the endpoints).</summary>
public enum BoquilhasRefusalReason
{
    /// <summary>The row changed after the operator observed it; nothing written. (409 <c>stale-version</c>)</summary>
    StaleVersion,

    /// <summary>A register for this BQ context already exists. (409 <c>register-exists</c>)</summary>
    RegisterExists,
}

/// <summary>
/// The closed Boquilhas result set.
/// </summary>
/// <remarks>
/// No result type carries a lifecycle status, a document state, a permission decision or a
/// navigation target.</remarks>
public abstract record BoquilhasResult
{
    private BoquilhasResult()
    {
    }

    /// <summary>The register list (route 1).</summary>
    public sealed record ListFound(IReadOnlyList<RegisterListItemReadModel> Rows, int Total) : BoquilhasResult;

    /// <summary>The local Histórico list — movement-level rows with their production context (route 12).</summary>
    public sealed record HistoryFound(IReadOnlyList<HistoryMovementItemReadModel> Rows, int Total) : BoquilhasResult;

    /// <summary>The register ficha (route 2).</summary>
    /// <remarks>The positional member is named <c>Value</c> (the accepted nested-type/member rule).</remarks>
    public sealed record Ficha(RegisterFichaReadModel Value) : BoquilhasResult;

    /// <summary>The per-movement edit/audit trail (route 3).</summary>
    public sealed record MovementAuditFound(Guid MovementId, IReadOnlyList<MovementAuditItemReadModel> Entries) : BoquilhasResult;

    /// <summary>The register identity was created (route 4) — NO quantity event.</summary>
    public sealed record RegisterCreated(Guid BoquilhasId) : BoquilhasResult;

    /// <summary>The movement was appended (route 5; the single quantity event).</summary>
    public sealed record MovementAppended(Guid MovementId, int Version) : BoquilhasResult;

    /// <summary>The SAME movement row was edited (route 6; no second quantity event).</summary>
    public sealed record MovementEdited(Guid MovementId, int Version) : BoquilhasResult;

    /// <summary>The reference → productions read of the association flow (composed from <c>IJobOnService</c>).</summary>
    public sealed record ProductionsFound(IReadOnlyList<JobOn.JobOnProductionListItem> Productions) : BoquilhasResult;

    /// <summary>The Job On ficha read (composed from <c>IJobOnService</c>).</summary>
    /// <remarks>The positional member is named <c>Value</c> (same nested-type/member rule).</remarks>
    public sealed record JobOnFichaFound(JobOn.JobOnFicha Value) : BoquilhasResult;

    /// <summary>The BQ context was created/updated by Job On's own code; the real <c>bq_id</c> is returned.</summary>
    public sealed record BqAssociated(Guid JobOnId, Guid BqId, int JobOnVersion) : BoquilhasResult;

    /// <summary>The six current machine → repairer assignments (consumed read, route 10).</summary>
    public sealed record AssignmentsFound(IReadOnlyList<MachineRepairerAssignmentReadModel> Assignments) : BoquilhasResult;

    /// <summary>The repairer register (consumed read, route 11).</summary>
    public sealed record RepairersFound(IReadOnlyList<RepairerReadModel> Repairers) : BoquilhasResult;

    /// <summary>The exact contracted validation codes; nothing was written.</summary>
    public sealed record ValidationFailed(IReadOnlyList<string> Errors) : BoquilhasResult;

    /// <summary>The requested register, production or trail does not exist.</summary>
    public sealed record NotFound(Guid Id) : BoquilhasResult;

    /// <summary>A typed, actionable refusal; nothing was written.</summary>
    public sealed record Refused(BoquilhasRefusalReason Reason, string Message) : BoquilhasResult;
}