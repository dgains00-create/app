using DMO.Domain.Boquilhas;

namespace DMO.Application.Boquilhas;

/// <summary>
/// The Boquilhas command/query carriers and the closed result set.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §9, §12, §17.2, §19.1, §22.2, §23.2–§23.4 and §24.2.
/// <para>
/// Identity discipline: <c>boquilhas_id</c> and <c>movement_id</c> never appear in any carrier as a
/// client-supplied value <b>for creation</b> — the backend allocates both inside the write
/// transactions. No carrier carries <c>jobon_id</c>, a <c>production_id</c>, a client-minted
/// identity, an actor/time fact or a balance value. Movement type and <c>recorded_at</c> are
/// immutable and appear in no edit carrier (§19.1, AC-E6).</para>
/// </remarks>

// ------------------------------------------------------------------ queries (§13.2/§24.2)

/// <summary>
/// The Registo lot-grid query (route 4): active default, state/reference/lot/machine filters,
/// 1-based paging. Filters are always backend SQL predicates — never client-side filtering.
/// </summary>
public sealed record BoquilhasListQuery(
    string? State,
    string? Reference,
    string? Lot,
    string? Machine,
    int Page = 1,
    int PageSize = 50);

/// <summary>
/// The local Histórico query (route 18): the exact §24.2 filter set, every filter a backend SQL
/// predicate. Unknown/ill-formed values → 400 <c>FILTER_INVALID</c> (never a silent full list).
/// Page is 1-based, <c>1 &lt;= PageSize &lt;= 100</c>.
/// </summary>
public sealed record BoquilhasHistoryQuery(
    string? State,
    string? Reference,
    string? Lot,
    string? Machine,
    DateOnly? BusinessDateFrom,
    DateOnly? BusinessDateTo,
    string? MovementType,
    Guid? RepairerId,
    int Page = 1,
    int PageSize = 50);

// ------------------------------------------------------------------ commands (§9/§17/§22/§23)

/// <summary>
/// The aggregate opening command (route 7, §22.2): exactly one anchor, one-or-more machines,
/// a positive initial quantity (the Início), the opening business date (default today at the
/// surface), the optional manual utilisation still and compact observations.
/// </summary>
public sealed record CreateBoquilhasCommand(
    Guid? BqId,
    Guid? ToolId,
    IReadOnlyList<string> Machines,
    int InitialQuantity,
    DateOnly OpeningDate,
    decimal? UtilisationPercent,
    string? Observations,
    Guid CreatedByUserId);

/// <summary>
/// The movement-append command (route 8, §17.2): the observed aggregate version, the closed-set
/// type token, a positive whole-unit quantity, the operator-editable business date, the optional
/// machine line context (required on external Saída) and the final selected repairer (required on
/// external Saída).
/// </summary>
public sealed record AppendMovementCommand(
    Guid BoquilhasId,
    int ExpectedAggregateVersion,
    string MovementType,
    int Quantity,
    DateOnly BusinessDate,
    string? Machine,
    Guid? RepairerId,
    string? Observations);

/// <summary>
/// The movement-edit command (route 9, §19.1): the SAME <c>movement_id</c>, the observed aggregate +
/// movement versions and the new values of the editable fields only. Movement type and
/// <c>recorded_at</c> are immutable and not carried (AC-E6).
/// </summary>
public sealed record EditMovementCommand(
    Guid BoquilhasId,
    int ExpectedAggregateVersion,
    Guid MovementId,
    int ExpectedMovementVersion,
    int Quantity,
    DateOnly BusinessDate,
    string? Machine,
    Guid? RepairerId,
    string? Observations);

/// <summary>The close command (route 10, §23.2): identity + observed version only.</summary>
public sealed record CloseBoquilhasCommand(Guid BoquilhasId, int ExpectedVersion);

/// <summary>The reopen command (route 11, §23.3): identity + observed version + required reason.</summary>
public sealed record ReopenBoquilhasCommand(Guid BoquilhasId, int ExpectedVersion, string Reason);

/// <summary>
/// The opening-facts update command (route 12, §23.4): the operator-editable aggregate context —
/// opening business date, manual utilisation still, observations and the machine set (replace).
/// Never touches movements, never changes balance.
/// </summary>
public sealed record UpdateOpeningFactsCommand(
    Guid BoquilhasId,
    int ExpectedVersion,
    DateOnly OpeningDate,
    decimal? UtilisationPercent,
    string? Observations,
    IReadOnlyList<string> Machines);

/// <summary>
/// The BQ-context association command (route 15, §22.3): creates the missing <c>bq_contexts</c> row
/// exclusively through <c>IJobOnService.UpdateAsync</c> (Keep every fact + Set the BQ slot).
/// </summary>
public sealed record AssociateBqCommand(
    Guid JobOnId,
    Guid ToolId,
    int ExpectedJobOnVersion);

