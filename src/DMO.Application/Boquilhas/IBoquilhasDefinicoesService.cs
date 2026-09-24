namespace DMO.Application.Boquilhas;

/// <summary>
/// The <c>Boquilhas > Definições</c> service contract: the repairer register and the independent
/// line/machine → repairer assignments — the repairer family owned by Boquilhas (Owner
/// clarification P2-T07 §34.3; P2-T05 §31.3; supersedes the affected P2-T05/P2-T07 ownership
/// wording). NOT owned by Controlo anymore, NOT owned by Admin.
/// </summary>
/// <remarks>
/// The physical <c>repairers</c>/<c>machine_repairer_assignments</c> tables stay exactly where they
/// are — this delta changes ownership/service/UI only, no schema migration (Owner rule). The PDF/
/// email/document settings are NOT part of this surface; they remain under
/// <c>Controlo_Create → Definições</c>. Every write is version-guarded; no delete path exists.</remarks>
public interface IBoquilhasDefinicoesService
{
    /// <summary>The repairer register (name + version, deterministic order).</summary>
    Task<BoquilhasDefinicoesResult> ListRepairersAsync(CancellationToken cancellationToken);

    /// <summary>Adds a repairer: name is the only required business data.</summary>
    Task<BoquilhasDefinicoesResult> CreateRepairerAsync(
        CreateRepairerCommand command,
        CancellationToken cancellationToken);

    /// <summary>Renames a repairer on the same <c>repairer_id</c>, version-guarded.</summary>
    Task<BoquilhasDefinicoesResult> RenameRepairerAsync(
        RenameRepairerCommand command,
        CancellationToken cancellationToken);

    /// <summary>Every current machine assignment (all six machines; absent row = no repairer).</summary>
    Task<BoquilhasDefinicoesResult> ListMachineAssignmentsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sets/changes/clears the independent assignment of ONE machine — the other five are never
    /// touched. A null repairer id is the explicit clear.
    /// </summary>
    Task<BoquilhasDefinicoesResult> SetMachineAssignmentAsync(
        SetMachineAssignmentCommand command,
        CancellationToken cancellationToken);
}