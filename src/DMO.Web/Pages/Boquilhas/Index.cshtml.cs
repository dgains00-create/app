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
/// Registo surface (P2-T07 OWNER CLARIFICATION): the register list (production context + derived
/// outstanding) and the opened register's core screen — production context, movement ledger,
/// movement entry, movement edit/detail with the AuditTrail.
/// </summary>
/// <remarks>
/// Authority: P2-T07 OWNER CLARIFICATION. The core screen focuses on: production context,
/// Boquilhas movements, add movement, edit selected movement. There is NO lifecycle UI: no
/// close/reopen controls, no active/closed status, no opening-facts/utilisation surface. Tables
/// follow the accepted arbitration (single click selects, double click opens, actions outside the
/// table; no per-row action grids). The page is server-gated <c>boquilhas</c>; mutations are
/// executed by the page-owned adapter against the accepted minimal-API routes; backend validation
/// is authoritative and a stale movement version enters the accepted conflict presentation with
/// the explicit reload recovery (D2).</remarks>
[Authorize(Policy = BoquilhasPolicyNames.Boquilhas)]
public sealed class IndexModel : PageModel
{
    private const string ShellTitle = "Boquilhas — Registo";
    private const string ShellContext = "Registo de movimentos de reparação externa BQ";

    private readonly IBoquilhasService _service;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>Creates the page over the Boquilhas service, the current account and the shell.</summary>
    public IndexModel(
        IBoquilhasService service,
        ICurrentAccountContext currentAccount,
        ShellPresentationService shell,
        ILogger<IndexModel> logger)
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

    // ---- list filters (backend-applied) ------------------------------------------------------

    /// <summary>Reference filter (traversal through the REAL BQ context).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Reference { get; set; }

    /// <summary>Lot filter (traversal).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Lot { get; set; }

    /// <summary>1-based page (DenseDataTable consumer-owned paging).</summary>
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    /// <summary>Page size (1–100).</summary>
    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 50;

    /// <summary>Open the exact register (single/double click arbitration).</summary>
    [BindProperty(SupportsGet = true)]
    public Guid? BoquilhasId { get; set; }

    /// <summary>Open the exact movement's edit/detail (double click on the ledger row).</summary>
    [BindProperty(SupportsGet = true)]
    public Guid? MovementId { get; set; }

    // ---- presentation state ----------------------------------------------------------------

    /// <summary>The exact validation codes of a refused query.</summary>
    public IReadOnlyList<string> ValidationErrors { get; private set; } = [];

    /// <summary>The register list, or <c>null</c> on a refused query.</summary>
    public DenseTablePresentation? Registers { get; private set; }

    /// <summary>The page-owned opaque row-key → register route map of the list.</summary>
    public IReadOnlyList<OpenRouteEntry> OpenRoutes { get; private set; } = [];

    /// <summary>The opened register (production context + ledger + outstanding).</summary>
    public RegisterView? Register { get; private set; }

    /// <summary>The movement-entry resolutions (assignments + register, consumed reads).</summary>
    public MovementEntryContext? Entry { get; private set; }

    /// <summary>The opened movement's edit/detail (trail via AuditTrail), when opened.</summary>
    public MovementDetailView? Detail { get; private set; }

