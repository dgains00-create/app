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
/// Registo surface (P2-T07 contract §25.1 regions R1–R6): the active lot grid, the opened aggregate
/// register (summary, balance, movements, close/reopen/utilisation regions), movement entry, the
/// action region and the read-only production-line contextual panel.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §23 (active aggregate + utilisation), §24.3 (table interaction:
/// single click selects, double click opens, actions outside the table), §22.4 (R6 contextual panel
/// reads REAL production context; never a simulated Job On sidebar) and §25.1 (regions R1–R6).
/// The page is server-gated <c>boquilhas</c>. Mutations are executed by the page-owned adapter
/// (<c>dmo-boquilhas.js</c>) against the accepted minimal-API routes; backend validation is
/// authoritative, every persisted-state refusal surfaces its typed reason, and a stale version
/// enters the accepted conflict presentation with the explicit reload recovery (D2).</remarks>
[Authorize(Policy = BoquilhasPolicyNames.Boquilhas)]
public sealed class IndexModel : PageModel
{
    private const string ShellTitle = "Boquilhas — Registo";
    private const string ShellContext = "Registo de reparações externas BQ";

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

    // ---- R1 — lot-grid filters (backend-applied) -------------------------------------------

    /// <summary>Aggregate/file-state filter (<c>active</c> default | <c>closed</c>).</summary>
    [BindProperty(SupportsGet = true)]
    public string? State { get; set; }

    /// <summary>Reference filter (traversal: frozen triple | live Tool).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Reference { get; set; }

    /// <summary>Lot filter (traversal).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Lot { get; set; }

    /// <summary>Machine filter (aggregate machine-set membership).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Machine { get; set; }

    /// <summary>1-based page (DenseDataTable consumer-owned paging).</summary>
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    /// <summary>Page size (1–100, contract §24.2 discipline).</summary>
    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 50;

    /// <summary>Open the exact aggregate in R2 (single/double click arbitration).</summary>
    [BindProperty(SupportsGet = true)]
    public Guid? BoquilhasId { get; set; }

    /// <summary>Open the exact movement's edit/detail in R5 (double click on the ledger row).</summary>
    [BindProperty(SupportsGet = true)]
    public Guid? MovementId { get; set; }

    // ---- presentation state ----------------------------------------------------------------

    /// <summary>The exact contracted validation codes of a refused query.</summary>
    public IReadOnlyList<string> ValidationErrors { get; private set; } = [];

    /// <summary>R1 — the lot grid, or <c>null</c> on a refused query.</summary>
    public DenseTablePresentation? LotGrid { get; private set; }

    /// <summary>The page-owned opaque row-key → aggregate route map of the lot grid.</summary>
    public IReadOnlyList<OpenRouteEntry> OpenRoutes { get; private set; } = [];

    /// <summary>R2 — the opened aggregate register (ficha view).</summary>
    public FichaView? Ficha { get; private set; }

    /// <summary>R3 — the movement-entry resolutions (assignments + register, consumed reads).</summary>
    public MovementEntryContext? Entry { get; private set; }

    /// <summary>R4 — the action region (DecisionBar).</summary>
    public DecisionBarPresentation? Actions { get; private set; }

    /// <summary>R5 — the opened movement's edit/detail (trail via AuditTrail), when opened.</summary>
    public MovementDetailView? Detail { get; private set; }

    /// <summary>The observed aggregate version carried to the mutation routes (refreshed only on success).</summary>
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

