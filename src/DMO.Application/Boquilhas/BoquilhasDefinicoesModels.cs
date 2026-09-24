namespace DMO.Application.Boquilhas;

/// <summary>
/// The command carriers and closed result set of <c>Boquilhas > Definições</c> — the repairer
/// family ownership transferred by the Owner clarification (P2-T07 §34.3; P2-T05 §31.3): the
/// repairer register and the independent line/machine → repairer assignments belong to Boquilhas,
/// NOT to Controlo and NOT to Admin.
/// </summary>
/// <remarks>
/// Authority: P2-T07 OWNER CLARIFICATION §34.3 (ownership transfer; supersedes the affected
/// P2-T05/P2-T07 ownership wording). The shape rules of the closed model are UNCHANGED as shape:
/// name is the only required repairer data (no invented fields), no delete/deactivation path, one
/// INDEPENDENT assignment per machine (<c>B1</c>,<c>B2</c>,<c>B3</c>,<c>C1</c>,<c>C2</c>,<c>C3</c>)
/// — changing one machine never touches another — no "Linha B"/"Linha C" grouping, current-state
/// only, and historical preservation on movement records (changing the default repairer of a line
/// never rewrites past movements). The physical <c>repairers</c>/<c>machine_repairer_assignments</c>
/// tables stay where they are — this delta changes ownership/service/UI only, no schema migration
/// (Owner rule). The PDF/email/document settings stay under <c>Controlo_Create → Definições</c>.
/// </remarks>

// ------------------------------------------------------------------ commands

/// <summary>Adds a repairer: the only required business data is the name (§34.3 shape).</summary>
public sealed record CreateRepairerCommand(string Name);

/// <summary>Renames a repairer on the same <c>repairer_id</c>, version-guarded.</summary>
public sealed record RenameRepairerCommand(Guid RepairerId, int ExpectedVersion, string Name);

/// <summary>
/// Sets/changes/clears the independent assignment of ONE machine: a null <see cref="RepairerId"/>
/// clears the assignment (explicit operator action); a value upserts the row. The version is the
/// observed assignment version, or none on the first set. The other five machines are never
/// touched.
/// </summary>
public sealed record SetMachineAssignmentCommand(string Machine, Guid? RepairerId, int? ExpectedVersion);

// ------------------------------------------------------------------ results

/// <summary>The typed reason of a Definições refusal (transport token <c>stale-version</c>).</summary>
public enum BoquilhasDefinicoesRefusalReason
{
    /// <summary>The row changed after it was observed; nothing written.</summary>
    StaleVersion,
}

/// <summary>The closed Boquilhas Definições result set.</summary>
public abstract record BoquilhasDefinicoesResult
{
    private BoquilhasDefinicoesResult()
    {
    }

    /// <summary>The repairer register (name + version, deterministic order).</summary>
    public sealed record RepairersFound(IReadOnlyList<BoquilhasRepairerItem> Repairers) : BoquilhasDefinicoesResult;

    /// <summary>A repairer was added; the backend allocated <c>repairer_id</c>.</summary>
    public sealed record RepairerCreated(Guid RepairerId, int Version) : BoquilhasDefinicoesResult;

    /// <summary>The same <c>repairer_id</c> was renamed; the version incremented.</summary>
    public sealed record RepairerRenamed(Guid RepairerId, int Version) : BoquilhasDefinicoesResult;

    /// <summary>Every current machine assignment (absent row = no repairer assigned).</summary>
    public sealed record AssignmentsFound(IReadOnlyList<BoquilhasMachineAssignmentItem> Assignments) : BoquilhasDefinicoesResult;

    /// <summary>The machine's independent assignment was set/changed; version incremented.</summary>
    public sealed record AssignmentSet(string Machine, int Version) : BoquilhasDefinicoesResult;

    /// <summary>The machine's assignment was cleared (row removed; other machines untouched).</summary>
    public sealed record AssignmentCleared(string Machine) : BoquilhasDefinicoesResult;

    /// <summary>The exact contracted validation codes; nothing was written.</summary>
    public sealed record ValidationFailed(IReadOnlyList<string> Errors) : BoquilhasDefinicoesResult;

    /// <summary>The requested settings record does not exist.</summary>
    public sealed record NotFound(Guid Id) : BoquilhasDefinicoesResult;

    /// <summary>A typed, actionable refusal; nothing was written.</summary>
    public sealed record Refused(BoquilhasDefinicoesRefusalReason Reason, string Message) : BoquilhasDefinicoesResult;
}

/// <summary>One repairer-register item of the Definições surface.</summary>
public sealed record BoquilhasRepairerItem(Guid RepairerId, string Name, int Version);

/// <summary>One machine's current independent assignment.</summary>
public sealed record BoquilhasMachineAssignmentItem(string Machine, Guid? RepairerId, int Version);