    /// <inheritdoc />
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);
        ViewData["DmoShell"] = await _shell.BuildAsync(current, ShellTitle, ShellContext, cancellationToken);

        if (BoquilhasId is { } boquilhasId)
        {
            await LoadRegisterAsync(boquilhasId, cancellationToken);
        }

        await LoadRegisterListAsync(cancellationToken);
    }

    private async Task LoadRegisterListAsync(CancellationToken cancellationToken)
    {
        var query = new BoquilhasListQuery(
            Normalize(Reference),
            Normalize(Lot),
            Math.Max(PageNumber, 1),
            PageSize);

        var result = await _service.GetListAsync(query, cancellationToken);

        switch (result)
        {
            case BoquilhasResult.ListFound(var rows, var total):
                BuildRegisterList(rows, total);
                break;

            case BoquilhasResult.ValidationFailed(var errors):
                ValidationErrors = errors;
                break;

            case BoquilhasResult.Refused(var reason, var message):
                _logger.LogWarning("Register list refused ({Reason}): {Message}", reason, message);
                break;

            default:
                _logger.LogWarning("Unexpected register-list result {Result}.", result.GetType().Name);
                break;
        }
    }

    private void BuildRegisterList(IReadOnlyList<RegisterListItemReadModel> rows, int total)
    {
        var columns = new List<DenseTableColumnPresentation>
        {
            Column("reference", "Referência"),
            Column("lot", "Lote"),
            Column("production", "Produção"),
            Column("machine", "Máquina"),
            Column("productionDate", "Data de produção"),
            Column("outstanding", "Pendente de reparação"),
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
                    DenseTableCellPresentation.Create(row.ProductionNumber ?? "—"),
                    DenseTableCellPresentation.Create(row.ProductionMachine ?? "—"),
                    DenseTableCellPresentation.Create(row.ProductionDate?.ToString("yyyy-MM-dd") ?? "—"),
                    DenseTableCellPresentation.Create(row.Outstanding.ToString()),
                    DenseTableCellPresentation.Create(row.MovementCount.ToString()),
                    DenseTableCellPresentation.Create(row.LastMovementAt?.ToString("yyyy-MM-dd HH:mm") + " UTC" ?? "—"),
                ],
                status: RecordStatusPresentation.Create(
                    row.Outstanding > 0 ? "Com saldo pendente" : "Liquidação nula",
                    row.Outstanding > 0 ? StatusTone.Warning : StatusTone.Neutral)))
            .ToList();

        Registers = DenseTablePresentation.Ready(
            caption: "Registos de Boquilhas",
            columns,
            tableRows,
            selectionEnabled: true,
            openEnabled: true,
            resultSummary: rows.Count == 0 ? null : $"{total} linha" + (total == 1 ? "" : "s"),
            filters:
            [
                Filter("reference", "Referência", Reference),
                Filter("lot", "Lote", Lot),
            ]);

        OpenRoutes = rows
            .Select(row => new OpenRouteEntry(
                row.BoquilhasId.ToString(),
                $"/boquilhas?boquilhasId={row.BoquilhasId}"))
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

        // The movement-entry resolutions (consumed reads; never administered here).
        var assignmentsResult = await _service.GetMachineAssignmentsAsync(cancellationToken);
        var repairersResult = await _service.GetRepairersAsync(cancellationToken);

        Entry = new MovementEntryContext(
            assignmentsResult is BoquilhasResult.AssignmentsFound(var assignments) ? assignments : [],
            repairersResult is BoquilhasResult.RepairersFound(var repairers) ? repairers : []);

        // The opened movement's edit/detail with its AuditTrail (double click on a ledger row).
        if (MovementId is { } movementId)
        {
            var movement = ficha.Movements.FirstOrDefault(candidate => candidate.MovementId == movementId);
            if (movement is not null)
            {
                var auditResult = await _service.GetMovementAuditAsync(boquilhasId, movementId, cancellationToken);

                if (auditResult is BoquilhasResult.MovementAuditFound(var auditId, var entries))
                {
                    Detail = new MovementDetailView(
                        movement,
                        entries,
                        BuildMovementTrail(entries));
                }
            }
        }
    }

    private static AuditTrailPresentation BuildMovementTrail(IReadOnlyList<MovementAuditItemReadModel> entries)
    {
        if (entries.Count == 0)
        {
            return AuditTrailPresentation.Empty(
                "Histórico de edições",
                "Sem edições — este movimento nunca foi alterado.");
        }

        var trailEntries = entries
            .Select(entry => AuditEntryPresentation.Create(
                entry.MovementAuditId.ToString(),
                "Edição do movimento",
                actor: entry.EditedByUserId.ToString(),
                actorUnavailableText: null,
                timestampText: entry.EditedAt.ToString("yyyy-MM-dd HH:mm") + " UTC",
                timestampValue: entry.EditedAt,
                detail: BuildAuditDetail(entry),
                beforeDetail: null,
                afterDetail: "versão atualizada",
                detailAction: null))
            .ToList();

        return AuditTrailPresentation.Ready("Histórico de edições", trailEntries);
    }

    private static string BuildAuditDetail(MovementAuditItemReadModel entry) =>
        $"Quantidade: {entry.BeforeQuantity} → {entry.AfterQuantity}; " +
        $"Data: {entry.BeforeBusinessDate:yyyy-MM-dd} → {entry.AfterBusinessDate:yyyy-MM-dd}; " +
        $"Máquina: {entry.BeforeMachine ?? "—"} → {entry.AfterMachine ?? "—"}; " +
        $"Reparador: {entry.BeforeRepairerId?.ToString() ?? "—"} → {entry.AfterRepairerId?.ToString() ?? "—"}; " +
        $"Observações: {entry.BeforeObservations ?? "—"} → {entry.AfterObservations ?? "—"}";

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DenseTableColumnPresentation Column(string key, string heading) =>
        DenseTableColumnPresentation.Create(key, heading, alignment: DenseTableColumnAlignment.Start);

    private static DenseTableFilterPresentation Filter(string key, string label, string? value) =>
        DenseTableFilterPresentation.Create(key, label, value ?? string.Empty);

    /// <summary>A row of the page-owned open-route map (opaque key → exact register route).</summary>
    public sealed record OpenRouteEntry(string Key, string Href);

    /// <summary>The opened register view state (the ficha read model + presentation).</summary>
    public sealed record RegisterView(RegisterFichaReadModel Ficha)
    {
        /// <summary>The movement-type label of one ledger row (presentation only).</summary>
        public string Label(string movementType) =>
            MovementKindTokens.Parse(movementType) is { } kind
                ? MovementKindTokens.ToLabel(kind)
                : movementType;
    }

    /// <summary>The movement-entry resolutions (consumed reads; never administered here).</summary>
    public sealed record MovementEntryContext(
        IReadOnlyList<MachineRepairerAssignmentReadModel> Assignments,
        IReadOnlyList<RepairerReadModel> Repairers);

    /// <summary>The movement edit/detail view state.</summary>
    public sealed record MovementDetailView(
        MovementReadModel Movement,
        IReadOnlyList<MovementAuditItemReadModel> Entries,
        AuditTrailPresentation Trail);
}