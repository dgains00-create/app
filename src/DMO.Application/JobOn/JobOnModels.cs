using DMO.Domain.Tools;

namespace DMO.Application.JobOn;

/// <summary>
/// The reference → productions query input carrier.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §8.1. The reference is required and trimmed; a blank reference is a
/// validation failure (<c>REFERENCE_REQUIRED</c>), never an empty result.
/// </remarks>
public sealed record FindProductionsQuery(string Reference);

/// <summary>
/// One matching Job On production occurrence: identity plus human production facts only.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §8.1 "Item shape" and the "Deliberately not in the item" row: no
/// context id, no context presence flag, no Tool fact, no Controlo/Boquilhas state and no document
/// availability is copied into this carrier.
/// </remarks>
public sealed record JobOnProductionListItem(
    Guid JobOnId,
    string Reference,
    string ProductionNumber,
    string Machine,
    DateOnly? ProductionDate);

/// <summary>
/// The live Tool facts of a Tool referenced by a Job On context, composed at read time.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §8.4 and Q21. It is a projection: it is never persisted into
/// <c>job_ons</c> or a context row, and it is not Tool access. The frozen triple of the context is
/// always presented separately and is never refreshed from this projection.
/// </remarks>
public sealed record ToolSummaryProjection(
    Guid ToolId,
    ToolType Type,
    string Reference,
    string Lot,
    Processo? Processo,
    int? Quantity,
    IReadOnlyList<MachineCode> CompatibleMachines);

/// <summary>
/// One context of a Job On ficha: the context identity, the direct canonical Tool relation, the
/// frozen triple and the live Tool projection.
/// </summary>
public sealed record ToolContextFicha(
    ToolContextType ContextType,
    Guid ContextId,
    Guid ToolId,
    ToolType ToolType,
    string ToolReference,
    string ToolLot,
    ToolSummaryProjection Tool);

/// <summary>
/// The Job On read model (ficha): the occurrence plus its existing contexts.
/// </summary>
/// <remarks>
/// A read never mutates: no write, no version bump and no implicit completion of the context set. A
/// Job On with zero contexts is a legitimate ficha.
/// </remarks>
public sealed record JobOnFicha(
    Guid JobOnId,
    string Reference,
    string ProductionNumber,
    string Machine,
    DateOnly? ProductionDate,
    Guid? CopiedFromJobOnId,
    int Version,
    IReadOnlyList<ToolContextFicha> Contexts);

/// <summary>
/// The explicit Tool-association action of one context slot.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §11.2. <see cref="Keep"/> and <see cref="Remove"/> are never conflated
/// with "no value supplied": the change list is the only association mechanism, and the nullable Tool
/// ids on the create command are the only place a bare id expresses an association.
/// </remarks>
public enum ToolAssociationAction
{
    /// <summary>Do nothing for this slot.</summary>
    Keep,

    /// <summary>Select this Tool for the slot: insert the context, or update it in place.</summary>
    Set,

    /// <summary>Remove the Tool association from the slot (delete the context row).</summary>
    Remove,
}

/// <summary>One explicit Tool-association change of an edit command.</summary>
/// <remarks>
/// <see cref="ToolId"/> is required for <see cref="ToolAssociationAction.Set"/> and must be absent
/// for <see cref="ToolAssociationAction.Keep"/>/<see cref="ToolAssociationAction.Remove"/>.
/// </remarks>
public sealed record ToolAssociationChange(
    ToolContextType ContextType,
    ToolAssociationAction Action,
    Guid? ToolId);

/// <summary>
/// The Job On create command: the simplified Beta facts plus the optional CM/MF/BQ Tool slots.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §11.1. <c>processo</c> is not part of the command: it is consumed
/// through the CM context's Tool and is never a Job On fact.
/// </remarks>
public sealed record CreateJobOnCommand(
    string Reference,
    string ProductionNumber,
    string Machine,
    DateOnly? ProductionDate,
    Guid? CmToolId,
    Guid? MfToolId,
    Guid? BqToolId);

/// <summary>
/// The Job On edit command: the four editable facts plus the explicit Tool-association changes.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §11.2 as read through the Architect plan review §23 item N1: the
/// explicit <see cref="Associations"/> list is the association mechanism. No nullable Tool id is
/// carried here, because a nullable id has no independent association meaning and "leave as is"
/// must never be conflated with "remove" — a dead parameter that silently did nothing would be a
/// second, weaker association path.
/// <para>
/// Not editable: the frozen triple as a field, <c>jobon_id</c>, <c>copied_from_jobon_id</c>,
/// <c>created_at</c> and <c>version</c>.
/// </para>
/// </remarks>
public sealed record UpdateJobOnCommand(
    Guid JobOnId,
    int ExpectedVersion,
    string Reference,
    string ProductionNumber,
    string Machine,
    DateOnly? ProductionDate,
    IReadOnlyList<ToolAssociationChange> Associations,
    bool DateThresholdWarningAcknowledged);

