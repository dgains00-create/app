using DMO.Application.Boquilhas;
using DMO.Application.Session;
using DMO.Domain.Tools;
using DMO.Web.Frontend.Shared.Contracts;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.Boquilhas;

/// <summary>
/// Novo surface (P2-T07 contract §25.2 regions N1–N4): the aggregate opening flow — production-linked
/// or standalone — with the shared Tool orchestration, the Job On reads, the BQ-context presence and
/// the opening facts (machines, Início quantity, opening date, manual % utilização, observations).
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §16 (shared Tool search/select/create orchestration: BQ candidates
/// only, never auto-selected, contextual create returns the canonical <c>tool_id</c> and restores
/// the SAME origin state), §22 (production-linked | standalone flows; no fake Job On / no fake
/// <c>bq_id</c>) and §25.2 (regions N1–N4). The page is server-gated <c>boquilhas</c>. The BQ
/// context creation composes <c>IJobOnService</c> (route 15) — the <c>bq_contexts</c> row is
/// created by Job On's own code; Tool search/create happens on the <c>ferramentas</c>-gated P2-T04
/// routes through the shared picker (no second Tool picker/registry, AC-T4).</remarks>
[Authorize(Policy = BoquilhasPolicyNames.Boquilhas)]
public sealed class NovoModel : PageModel
{
    private const string ShellTitle = "Boquilhas — Novo";
    private const string ShellContext = "Abertura de registo de reparação externa BQ";

    private readonly IBoquilhasService _service;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;
    private readonly ILogger<NovoModel> _logger;

    /// <summary>Creates the page over the Boquilhas service, the current account and the shell.</summary>
    public NovoModel(
        IBoquilhasService service,
        ICurrentAccountContext currentAccount,
        ShellPresentationService shell,
        ILogger<NovoModel> logger)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(currentAccount);
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(logger);
        _service = service;
        _currentAccount = currentAccount;
        _shell = shell;
        _logger = logger;
    }

    /// <summary>Prefill only: the Job On occurrence to open (production-linked flow).</summary>
    [BindProperty(SupportsGet = true)]
    public Guid? JobOnId { get; set; }

    /// <summary>The consumed repairer register for the opening surface (route 17 shape).</summary>
    public IReadOnlyList<RepairerReadModel> Repairers { get; private set; } = [];

    // ---- N3 — the shared Tool picker of the opening (BQ candidates only; never auto-selected) --

    /// <summary>The shared picker presentation of the BQ Tool selection (explicit selection only).</summary>
    public ToolPickerPresentation? ToolPicker { get; private set; }

    /// <summary>The page-owned opaque candidate-key → canonical tool_id map (never parsed as identity).</summary>
    public IReadOnlyList<ToolCandidateEntry> ToolCandidateMap { get; private set; } = [];

    /// <summary>The six settled independent machines (opening-facts pre-fill assistance).</summary>
    public static IReadOnlyList<MachineCode> Machines => MachineCode.All;

    /// <inheritdoc />
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);
        ViewData["DmoShell"] = await _shell.BuildAsync(current, ShellTitle, ShellContext, cancellationToken);

        var repairersResult = await _service.GetRepairersAsync(cancellationToken);
        if (repairersResult is BoquilhasResult.RepairersFound(var repairers))
        {
            Repairers = repairers;
        }

        // The picker starts ready-and-empty: no candidate is ever auto-selected (AC-T2); the
        // consume-enabled create affordance posts the P2-T04 ferramentas route (shared
        // orchestration) and the subflow returns to THIS origin state (P2-T03 opaque origin token).
        ToolPicker = ToolPickerPresentation.Create(
            CommonState.Ready,
            "Ferramenta BQ",
            query: string.Empty,
            candidates: [],
            selectedCandidateKey: null,
            originToken: "boquilhas-novo",
            originContext:
            [
                ToolPickerFactPresentation.Create("Origem", "Novo registo de Boquilhas"),
            ],
            expectedTypeLabel: "BQ",
            createAction: SharedActionPresentation.CreateEnabled(
                "boquilhas-create-tool", "Criar ferramenta", "A criar…"),
            cancelAction: SharedActionPresentation.CreateEnabled(
                ToolPickerPresentation.DefaultCancelActionKey, "Cancelar"),
            retryAction: SharedActionPresentation.CreateEnabled(
                "boquilhas-retry", "Tentar novamente"),
            resultSummary: null);
    }

    /// <summary>One page-owned opaque picker candidate entry (key → canonical <c>tool_id</c>).</summary>
    public sealed record ToolCandidateEntry(string Key, Guid ToolId);

    /// <summary>One page-owned "open production" route entry (selection → exact Job On).</summary>
    public sealed record ProductionOpenEntry(string Key, string Href);
}