        await LoadLotGridAsync(cancellationToken);
    }

    private async Task LoadLotGridAsync(CancellationToken cancellationToken)
    {
        var query = new BoquilhasListQuery(
            Normalize(State),
            Normalize(Reference),
            Normalize(Lot),
            Normalize(Machine),
            Math.Max(PageNumber, 1),
            PageSize);

        var result = await _service.GetListAsync(query, cancellationToken);

        switch (result)
        {
            case BoquilhasResult.ListFound(var rows, var total):
                BuildLotGrid(rows, total);
                break;

            case BoquilhasResult.ValidationFailed(var errors):
                ValidationErrors = errors;
                break;

            case BoquilhasResult.Refused(var reason, var message):
                _logger.LogWarning("Lot grid refused ({Reason}): {Message}", reason, message);
                break;

            default:
                _logger.LogWarning("Unexpected lot-grid result {Result}.", result.GetType().Name);
                break;
        }
    }

    private void BuildLotGrid(IReadOnlyList<BoquilhaListItemReadModel> rows, int total)
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
        };

        var tableRows = rows
            .Select(row => DenseTableRowPresentation.Create(
                row.BoquilhasId.ToString(),
                $"Boquilhas {row.BoquilhasId} — {row.Reference ?? "(sem contexto)"}",
                [
                    DenseTableCellPresentation.Create(row.Reference ?? "—"),
                    DenseTableCellPresentation.Create(row.Lot ?? "—"),
                    DenseTableCellPresentation.Create(string.Join(", ", row.Machines)),
                    DenseTableCellPresentation.Create(
                        row.State == "closed" ? "Fechado" : "Ativo"),
                    DenseTableCellPresentation.Create(row.OpeningDate.ToString("yyyy-MM-dd")),
                    DenseTableCellPresentation.Create(row.InitialQuantity.ToString()),
                    DenseTableCellPresentation.Create(row.Disponivel.ToString()),
                    DenseTableCellPresentation.Create(row.EmReparacao.ToString()),
                    DenseTableCellPresentation.Create(row.Irreparavel.ToString()),
                ],
                status: RecordStatusPresentation.Create(
                    row.State == "closed" ? "Fechado" : "Ativo",
                    row.State == "closed" ? StatusTone.Neutral : StatusTone.Success)))
            .ToList();

        // Single click selects the row; double click (or the explicit open control) opens the exact
        // aggregate in R2 (contract §24.3, AC-H3); actions live outside the table.
        LotGrid = DenseTablePresentation.Ready(
            caption: "Registo de Boquilhas",
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
                Filter("state", "Estado", State ?? "active"),
            ]);

        OpenRoutes = rows
            .Select(row => new OpenRouteEntry(
                row.BoquilhasId.ToString(),
                $"/boquilhas?boquilhasId={row.BoquilhasId}"))
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

        // R3: the movement-entry resolutions (consumed reads; never administered here).
        var assignmentsResult = await _service.GetMachineAssignmentsAsync(cancellationToken);
        var repairersResult = await _service.GetRepairersAsync(cancellationToken);

        Entry = new MovementEntryContext(
            assignmentsResult is BoquilhasResult.AssignmentsFound(var assignments) ? assignments : [],
            repairersResult is BoquilhasResult.RepairersFound(var repairers) ? repairers : []);

        // R4: the DecisionBar actions per aggregate state (contract §25.1 R4).
        Actions = BuildActionBar(ficha);

        // R5: the opened movement's edit/detail with its AuditTrail (double click on a ledger row).
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

    private static DecisionBarPresentation BuildActionBar(BoquilhasFichaReadModel ficha)
    {
        var actions = new List<DecisionBarActionPresentation>();
        var isClosed = ficha.State == "closed";

        actions.Add(DecisionBarActionPresentation.Create(
            isClosed
                ? SharedActionPresentation.CreateDisabled("movement", "Registar movimento", "O registo está fechado; reabra-o para registar movimentos.")
                : SharedActionPresentation.CreateEnabled("movement", "Registar movimento", "A registar…"),
            DecisionBarActionGroup.Primary));

        actions.Add(DecisionBarActionPresentation.Create(
            isClosed
                ? SharedActionPresentation.CreateDisabled("edit", "Editar movimento", "O registo está fechado; reabra-o para editar movimentos.")
                : SharedActionPresentation.CreateEnabled("edit", "Editar movimento", "A editar…"),
            DecisionBarActionGroup.Secondary));

        actions.Add(DecisionBarActionPresentation.Create(
            isClosed
                ? SharedActionPresentation.CreateDisabled("close", "Fechar registo", "O registo já está fechado.")
                : SharedActionPresentation.CreateEnabled("close", "Fechar registo", "A fechar…"),
            DecisionBarActionGroup.Danger));

        actions.Add(DecisionBarActionPresentation.Create(
            isClosed
                ? SharedActionPresentation.CreateEnabled("reopen", "Reabrir", "A reabrir…")
                : SharedActionPresentation.CreateDisabled("reopen", "Reabrir", "O registo está ativo."),
            DecisionBarActionGroup.Secondary));

        actions.Add(DecisionBarActionPresentation.Create(
            isClosed
                ? SharedActionPresentation.CreateDisabled("opening", "Guardar alterações de abertura", "O registo está fechado; reabra-o para alterar os dados de abertura.")
                : SharedActionPresentation.CreateEnabled("opening", "Guardar alterações de abertura", "A guardar…"),
            DecisionBarActionGroup.Secondary));

        return DecisionBarPresentation.Create(
            CommonState.Ready,
            actions,
            regionLabel: "Ações do registo",
            statusText: "Início, Saída, Entrada e Irreparável são os únicos tipos de movimento; Editar é uma ação sobre um movimento selecionado.");
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

    /// <summary>A row of the page-owned open-route map (opaque key → exact aggregate route).</summary>
    public sealed record OpenRouteEntry(string Key, string Href);

    /// <summary>The R2 register view state (the ficha read model + derived presentation facts).</summary>
    public sealed record FichaView(BoquilhasFichaReadModel Ficha)
    {
        /// <summary>The movement-type label of one ledger row (presentation only).</summary>
        public string Label(string movementType) =>
            MovementKindTokens.Parse(movementType) is { } kind
                ? MovementKindTokens.ToLabel(kind)
                : movementType;

        /// <summary>The initial quantity (the Início of the ledger); display only.</summary>
        public int InitialQuantity =>
            Ficha.Inicio?.Quantity ?? 0;
    }

    /// <summary>The R3 movement-entry resolutions (consumed reads; never administered here).</summary>
    public sealed record MovementEntryContext(
        IReadOnlyList<MachineRepairerAssignmentReadModel> Assignments,
        IReadOnlyList<RepairerReadModel> Repairers);

    /// <summary>The R5 movement edit/detail view state.</summary>
    public sealed record MovementDetailView(
        MovementReadModel Movement,
        IReadOnlyList<MovementAuditItemReadModel> Entries,
        AuditTrailPresentation Trail);
}