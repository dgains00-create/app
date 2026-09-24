using DMO.Application.Boquilhas;
using DMO.Application.Session;
using DMO.Web.Frontend.Shared.Contracts;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.Boquilhas;

/// <summary>
/// Novo surface (P2-T07 OWNER CLARIFICATION): associate the register with the REAL production —
/// reference search → explicit production selection → Job On ficha → BQ-context presence →
/// create the REGISTER IDENTITY (no quantity movement is manufactured).
/// </summary>
/// <remarks>
/// Authority: P2-T07 OWNER CLARIFICATION. There is NO two-flow choice (production-linked |
/// standalone): every register belongs to a REAL Job On/BQ context, and there are no opening facts
/// (no initial quantity, no utilisation, no machine set) — the register creation establishes the
/// identity only. The BQ context creation composes <c>IJobOnService</c> (route 9); the shared Tool
/// orchestration stays on the <c>ferramentas</c>-gated P2-T04 routes through the shared picker.
/// The page is server-gated <c>boquilhas</c>.</remarks>
[Authorize(Policy = BoquilhasPolicyNames.Boquilhas)]
public sealed class NovoModel : PageModel
{
    private const string ShellTitle = "Boquilhas — Novo";
    private const string ShellContext = "Abrir o registo de movimentos de uma produção BQ";

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

    /// <summary>Prefill only: the Job On occurrence to open.</summary>
    [BindProperty(SupportsGet = true)]
    public Guid? JobOnId { get; set; }

    /// <summary>
    /// The shared picker presentation of the BQ Tool selection for the association subflow
    /// (explicit selection only; the picker never auto-selects).
    /// </summary>
    public ToolPickerPresentation? ToolPicker { get; private set; }

    /// <inheritdoc />
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);
        ViewData["DmoShell"] = await _shell.BuildAsync(current, ShellTitle, ShellContext, cancellationToken);

        // The picker starts ready-and-empty: no candidate is ever auto-selected; the create
        // affordance posts the P2-T04 ferramentas route (shared orchestration).
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
            resultSummary: null);
    }
}