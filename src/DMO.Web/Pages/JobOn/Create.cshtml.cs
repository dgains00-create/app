using DMO.Application.Access;
using DMO.Application.Accounts;
using DMO.Application.JobOn;
using DMO.Application.Session;
using DMO.Application.Tools;
using DMO.Domain.Tools;
using DMO.Web.Authorization;
using DMO.Web.Frontend.Shared.Contracts;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.JobOn;

/// <summary>
/// Job On create surface: the simplified Beta facts plus the optional CM/MF/BQ Tool associations.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §13.2 route 4, §11.1, §18. The surface composes the accepted P2-T03
/// ToolPicker through the P2-T04 orchestration adapter; the picker remains presentation/mechanics
/// only, and the canonical identities live in this surface's own form state.
/// <para>
/// The origin state is preserved: the Tool search is a round trip to this same route carrying the
/// form's own values, and the Tool create subflow posts to the contextual Ferramentas endpoint without
/// leaving this page (no standalone Tool create page and no server-side draft store exist).
/// </para>
/// </remarks>
[Authorize(Policy = JobOnPolicyNames.JobOnCreate)]
public sealed class CreateModel : PageModel
{
    private const string ShellTitle = "Novo Job On";
    private const string ShellContext = "Criação da ocorrência de produção";
    private const int ToolSearchLimit = 50;

    private readonly IJobOnService _jobOns;
    private readonly IToolService _tools;
    private readonly IModuleAccessService _access;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;
    private readonly ILogger<CreateModel> _logger;

    /// <summary>Creates the page over the Job On/Tool services, the access seam and the shared shell.</summary>
    public CreateModel(
        IJobOnService jobOns,
        IToolService tools,
        IModuleAccessService access,
        ICurrentAccountContext currentAccount,
        ShellPresentationService shell,
        ILogger<CreateModel> logger)
    {
        ArgumentNullException.ThrowIfNull(jobOns);
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentNullException.ThrowIfNull(access);
        ArgumentNullException.ThrowIfNull(currentAccount);
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(logger);
        _jobOns = jobOns;
        _tools = tools;
        _access = access;
        _currentAccount = currentAccount;
        _shell = shell;
        _logger = logger;
    }

    /// <summary>Production reference (create fact).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Reference { get; set; }

    /// <summary>Production number (create fact).</summary>
    [BindProperty(SupportsGet = true)]
    public string? ProductionNumber { get; set; }

    /// <summary>Machine code (create fact).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Machine { get; set; }

    /// <summary>Optional planned production date (create fact).</summary>
    [BindProperty(SupportsGet = true)]
    public DateOnly? ProductionDate { get; set; }

    /// <summary>Prefill-only reference supplied by the origin context.</summary>
    [BindProperty(SupportsGet = true)]
    public string? SourceReference { get; set; }

    /// <summary>The slot whose Tool search was requested.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Slot { get; set; }

    /// <summary>The searched slot's free-text query.</summary>
    [BindProperty(SupportsGet = true)]
    public string? SlotQuery { get; set; }

    /// <summary>The searched slot's lot criterion.</summary>
    [BindProperty(SupportsGet = true)]
    public string? SlotLot { get; set; }

    /// <summary>The searched slot's machine criterion.</summary>
    [BindProperty(SupportsGet = true)]
    public string? SlotMachine { get; set; }

    /// <summary>The canonical Tool associated with the CM slot.</summary>
    [BindProperty(SupportsGet = true)]
    public Guid? CmToolId { get; set; }

    /// <summary>The canonical Tool associated with the MF slot.</summary>
    [BindProperty(SupportsGet = true)]
    public Guid? MfToolId { get; set; }

    /// <summary>The canonical Tool associated with the BQ slot.</summary>
    [BindProperty(SupportsGet = true)]
    public Guid? BqToolId { get; set; }

    /// <summary>The three association regions, in the settled CM/MF/BQ order.</summary>
    public IReadOnlyList<JobOnSlotRegion> Slots { get; private set; } = [];