// ---------------------------------------------------------------- repository write units (§8.2)

/// <summary>
/// The create unit of <c>IBoquilhasRepository.CreatedAsync</c>: the anchor, the machine set, the
/// opening facts and the backend opening actor. The repository allocates <c>boquilhas_id</c>, the
/// Início <c>movement_id</c> and the timestamps inside the single create transaction (§10/§22.2).
/// </summary>
public sealed record BoqCreateUnit(
    Guid? BqId,
    Guid? ToolId,
    IReadOnlyList<string> Machines,
    int InitialQuantity,
    DateOnly OpeningDate,
    decimal? UtilisationPercent,
    string? Observations,
    Guid CreatedByUserId);

/// <summary>
/// The append unit of <c>IBoquilhasRepository.AppendMovementAsync</c>: the observed aggregate
/// version and the movement facts; the repository allocates <c>movement_id</c> and
/// <c>recorded_at</c> and computes the Entrada expected/excess facts inside the transaction.
/// </summary>
public sealed record BoqAppendUnit(
    Guid BoquilhasId,
    int ExpectedAggregateVersion,
    string MovementType,
    int Quantity,
    DateOnly BusinessDate,
    string? Machine,
    Guid? RepairerId,
    string? Observations,
    Guid RecordedByUserId);

/// <summary>
/// The edit unit of <c>IBoquilhasRepository.EditMovementAsync</c>: both observed versions and the
/// new values. The repository performs the guarded UPDATE of the SAME row + the audit row in one
/// transaction — no second movement row, no second quantity event (§10/§19).
/// </summary>
public sealed record BoqEditUnit(
    Guid BoquilhasId,
    int ExpectedAggregateVersion,
    Guid MovementId,
    int ExpectedMovementVersion,
    int Quantity,
    DateOnly BusinessDate,
    string? Machine,
    Guid? RepairerId,
    string? Observations,
    Guid EditedByUserId);

/// <summary>The close unit of <c>IBoquilhasRepository.CloseAsync</c> (§23.2).</summary>
public sealed record BoqCloseUnit(Guid BoquilhasId, int ExpectedVersion, Guid ClosedByUserId);

/// <summary>The reopen unit of <c>IBoquilhasRepository.ReopenAsync</c> (§23.3).</summary>
public sealed record BoqReopenUnit(
    Guid BoquilhasId,
    int ExpectedVersion,
    string Reason,
    Guid ReopenedByUserId);

/// <summary>The opening-facts unit of <c>IBoquilhasRepository.UpdateOpeningFactsAsync</c> (§23.4).</summary>
public sealed record BoqOpeningFactsUnit(
    Guid BoquilhasId,
    int ExpectedVersion,
    DateOnly OpeningDate,
    decimal? UtilisationPercent,
    string? Observations,
    IReadOnlyList<string> Machines);

/// <summary>The edit outcome: the edited SAME movement row and the aggregate version after the write.</summary>
public sealed record BoqEditResult(
    BoquilhaMovement Movement,
    int AggregateVersion);

/// <summary>The close outcome: the immutable snapshot row and the new aggregate version.</summary>
public sealed record BoqCloseResult(CloseSnapshot Snapshot, int AggregateVersion);

/// <summary>The reopen outcome: the new append-only reopen row and the new aggregate version.</summary>
public sealed record BoqReopenResult(ReopeningRecord Reopen, int AggregateVersion);

/// <summary>The typed reason of a Boquilhas refusal (the exact transport tokens are in §12.3).</summary>
public enum BoquilhasRefusalReason
{
    /// <summary>The row changed after the operator observed it; nothing written. (409 <c>stale-version</c>)</summary>
    StaleVersion,

    /// <summary>The Saída quantity exceeds Disponível. (409 <c>saida-exceeds-available</c>)</summary>
    SaidaExceedsAvailable,

    /// <summary>The Irreparável quantity exceeds Em reparação. (409 <c>irreparavel-exceeds-in-repair</c>)</summary>
    IrreparavelExceedsInRepair,

    /// <summary>
    /// An active aggregate already exists for the same anchor (application pre-check or the DB
    /// partial unique index mapped from 23505 — both the same typed result, never a 500).
    /// (409 <c>active-aggregate-exists</c>)
    /// </summary>
    ActiveAggregateExists,

    /// <summary>The aggregate is already closed. (409 <c>already-closed</c>)</summary>
    AlreadyClosed,

    /// <summary>The aggregate is not closed. (409 <c>not-closed</c>)</summary>
    NotClosed,

