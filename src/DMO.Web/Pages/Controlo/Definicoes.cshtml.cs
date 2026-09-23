using DMO.Application.ControloCreate;
using DMO.Application.Session;
using DMO.Domain.Controlo;
using DMO.Domain.Tools;
using DMO.Web.Authorization;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.Controlo;

/// <summary>
/// Definições surface: the operational configuration sections owned by Controlo_Create
/// (P2-T05 contract §9, §8.7; post-closure glass-density correction contract §5.3 adds the
/// current operational glass densities per processo).
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §9 (ownership), §10 (repairers), §11 (machine assignments),
/// §12 (PDF directory), §13 (email lists), §14 (email templates); post-closure glass-density
/// correction contract §5.1–§5.3 (glass densities).
/// <para>
/// Definições is a <b>surface inside the Controlo Create working area</b> — not a destination, not
/// a Module, not registered anywhere (§9.1, AC-Y1). It is gated by exactly the same
/// <c>controlo-create</c> policy as every other P2-T05 route (§21.2/§22); an approve-only caller is
/// denied server-side (§21.5, AUT2). Every fact is a real backend read; the page-owned adapter
/// (<c>dmo-controlo.js</c>) executes the accepted settings routes. NNPB and PS are fixed canonical
/// process entries — no per-Tool selection, no process creation/deletion.</para>
/// </remarks>
[Authorize(Policy = ControloPolicyNames.ControloCreate)]
public sealed class DefinicoesModel : PageModel
{
    private const string ShellTitle = "Definições";
    private const string ShellContext = "Definições operacionais do Controlo";

    private readonly IControloDefinicoesService _settings;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;
    private readonly ILogger<DefinicoesModel> _logger;

    /// <summary>Creates the page over the settings service and the shared shell.</summary>
    public DefinicoesModel(
        IControloDefinicoesService settings,
        ICurrentAccountContext currentAccount,
        ShellPresentationService shell,
        ILogger<DefinicoesModel> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(currentAccount);
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(logger);
        _settings = settings;
        _currentAccount = currentAccount;
        _shell = shell;
        _logger = logger;
    }

    /// <summary>Section 1 — the repairer register (name is the only business data).</summary>
    public IReadOnlyList<Repairer> Repairers { get; private set; } = [];

    /// <summary>Section 2 — the independent machine assignments of B1…C3.</summary>
    public IReadOnlyList<MachineAssignmentView> MachineAssignments { get; private set; } = [];

    /// <summary>Section 3 — the configured PDF base directory (null = explicit not-configured).</summary>
    public PdfDirectoryView? PdfDirectory { get; private set; }

    /// <summary>Section 4 — the named email lists.</summary>
    public IReadOnlyList<EmailListListItem> EmailLists { get; private set; } = [];

    /// <summary>Section 5 — the email templates.</summary>
    public IReadOnlyList<EmailTemplate> EmailTemplates { get; private set; } = [];

    /// <summary>Section 6 — the current operational glass densities (NNPB and PS, g/cm³).</summary>
    public IReadOnlyList<GlassDensitySetting> GlassDensities { get; private set; } = [];

    /// <summary>Whether any settings read failed (a section is never silently empty).</summary>
    public bool LookupFailed { get; private set; }

    /// <inheritdoc />
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);
        ViewData["DmoShell"] = await _shell.BuildAsync(current, ShellTitle, ShellContext, cancellationToken);

        try
        {
            if (await _settings.ListRepairersAsync(cancellationToken) is SettingsResult.RepairersFound(var repairers))
            {
                Repairers = repairers;
            }
            else
            {
                LookupFailed = true;
            }

            if (await _settings.ListMachineAssignmentsAsync(cancellationToken) is SettingsResult.AssignmentsFound(var assignments))
            {
                MachineAssignments = MachineCode.All
                    .Select(machine =>
                    {
                        var assignment = assignments.FirstOrDefault(candidate => candidate.Machine.Value == machine.Value);
                        return new MachineAssignmentView(machine.Value, assignment?.RepairerId.Value, assignment?.Version ?? 1);
                    })
                    .ToList();
            }
            else
            {
                LookupFailed = true;
            }

            if (await _settings.GetPdfDirectoryAsync(cancellationToken) is SettingsResult.PdfDirectoryFound(var view))
            {
                PdfDirectory = view;
            }
            else
            {
                LookupFailed = true;
            }

            if (await _settings.ListEmailListsAsync(cancellationToken) is SettingsResult.EmailListsFound(var lists))
            {
                EmailLists = lists;
            }
            else
            {
                LookupFailed = true;
            }

            if (await _settings.ListEmailTemplatesAsync(cancellationToken) is SettingsResult.EmailTemplatesFound(var templates))
            {
                EmailTemplates = templates;
            }
            else
            {
                LookupFailed = true;
            }

            if (await _settings.ListGlassDensitiesAsync(cancellationToken) is SettingsResult.GlassDensitiesFound(var densities))
            {
                GlassDensities = densities;
            }
            else
            {
                LookupFailed = true;
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Definições read failed.");
            LookupFailed = true;
        }
    }
}

/// <summary>One machine's independent assignment presentation.</summary>
public sealed record MachineAssignmentView(string Machine, Guid? RepairerId, int Version);