    /// <summary>Existing productions of the reference (existing-production detection).</summary>
    public DenseTablePresentation? ExistingProductions { get; private set; }

    /// <summary>The P2-T04-owned opaque row-key → ficha route map of the existing productions.</summary>
    public IReadOnlyList<JobOnRouteEntry> ProductionRoutes { get; private set; } = [];

    /// <summary>Whether the productions lookup did not complete.</summary>
    public bool ProductionsLookupFailed { get; private set; }

    /// <summary>Whether the caller holds the contextual Ferramentas capability (create affordance).</summary>
    public bool CanCreateTool { get; private set; }

    /// <summary>The action region of this surface.</summary>
    public DecisionBarPresentation Actions { get; private set; } = null!;

    /// <summary>The contracted machine codes available for selection.</summary>
    public static IReadOnlyList<string> Machines { get; } =
        MachineCode.All.Select(machine => machine.Value).ToList();

    /// <inheritdoc />
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);
        ViewData["DmoShell"] = await _shell.BuildAsync(current, ShellTitle, ShellContext, cancellationToken);

        // Pre-fill is assistance only: the known context may propose the reference, and nothing else.
        Reference = string.IsNullOrWhiteSpace(Reference) ? SourceReference?.Trim() : Reference.Trim();
        ProductionNumber = ProductionNumber?.Trim();

        CanCreateTool = await HasFerramentasAsync(current, cancellationToken);

        if (!string.IsNullOrWhiteSpace(Reference))
        {
            await LoadExistingProductionsAsync(Reference, cancellationToken);
        }

        Slots = await BuildSlotsAsync(cancellationToken);

        Actions = DecisionBarPresentation.Create(
            CommonState.Ready,
            [
                DecisionBarActionPresentation.Create(
                    SharedActionPresentation.CreateEnabled("save", "Guardar", "A guardar…"),
                    DecisionBarActionGroup.Primary),
                DecisionBarActionPresentation.Create(
                    SharedActionPresentation.CreateEnabled("cancel", "Cancelar"),
                    DecisionBarActionGroup.Secondary),
            ],
            regionLabel: "Ações do Job On");
    }

    private async Task<bool> HasFerramentasAsync(CurrentAccount current, CancellationToken cancellationToken)
    {
        if (current is not CurrentAccount.User(var user))
        {
            return false;
        }

        return await _access.HasModuleAsync(
            new AccountResolution.User(user),
            ModuleCatalog.Ferramentas,
            cancellationToken);
    }

    private async Task LoadExistingProductionsAsync(string reference, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _jobOns.FindProductionsAsync(new FindProductionsQuery(reference), cancellationToken);

            if (result is not JobOnResult.ProductionsFound(var productions))
            {
                ProductionsLookupFailed = result is not JobOnResult.ValidationFailed;
                return;
            }

            var columns = new List<DenseTableColumnPresentation>
            {
                DenseTableColumnPresentation.Create("reference", "Referência"),
                DenseTableColumnPresentation.Create("production-number", "Número de produção"),
                DenseTableColumnPresentation.Create("machine", "Máquina"),
                DenseTableColumnPresentation.Create("production-date", "Data de produção"),
            };

            var rows = new List<DenseTableRowPresentation>(productions.Count);
            var routes = new List<JobOnRouteEntry>(productions.Count);

            for (var index = 0; index < productions.Count; index++)
            {
                var production = productions[index];
                var key = $"p-{index}";
                routes.Add(new JobOnRouteEntry(key, $"/jobon/{production.JobOnId}"));

                rows.Add(DenseTableRowPresentation.Create(
                    key,
                    $"{production.Reference} — produção {production.ProductionNumber}",
                    [
                        DenseTableCellPresentation.Create(production.Reference),
                        DenseTableCellPresentation.Create(production.ProductionNumber),
                        DenseTableCellPresentation.Create(production.Machine),
                        DenseTableCellPresentation.Create(
                            production.ProductionDate?.ToString("yyyy-MM-dd") ?? "sem data"),
                    ]));
            }

            ProductionRoutes = routes;
            ExistingProductions = DenseTablePresentation.Create(
                CommonState.Ready,
                "Produções já existentes para esta referência",
                columns,
                rows,
                openEnabled: true,
                resultSummary: $"{rows.Count} produção(ões) existentes.",
                actionsColumnHeading: "Abrir",
                regionLabel: "Produções existentes");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Existing-production detection failed.");
            ProductionsLookupFailed = true;
        }
    }

    private async Task<IReadOnlyList<JobOnSlotRegion>> BuildSlotsAsync(CancellationToken cancellationToken)
    {
        var requestedSlot = ToolTokens.ParseContextType(Slot?.Trim());
        var regions = new List<JobOnSlotRegion>(JobOnSlotTokens.All.Count);

        foreach (var contextType in JobOnSlotTokens.All)
        {
            var isRequested = requestedSlot == contextType;
            var items = new List<ToolSearchItem>();
            var lookupFailed = false;
            var searched = false;

            if (isRequested)
            {
                searched = true;

                try
                {
                    // Only the contracted predicates are applied. The search is pre-filled from the
                    // known origin context and nothing is inferred from it.
                    var query = new ToolSearchQuery(
                        SlotQuery,
                        ToolTokens.RequiredToolType(contextType),
                        Reference,
                        SlotLot,
                        MachineCode.Parse(SlotMachine?.Trim()),
                        ToolSearchLimit);

                    var result = await _tools.SearchAsync(query, cancellationToken);
                    switch (result)
                    {
                        case ToolResult.SearchResults(var found):
                            items.AddRange(found);
                            break;

                        case ToolResult.ValidationFailed(var errors):
                            _logger.LogWarning(
                                "The Tool search criteria were rejected: {Errors}",
                                string.Join(", ", errors));
                            lookupFailed = true;
                            break;

                        default:
                            lookupFailed = true;
                            break;
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Tool search failed for slot {Slot}.", contextType);
                    lookupFailed = true;
                }
            }

            var adapter = new JobOnToolPickerAdapter(contextType, items);
            var associated = AssociatedToolId(contextType);

            if (associated is { } toolId)
            {
                adapter.Associate(toolId);
            }

            var state = lookupFailed
                ? CommonState.LookupFailed
                : searched && items.Count == 0
                    ? CommonState.Empty
                    : CommonState.Ready;

            var message = lookupFailed
                ? "Não foi possível obter as ferramentas."
                : state == CommonState.Empty
                    ? "Sem ferramentas correspondentes."
                    : null;

            var picker = adapter.BuildPresentation(
                state,
                $"Ferramentas do contexto {adapter.SlotToken}",
                SlotQuery,
                message,
                resultSummary: searched && !lookupFailed ? $"{items.Count} ferramenta(s) encontradas." : null,
                createAction: CanCreateTool
                    ? SharedActionPresentation.CreateEnabled("create-tool", "Criar ferramenta", "A criar…")
                    : null,
                cancelAction: SharedActionPresentation.CreateEnabled("cancel", "Cancelar"),
                retryAction: lookupFailed
                    ? SharedActionPresentation.CreateEnabled("retry", "Tentar novamente")
                    : null);

            regions.Add(new JobOnSlotRegion(
                JobOnSlotTokens.Token(contextType),
                JobOnSlotTokens.HiddenField(contextType),
                JobOnSlotTokens.SearchField(contextType),
                associated,
                associated?.ToString(),
                picker,
                adapter.CandidateMap));
        }

        return regions;
    }

    private Guid? AssociatedToolId(ToolContextType contextType) => contextType switch
    {
        ToolContextType.Cm => CmToolId,
        ToolContextType.Mf => MfToolId,
        _ => BqToolId,
    };
}
