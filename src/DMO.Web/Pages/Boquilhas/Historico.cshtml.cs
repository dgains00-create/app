using DMO.Application.Boquilhas;
using DMO.Application.Session;
using DMO.Domain.Boquilhas;
using DMO.Web.Frontend.Shared.Contracts;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.Boquilhas;

/// <summary>
/// Histórico surface (P2-T07 contract §25.3 regions H1–H3): the LOCAL Boquilhas Histórico — filters
/// applied by the backend, the history table with the accepted selection/open semantics and the
/// exact-aggregate detail with outside-table actions per state.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §24 (HISTÓRICO (local) inside the module — never HISTÓRICO GLOBAL;
/// every filter a backend SQL predicate; paging 1-based deterministic; total backend-counted) and
/// §25.3 (regions H1–H3). The page is server-gated <c>boquilhas</c>. Double click (or the explicit
/// open control) opens the exact aggregate in the Registo detail; single click selects; actions
/// (Abrir/Reabrir) live OUTSIDE the table (AC-H3).</remarks>
[Authorize(Policy = BoquilhasPolicyNames.Boquilhas)]
public sealed class HistoricoModel : PageModel
{
    private const string ShellTitle = "Boquilhas — Histórico";
    private const string ShellContext = "Histórico local de registos de reparação externa BQ";

    private readonly IBoquilhasService _service;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;
    private readonly ILogger<HistoricoModel> _logger;

    /// <summary>Creates the page over the Boquilhas service, the current account and the shell.</summary>
    public HistoricoModel(
        IBoquilhasService service,
        ICurrentAccountContext currentAccount,
        ShellPresentationService shell,
        ILogger<HistoricoModel> logger)
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

    // ---- H1 — filters (the exact §24.2 set; backend-applied) ----------------------------------

    /// <summary>Aggregate/file-state filter (both when null).</summary>
    [BindProperty(SupportsGet = true)]
    public string? State { get; set; }

    /// <summary>Reference filter (traversal).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Reference { get; set; }

    /// <summary>Lot filter (traversal).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Lot { get; set; }

    /// <summary>Machine filter (aggregate machine-set membership).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Machine { get; set; }

    /// <summary>Movement business-date period start (EXISTS).</summary>
    [BindProperty(SupportsGet = true)]
    public DateOnly? BusinessDateFrom { get; set; }

    /// <summary>Movement business-date period end (EXISTS).</summary>
    [BindProperty(SupportsGet = true)]
    public DateOnly? BusinessDateTo { get; set; }

    /// <summary>Movement-type filter (EXISTS).</summary>
    [BindProperty(SupportsGet = true)]
    public string? MovementType { get; set; }

    /// <summary>Repairer filter (EXISTS over movement repairer).</summary>
    [BindProperty(SupportsGet = true)]
    public Guid? RepairerId { get; set; }

    /// <summary>1-based page.</summary>
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    /// <summary>Page size (1–100).</summary>
    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 50;

    /// <summary>Open the exact aggregate (detail region H3).</summary>
    [BindProperty(SupportsGet = true)]
    public Guid? BoquilhasId { get; set; }

    // ---- presentation state ------------------------------------------------------------------

    /// <summary>The exact contracted validation codes of a refused query.</summary>
    public IReadOnlyList<string> ValidationErrors { get; private set; } = [];

    /// <summary>H2 — the history table, or <c>null</c> on a refused query.</summary>
    public DenseTablePresentation? History { get; private set; }

    /// <summary>The page-owned opaque row-key → aggregate route map of the history table.</summary>
    public IReadOnlyList<OpenRouteEntry> OpenRoutes { get; private set; } = [];

    /// <summary>H3 — the exact aggregate detail (ficha content), when opened.</summary>
    public FichaView? Ficha { get; private set; }

    /// <summary>H3 — the reabrir affordance state of the opened aggregate (outside the table).</summary>
    public ReopenAffordanceView? Reopen { get; private set; }

    /// <summary>The observed aggregate version of the opened detail.</summary>
    public int ObservedVersion { get; private set; } = 1;