/// <summary>
/// The Job On duplication command: an explicit source plus the new occurrence's own facts.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §10.1. The reference is not supplied: a duplicate is another
/// production <b>of the same reference</b>. The source is always explicit and is never inferred,
/// never "the latest" and never "the previous".
/// </remarks>
public sealed record DuplicateJobOnCommand(
    Guid SourceJobOnId,
    int ExpectedSourceVersion,
    string ProductionNumber,
    string Machine,
    DateOnly? ProductionDate);

/// <summary>
/// The Job On delete command: explicit confirmation plus the stronger date-threshold
/// acknowledgement.
/// </summary>
public sealed record DeleteJobOnCommand(
    Guid JobOnId,
    int ExpectedVersion,
    bool DeleteConfirmed,
    bool DateThresholdWarningAcknowledged);

/// <summary>The typed reason of a Job On refusal (the exact transport tokens are in §14.2).</summary>
public enum JobOnRefusalReason
{
    /// <summary>The row changed after the operator observed it; nothing written.</summary>
    StaleVersion,

    /// <summary>Another Job On already owns this (reference, production number) pair.</summary>
    DuplicateProduction,

    /// <summary>Dependent operational facts exist; nothing deleted.</summary>
    DependencyExists,

    /// <summary>The production date is already reached/passed and needs explicit acknowledgement.</summary>
    DateThresholdConfirmationRequired,
}

/// <summary>
/// One pending-association candidate: a real <c>cm_contexts</c> row resolving to the anchor Tool.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §20.4.2 (the accepted Q-CAND additive read) and §4.3. Candidates are
/// real context rows, never synthesized (PID9/AC-P6); association is always human-confirmed, never
/// automatic and never inferred (CROSS_MODULE_FLOWS anti-inference rules).
/// </remarks>
public sealed record PesoAssociationCandidate(
    Guid CmContextId,
    Guid JobOnId,
    string Reference,
    string ProductionNumber,
    string Machine);

/// <summary>
/// The closed Job On result set.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §14.1. No result type carries a lifecycle status, an availability
/// state, a document state, a Controlo or Boquilhas fact, a permission decision or a navigation
/// target.
/// </remarks>
public abstract record JobOnResult
{
    private JobOnResult()
    {
    }

    /// <summary>Every matching occurrence for the reference, including older ones.</summary>
    public sealed record ProductionsFound(IReadOnlyList<JobOnProductionListItem> Productions) : JobOnResult;

    /// <summary>The Job On ficha.</summary>
    /// <remarks>
    /// The positional member is named <c>Value</c> rather than <c>Ficha</c> because a nested record
    /// type and a member of the same name cannot coexist in C# (CS0542); the type identity
    /// <c>JobOnResult.Ficha</c> and the positional pattern <c>is JobOnResult.Ficha(var ficha)</c> are
    /// unchanged.
    /// </remarks>
    public sealed record Ficha(JobOnFicha Value) : JobOnResult;

    /// <summary>The read-only duplication preview of an explicitly chosen source.</summary>
    public sealed record DuplicationPreview(Guid SourceJobOnId, int SourceVersion, JobOnFicha Source) : JobOnResult;

    /// <summary>The real backend-allocated production-occurrence identity.</summary>
    public sealed record Created(Guid JobOnId, int Version) : JobOnResult;

    /// <summary>The occurrence was updated; the version incremented exactly once.</summary>
    public sealed record Updated(Guid JobOnId, int Version) : JobOnResult;

    /// <summary>
    /// The real pending-association candidates resolving to the supplied Tool (P2-T05 §20.4.2
    /// additive read, Q-CAND).
    /// </summary>
    public sealed record AssociationCandidates(IReadOnlyList<PesoAssociationCandidate> Candidates) : JobOnResult;

    /// <summary>The duplicated occurrence and its explicit source.</summary>
    public sealed record Duplicated(Guid JobOnId, Guid SourceJobOnId, int Version) : JobOnResult;

    /// <summary>The occurrence and its own contexts were deleted.</summary>
    public sealed record Deleted(Guid JobOnId) : JobOnResult;

    /// <summary>The exact contracted validation codes; nothing was written.</summary>
    public sealed record ValidationFailed(IReadOnlyList<string> Errors) : JobOnResult;

    /// <summary>The requested occurrence or Tool does not exist.</summary>
    public sealed record NotFound(Guid JobOnId) : JobOnResult;

    /// <summary>A typed, actionable refusal; nothing was written.</summary>
    public sealed record Refused(
        JobOnRefusalReason Reason,
        string Message,
        Guid? ExistingJobOnId = null,
        IReadOnlyList<JobOnDependency>? Dependencies = null) : JobOnResult;
}
