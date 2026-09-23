using DMO.Domain.Tools;

namespace DMO.Domain.JobOn;

/// <summary>
/// One Job On production occurrence with its optional production contexts.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §2.4, §3.3 and §6.
/// <para>
/// Captured facts are exactly <c>reference</c>, <c>production_number</c>, <c>machine</c> and the
/// optional <c>production_date</c> — plus the system facts <c>jobon_id</c>,
/// <c>copied_from_jobon_id</c>, <c>version</c> and the timestamps. The optional production contexts
/// are separate rows, never columns on the Job On.
/// </para>
/// <para>
/// Deliberately absent: <c>processo</c> (Tool-owned, reached through the CM context), any
/// status/state field (<c>rascunho</c>, <c>planeado</c>, <c>em fabrico</c>, <c>fechado</c>,
/// <c>cancelado</c>, <c>active</c>, <c>locked</c>, <c>approved</c>), <c>production_id</c>,
/// <c>job_on_revision_id</c>, quantity snapshots, Pegamentos facts and every reverse-ID array.
/// Conversely, there is <b>no</b> reverse collection from a Tool to its Job Ons.
/// </para>
/// <para>
/// <see cref="CopiedFromJobOnId"/> is recorded once at duplication as structural lineage and is
/// never rewritten; it never makes the new occurrence a revision of the source.
/// </para>
/// </remarks>
public sealed record JobOn(
    JobOnId JobOnId,
    string Reference,
    string ProductionNumber,
    MachineCode Machine,
    DateOnly? ProductionDate,
    Guid? CopiedFromJobOnId,
    int Version,
    IReadOnlyList<ToolContext> Contexts);
