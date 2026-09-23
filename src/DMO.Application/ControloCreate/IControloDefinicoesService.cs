namespace DMO.Application.ControloCreate;

/// <summary>
/// The single <c>Controlo_Create → Definições</c> settings service contract.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §9–§14 and §20.3.
/// <para>
/// All five areas are site-wide database configuration owned by Controlo_Create (Q-SITE); no
/// per-user dimension exists. No setting value ever becomes a canonical identity, a join key, a
/// document identity or production truth (§9.1). The PDF-directory check (Q-PDF) executes on the
/// <b>server host</b> through the filesystem probe abstraction (SET12) and returns only the typed
/// §12.2 vocabulary.</para>
/// </remarks>
public interface IControloDefinicoesService
{
    // Repairers (§10) — name is the only required data.
    /// <summary>Lists the repairer register (deterministic name order).</summary>
    Task<SettingsResult> ListRepairersAsync(CancellationToken cancellationToken);

    /// <summary>Adds a repairer (name only).</summary>
    Task<SettingsResult> CreateRepairerAsync(CreateRepairerCommand command, CancellationToken cancellationToken);

    /// <summary>Renames a repairer on the same <c>repairer_id</c>, version-guarded.</summary>
    Task<SettingsResult> RenameRepairerAsync(RenameRepairerCommand command, CancellationToken cancellationToken);

    // Machine assignments (§11) — six independent machines, no grouping.
    /// <summary>Lists the current assignment of every machine (absent row = none).</summary>
    Task<SettingsResult> ListMachineAssignmentsAsync(CancellationToken cancellationToken);

    /// <summary>Sets/changes/clears ONE machine's independent assignment.</summary>
    Task<SettingsResult> SetMachineAssignmentAsync(SetMachineAssignmentCommand command, CancellationToken cancellationToken);

    /// <summary>Clears ONE machine's assignment (other machines untouched).</summary>
    Task<SettingsResult> ClearMachineAssignmentAsync(ClearMachineAssignmentCommand command, CancellationToken cancellationToken);

    // PDF directory (§12) — server-host base-directory configuration.
    /// <summary>Reads the configured base directory, or the explicit <c>not-configured</c> state.</summary>
    Task<SettingsResult> GetPdfDirectoryAsync(CancellationToken cancellationToken);

    /// <summary>Configures/changes the base directory (single-row upsert, version-checked).</summary>
    Task<SettingsResult> SetPdfDirectoryAsync(SetPdfDirectoryCommand command, CancellationToken cancellationToken);

    /// <summary>Runs the server-side accessibility check of the configured path (typed result).</summary>
    Task<SettingsResult> CheckPdfDirectoryAsync(CancellationToken cancellationToken);

    // Email lists (§13) — named lists with complete recipient sets.
    /// <summary>Lists the named lists (name + recipient count + version).</summary>
    Task<SettingsResult> ListEmailListsAsync(CancellationToken cancellationToken);

    /// <summary>Reads one named list with its complete recipient set.</summary>
    Task<SettingsResult> GetEmailListAsync(Guid emailListId, CancellationToken cancellationToken);

    /// <summary>Creates a named list with its complete initial recipient set.</summary>
    Task<SettingsResult> CreateEmailListAsync(CreateEmailListCommand command, CancellationToken cancellationToken);

    /// <summary>Replaces the list's name and its complete recipient set atomically.</summary>
    Task<SettingsResult> UpdateEmailListAsync(UpdateEmailListCommand command, CancellationToken cancellationToken);

    /// <summary>Deletes a named list after explicit confirmation.</summary>
    Task<SettingsResult> DeleteEmailListAsync(DeleteEmailListCommand command, CancellationToken cancellationToken);

    // Email templates (§14) — name/subject/body/document type, text verbatim.
    /// <summary>Lists the email templates.</summary>
    Task<SettingsResult> ListEmailTemplatesAsync(CancellationToken cancellationToken);

    /// <summary>Reads one email template.</summary>
    Task<SettingsResult> GetEmailTemplateAsync(Guid emailTemplateId, CancellationToken cancellationToken);

    /// <summary>Creates an email template.</summary>
    Task<SettingsResult> CreateEmailTemplateAsync(CreateEmailTemplateCommand command, CancellationToken cancellationToken);

    /// <summary>Updates an email template, version-guarded.</summary>
    Task<SettingsResult> UpdateEmailTemplateAsync(UpdateEmailTemplateCommand command, CancellationToken cancellationToken);

    /// <summary>Deletes an email template after explicit confirmation.</summary>
    Task<SettingsResult> DeleteEmailTemplateAsync(DeleteEmailTemplateCommand command, CancellationToken cancellationToken);
}