    /// <inheritdoc />
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);
        ViewData["DmoShell"] = await _shell.BuildAsync(current, ShellTitle, ShellContext, cancellationToken);

        if (BoquilhasId is { } boquilhasId)
        {
            await LoadFichaAsync(boquilhasId, cancellationToken);
        }

        await LoadHistoryAsync(cancellationToken);
    }

    private async Task LoadHistoryAsync(CancellationToken cancellationToken)
    {
        var query = new BoquilhasHistoryQuery(
            Normalize(State),
            Normalize(Reference),
            Normalize(Lot),
            Normalize(Machine),
            BusinessDateFrom,
            BusinessDateTo,
            Normalize(MovementType),
            RepairerId,
            Math.Max(PageNumber, 1),
            PageSize);

        var result = await _service.GetHistoryAsync(query, cancellationToken);

        switch (result)
        {
            case BoquilhasResult.HistoryFound(var rows, var total):
                BuildHistory(rows, total);
                break;

            case BoquilhasResult.ValidationFailed(var errors):
                ValidationErrors = errors;
                break;

            case BoquilhasResult.Refused(var reason, var message):
                _logger.LogWarning("Histórico refused ({Reason}): {Message}", reason, message);
                break;

            default:
                _logger.LogWarning("Unexpected history result {Result}.", result.GetType().Name);
                break;
        }
    }

    private void BuildHistory(IReadOnlyList<HistoryItemReadModel> rows, int total)
    {
        var columns = new List<DenseTableColumnPresentation>
        {
            Column("reference", "Referência"),
            Column("lot", "Lote"),
            Column("machines", "Máquinas"),
            Column("state", "Estado"),
            Column("openingDate", "Data de abertura"),
            Column("inicio", "Início"),
            Column("disponivel", "Disponível"),
            Column("emReparacao", "Em reparação"),
            Column("irreparavel", "Irreparável"),
            Column("movements", "Movimentos"),
            Column("lastMovement", "Último movimento"),
        };

        var tableRows = rows
            .Select(row => DenseTableRowPresentation.Create(
                row.BoquilhasId.ToString(),
                $"Boquilhas {row.BoquilhasId} — {row.Reference ?? "(sem contexto)"}",
                [
                    DenseTableCellPresentation.Create(row.Reference ?? "—"),
                    DenseTableCellPresentation.Create(row.Lot ?? "—"),
                    DenseTableCellPresentation.Create(string.Join(", ", row.Machines)),
                    DenseTableCellPresentation.Create(row.State == "closed" ? "Fechado" : "Ativo"),
                    DenseTableCellPresentation.Create(row.OpeningDate.ToString("yyyy-MM-dd")),
                    DenseTableCellPresentation.Create(row.InitialQuantity.ToString()),
                    DenseTableCellPresentation.Create(row.Disponivel.ToString()),
                    DenseTableCellPresentation.Create(row.EmReparacao.ToString()),
                    DenseTableCellPresentation.Create(row.Irreparavel.ToString()),
                    DenseTableCellPresentation.Create(row.MovementCount.ToString()),
                    DenseTableCellPresentation.Create(
                        row.LastMovementAt?.ToString("yyyy-MM-dd HH:mm") + " UTC" ?? "—"),
                ],
                status: RecordStatusPresentation.Create(
                    row.State == "closed" ? "Fechado" : "Ativo",
                    row.State == "closed" ? StatusTone.Neutral : StatusTone.Success)))
            .ToList();

        History = DenseTablePresentation.Ready(
            caption: "Histórico de Boquilhas",
            columns,
            tableRows,
            selectionEnabled: true,
            openEnabled: true,
            resultSummary: rows.Count == 0 ? null : $"{total} linha" + (total == 1 ? "" : "s"),
            filters:
            [
                Filter("reference", "Referência", Reference),
                Filter("lot", "Lote", Lot),
                Filter("machine", "Máquina", Machine),
                Filter("state", "Estado", State ?? string.Empty),
            ]);

        OpenRoutes = rows
            .Select(row => new OpenRouteEntry(
                row.BoquilhasId.ToString(),
                $"/boquilhas/historico?boquilhasId={row.BoquilhasId}"))
            .ToList();
    }

    private async Task LoadFichaAsync(Guid boquilhasId, CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(boquilhasId, cancellationToken);

        if (result is not BoquilhasResult.Ficha(var ficha))
        {
            _logger.LogWarning("Ficha for '{BoquilhasId}' could not be loaded.", boquilhasId);
            return;
        }

        ObservedVersion = ficha.Version;
        Ficha = new FichaView(ficha);

        // H3 — the outside-table reopen affordance (a closed aggregate may be reopened here iff the
        // exact eligibility holds; the backend re-asserts everything).
        Reopen = new ReopenAffordanceView(
            ficha.State == "closed",
            ficha.State == "closed"
                ? "Reabrir este registo"
                : "O registo está ativo.",
            ficha.State == "closed" ? ObservedVersion : 0);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DenseTableColumnPresentation Column(string key, string heading) =>
        DenseTableColumnPresentation.Create(key, heading, alignment: DenseTableColumnAlignment.Start);

    private static DenseTableFilterPresentation Filter(string key, string label, string? value) =>
        DenseTableFilterPresentation.Create(key, label, value ?? string.Empty);

    /// <summary>A row of the page-owned open-route map (opaque key → exact aggregate route).</summary>
    public sealed record OpenRouteEntry(string Key, string Href);

    /// <summary>The H3 detail view state (the ficha read model + presentation).</summary>
    public sealed record FichaView(BoquilhasFichaReadModel Ficha)
    {
        /// <summary>The movement-type label of one ledger row (presentation only).</summary>
        public string Label(string movementType) =>
            MovementKindTokens.Parse(movementType) is { } kind
                ? MovementKindTokens.ToLabel(kind)
                : movementType;
    }

    /// <summary>The H3 reopen affordance state (actions live OUTSIDE the table).</summary>
    public sealed record ReopenAffordanceView(bool Available, string Label, int Version);
}