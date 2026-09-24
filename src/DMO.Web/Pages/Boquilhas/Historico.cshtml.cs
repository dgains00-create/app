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
/// Histórico surface (P2-T07 OWNER CLARIFICATION): the LOCAL Boquilhas Histórico — the
/// movement-level chronological history (production/BQ context → movement history) with backend
/// filters and the exact-aggregate register detail (opened via the row).
/// </summary>
/// <remarks>
/// Authority: P2-T07 OWNER CLARIFICATION. It shows: production context, BQ reference/lot,
/// movement type, quantity, business_date, repairer where applicable, observations,
/// actor/time/audit where appropriate — no Início, no Irreparável, no active/closed/reopened
/// state. Every filter is a backend SQL predicate; unknown/ill-formed values → 400
/// <c>FILTER_INVALID</c> (never a silent full list); paging is 1-based with deterministic
/// ordering and the total is backend-counted for the same predicate. The table is a selection
/// surface: single click selects, double click opens; no per-row action grids (AC-H3). The page
/// is server-gated <c>boquilhas</c>.</remarks>
[Authorize(Policy = BoquilhasPolicyNames.Boquilhas)]
public sealed class HistoricoModel : PageModel
{
    private const string ShellTitle = "Boquilhas — Histórico";
    private const string ShellContext = "Histórico local de movimentos de reparação externa BQ";

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

    // ---- filters (backend-applied) -----------------------------------------------------------

    /// <summary>Reference filter (traversal).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Reference { get; set; }

    /// <summary>Lot filter (traversal).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Lot { get; set; }

    /// <summary>Machine filter (movement fact).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Machine { get; set; }

    /// <summary>Movement business-date period start.</summary>
    [BindProperty(SupportsGet = true)]
    public DateOnly? BusinessDateFrom { get; set; }

    /// <summary>Movement business-date period end.</summary>
    [BindProperty(SupportsGet = true)]
    public DateOnly? BusinessDateTo { get; set; }

    /// <summary>Movement-type filter.</summary>
    [BindProperty(SupportsGet = true)]
    public string? MovementType { get; set; }

    /// <summary>Repairer filter.</summary>
    [BindProperty(SupportsGet = true)]
    public Guid? RepairerId { get; set; }

    /// <summary>1-based page.</summary>
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    /// <summary>Page size (1–100).</summary>
    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 50;

    /// <summary>Open the exact movement's register (detail).</summary>
    [BindProperty(SupportsGet = true)]
    public Guid? BoquilhasId { get; set; }

    // ---- presentation state ------------------------------------------------------------------

    /// <summary>The exact validation codes of a refused query.</summary>
    public IReadOnlyList<string> ValidationErrors { get; private set; } = [];

    /// <summary>The movement-level history table, or <c>null</c> on a refused query.</summary>
    public DenseTablePresentation? History { get; private set; }

    /// <summary>The page-owned opaque row-key → register route map of the history table.</summary>
    public IReadOnlyList<OpenRouteEntry> OpenRoutes { get; private set; } = [];

    /// <summary>The exact register detail (ficha content), when opened.</summary>
    public RegisterView? Register { get; private set; }

    /// <inheritdoc />
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);
        ViewData["DmoShell"] = await _shell.BuildAsync(current, ShellTitle, ShellContext, cancellationToken);

        if (BoquilhasId is { } boquilhasId)
        {
            await LoadRegisterAsync(boquilhasId, cancellationToken);
        }

        await LoadHistoryAsync(cancellationToken);
    }

    private async Task LoadHistoryAsync(CancellationToken cancellationToken)
    {
        var query = new BoquilhasHistoryQuery(
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

    private void BuildHistory(IReadOnlyList<HistoryMovementItemReadModel> rows, int total)
    {
        var columns = new List<DenseTableColumnPresentation>
        {
            Column("reference", "Referência"),
            Column("lot", "Lote"),
            Column("production", "Produção"),
            Column("movement", "Movimento"),
            Column("quantity", "Quantidade"),
            Column("businessDate", "Data de negócio"),
            Column("recordedAt", "Registado em"),
            Column("repairer", "Reparador"),
            Column("machine", "Linha"),
            Column("observations", "Observações"),
        };

        var tableRows = rows
            .Select(row => DenseTableRowPresentation.Create(
                row.MovementId.ToString(),
                $"Boquilhas {row.BoquilhasId} — {row.Reference ?? "(sem contexto)"}",
                [
                    DenseTableCellPresentation.Create(row.Reference ?? "—"),
                    DenseTableCellPresentation.Create(row.Lot ?? "—"),
                    DenseTableCellPresentation.Create(row.ProductionNumber ?? "—"),
                    DenseTableCellPresentation.Create(
                        MovementKindTokens.Parse(row.MovementType) is { } kind
                            ? MovementKindTokens.ToLabel(kind)
                            : row.MovementType),
                    DenseTableCellPresentation.Create(row.Quantity.ToString()),
                    DenseTableCellPresentation.Create(row.BusinessDate.ToString("yyyy-MM-dd")),
                    DenseTableCellPresentation.Create(row.RecordedAt.ToString("yyyy-MM-dd HH:mm") + " UTC"),
                    DenseTableCellPresentation.Create(row.RepairerId?.ToString() ?? "—"),
                    DenseTableCellPresentation.Create(row.Machine ?? "—"),
                    DenseTableCellPresentation.Create(row.Observations ?? "—"),
                ],
                status: RecordStatusPresentation.Create(
                    row.MovementType == "entrada_sem_reparacao" ? "Entrada sem reparação" : row.MovementType,
                    row.MovementType == "entrada_sem_reparacao" ? StatusTone.Warning : StatusTone.Neutral)))
            .ToList();

        History = DenseTablePresentation.Ready(
            caption: "Histórico de movimentos de Boquilhas",
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
                Filter("movementType", "Movimento", MovementType ?? string.Empty),
            ]);

        OpenRoutes = rows
            .Select(row => new OpenRouteEntry(
                row.MovementId.ToString(),
                $"/boquilhas/historico?boquilhasId={row.BoquilhasId}"))
            .ToList();
    }

    private async Task LoadRegisterAsync(Guid boquilhasId, CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(boquilhasId, cancellationToken);

        if (result is not BoquilhasResult.Ficha(var ficha))
        {
            _logger.LogWarning("Ficha for '{BoquilhasId}' could not be loaded.", boquilhasId);
            return;
        }

        Register = new RegisterView(ficha);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DenseTableColumnPresentation Column(string key, string heading) =>
        DenseTableColumnPresentation.Create(key, heading, alignment: DenseTableColumnAlignment.Start);

    private static DenseTableFilterPresentation Filter(string key, string label, string? value) =>
        DenseTableFilterPresentation.Create(key, label, value ?? string.Empty);

    /// <summary>A row of the page-owned open-route map (opaque key → exact register route).</summary>
    public sealed record OpenRouteEntry(string Key, string Href);

    /// <summary>The register detail view state (the ficha read model + presentation).</summary>
    public sealed record RegisterView(RegisterFichaReadModel Ficha)
    {
        /// <summary>The movement-type label of one ledger row (presentation only).</summary>
        public string Label(string movementType) =>
            MovementKindTokens.Parse(movementType) is { } kind
                ? MovementKindTokens.ToLabel(kind)
                : movementType;
    }
}