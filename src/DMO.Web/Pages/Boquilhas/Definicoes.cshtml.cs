using DMO.Application.Boquilhas;
using DMO.Application.Session;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.Boquilhas;

/// <summary>
/// <c>Boquilhas > Definições</c> surface (Owner clarification P2-T07 §34.3 / P2-T05 §31.3): the
/// repairer register and the independent line/machine → repairer assignments — the repairer family
/// owned by Boquilhas, NOT by Controlo and NOT by Admin.
/// </summary>
/// <remarks>
/// Authority: P2-T07 OWNER CLARIFICATION §34.3 (ownership transfer; supersedes the affected
/// P2-T05/P2-T07 ownership wording). The physical <c>repairers</c>/<c>machine_repairer_assignments</c>
/// tables stay where they are — ownership/service/UI only, no schema migration (Owner rule). Shape
/// rules unchanged as shape: name is the only required repairer data (no delete path), ONE
/// independent assignment per machine (B1/B2/B3/C1/C2/C3; altering one machine never touches
/// another), no grouping, current-state only. Changing a default repairer NEVER rewrites historical
/// movements — the movement row keeps the repairer used at the time.
/// <para>
/// The page is server-gated <c>boquilhas</c>; mutations are executed by the page-owned adapter
/// against the <c>/boquilhas/definicoes</c> minimal-API routes; backend validation is authoritative
/// and a stale version enters the accepted conflict presentation with the explicit reload recovery
/// (D2). The PDF/email/document settings are NOT here — they remain in Controlo.</para>
/// </remarks>
[Authorize(Policy = BoquilhasPolicyNames.Boquilhas)]
public sealed class DefinicoesModel : PageModel
{
    private const string ShellTitle = "Boquilhas — Definições";
    private const string ShellContext = "Reparadores e associações máquina/linha → reparador";

    private readonly IBoquilhasDefinicoesService _settings;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;
    private readonly ILogger<DefinicoesModel> _logger;

    /// <summary>Creates the page over the Boquilhas Definições service and the shared shell.</summary>
    public DefinicoesModel(
        IBoquilhasDefinicoesService settings,
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
    public IReadOnlyList<RepairerView> Repairers { get; private set; } = [];

    /// <summary>Section 2 — the independent machine assignments of B1…C3.</summary>
    public IReadOnlyList<MachineAssignmentView> MachineAssignments { get; private set; } = [];

    /// <summary>Whether any settings read failed (a section is never silently empty).</summary>
    public bool LookupFailed { get; private set; }

    /// <inheritdoc />
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);
        ViewData["DmoShell"] = await _shell.BuildAsync(current, ShellTitle, ShellContext, cancellationToken);

        try
        {
            if (await _settings.ListRepairersAsync(cancellationToken) is BoquilhasDefinicoesResult.RepairersFound(var repairers))
            {
                Repairers = repairers
                    .Select(repairer => new RepairerView(
                        repairer.RepairerId,
                        repairer.Name,
                        repairer.Version))
                    .ToList();
            }
            else
            {
                LookupFailed = true;
            }

            if (await _settings.ListMachineAssignmentsAsync(cancellationToken)
                is BoquilhasDefinicoesResult.AssignmentsFound(var assignments))
            {
                MachineAssignments = assignments
                    .Select(assignment => new MachineAssignmentView(
                        assignment.Machine,
                        assignment.RepairerId,
                        assignment.Version))
                    .ToList();
            }
            else
            {
                LookupFailed = true;
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Boquilhas Definições read failed.");
            LookupFailed = true;
        }
    }
}

/// <summary>One repairer-register presentation row (light packet: id, name, version).</summary>
public sealed record RepairerView(Guid RepairerId, string Name, int Version);

/// <summary>One machine's independent assignment presentation.</summary>
public sealed record MachineAssignmentView(string Machine, Guid? RepairerId, int Version);