    /// <summary>A later close exists on another aggregate sharing the anchor. (409 <c>not-last-closed</c>)</summary>
    NotLastClosed,

    /// <summary>The aggregate is closed; no movement/opening-fact mutation is accepted without a reopen. (409 <c>aggregate-closed</c>)</summary>
    AggregateClosed,

    /// <summary>A second Início append is refused. (409 <c>only-one-inicio</c>)</summary>
    OnlyOneInicio,
}

/// <summary>
/// The closed Boquilhas result set (contract §12.1).
/// </summary>
/// <remarks>
/// No result type carries a lifecycle status beyond the aggregate status token, a document state, a
/// PDF/file fact, a permission decision or a navigation target.</remarks>
public abstract record BoquilhasResult
{
    private BoquilhasResult()
    {
    }

    /// <summary>The Registo lot grid (route 4).</summary>
    public sealed record ListFound(IReadOnlyList<BoquilhaListItemReadModel> Rows, int Total) : BoquilhasResult;

    /// <summary>The local Histórico list (route 18).</summary>
    public sealed record HistoryFound(IReadOnlyList<HistoryItemReadModel> Rows, int Total) : BoquilhasResult;

    /// <summary>The aggregate ficha (route 5).</summary>
    /// <remarks>The positional member is named <c>Value</c> rather than <c>Ficha</c> because a nested
    /// record type and a member of the same name cannot coexist in C# (the accepted P2-T04 reading
    /// for <c>JobOnResult.Ficha</c>).</remarks>
    public sealed record Ficha(BoquilhasFichaReadModel Value) : BoquilhasResult;

    /// <summary>The per-movement edit/audit trail (route 6).</summary>
    public sealed record MovementAuditFound(Guid MovementId, IReadOnlyList<MovementAuditItemReadModel> Entries) : BoquilhasResult;

    /// <summary>The aggregate was created: the real backend-allocated id and version 1.</summary>
    public sealed record Created(Guid BoquilhasId, int Version) : BoquilhasResult;

    /// <summary>The opening facts were updated; the aggregate version incremented exactly once.</summary>
    public sealed record OpeningFactsUpdated(Guid BoquilhasId, int Version) : BoquilhasResult;

    /// <summary>The movement was appended (the single quantity event).</summary>
    public sealed record MovementAppended(Guid MovementId, int Version, int AggregateVersion) : BoquilhasResult;

    /// <summary>The SAME movement row was edited (no second quantity event).</summary>
    public sealed record MovementEdited(Guid MovementId, int Version, int AggregateVersion) : BoquilhasResult;

    /// <summary>The SAME aggregate was closed; the immutable snapshot was written.</summary>
    public sealed record Closed(Guid BoquilhasId, int Version, DateTimeOffset ClosedAt) : BoquilhasResult;

    /// <summary>The SAME aggregate was reopened; the reopen record was written.</summary>
    public sealed record Reopened(Guid BoquilhasId, int Version, DateTimeOffset ReopenedAt) : BoquilhasResult;

    /// <summary>The reference → productions read of the opening flow (composed from <c>IJobOnService</c>).</summary>
    public sealed record ProductionsFound(IReadOnlyList<JobOn.JobOnProductionListItem> Productions) : BoquilhasResult;

    /// <summary>The Job On ficha read (composed from <c>IJobOnService</c>).</summary>
    /// <remarks>The positional member is named <c>Value</c> (same nested-type/member rule as
    /// <see cref="Ficha"/>).</remarks>
    public sealed record JobOnFichaFound(JobOn.JobOnFicha Value) : BoquilhasResult;

    /// <summary>The BQ context was created/updated by Job On's own code; the real <c>bq_id</c> is returned.</summary>
    public sealed record BqAssociated(Guid JobOnId, Guid BqId, int JobOnVersion) : BoquilhasResult;

    /// <summary>The six current machine → repairer assignments (consumed read, route 16).</summary>
    public sealed record AssignmentsFound(IReadOnlyList<MachineRepairerAssignmentReadModel> Assignments) : BoquilhasResult;

    /// <summary>The repairer register (consumed read, route 17).</summary>
    public sealed record RepairersFound(IReadOnlyList<RepairerReadModel> Repairers) : BoquilhasResult;

    /// <summary>The exact contracted validation codes; nothing was written.</summary>
    public sealed record ValidationFailed(IReadOnlyList<string> Errors) : BoquilhasResult;

    /// <summary>The requested aggregate or trail does not exist.</summary>
    public sealed record NotFound(Guid Id) : BoquilhasResult;

    /// <summary>A typed, actionable refusal; nothing was written.</summary>
    public sealed record Refused(BoquilhasRefusalReason Reason, string Message) : BoquilhasResult;
}