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
/// Job On edit surface: the four editable facts plus the three explicit Tool associations.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §13.2 route 7, §11.2 and §18. The association mechanism is the explicit
/// Keep/Set/Remove change list: the surface derives it by comparing the slot's original canonical
/// identity with the one the operator selected, and it never treats "no value" as "remove".
/// <para>
/// The frozen triple is presented as read-only historical data: it is never an editable field, and it
/// is refreshed only as the direct consequence of an explicit Tool re-selection for that slot.
/// </para>
/// </remarks>
[Authorize(Policy = JobOnPolicyNames.JobOnCreate)]
public sealed class EditModel : PageModel
{
    private const string ShellTitle = "Editar Job On";
    private const string ShellContext = "Edição da ocorrência de produção";
    private const int ToolSearchLimit = 50;

    private readonly IJobOnService _jobOns;
    private readonly IToolService _tools;
    private readonly IModuleAccessService _access;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;
    private readonly ILogger<EditModel> _logger;

    /// <summary>Creates the page over the Job On/Tool services, the access seam and the shared shell.</summary>
    public EditModel(
        IJobOnService jobOns,
        IToolService tools,
        IModuleAccessService access,
        ICurrentAccountContext currentAccount,
        ShellPresentationService shell,
        ILogger<EditModel> logger)
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

    /// <summary>The canonical Tool currently associated with the CM slot.</summary>
    [BindProperty(SupportsGet = true)]
    public Guid? CmToolId { get; set; }

    /// <summary>The canonical Tool currently associated with the MF slot.</summary>
    [BindProperty(SupportsGet = true)]
    public Guid? MfToolId { get; set; }

    /// <summary>The canonical Tool currently associated with the BQ slot.</summary>
    [BindProperty(SupportsGet = true)]
    public Guid? BqToolId { get; set; }

    /// <summary>The edited occurrence's ficha.</summary>
    public JobOnFicha? Ficha { get; private set; }

    /// <summary>The three association regions, in the settled CM/MF/BQ order.</summary>
    public IReadOnlyList<JobOnEditSlotRegion> Slots { get; private set; } = [];

    /// <summary>
    /// Whether the persisted production date is already reached or passed, so an unacknowledged save
    /// or delete is refused — a warning gate, never hard immutability.
    /// </summary>
    public bool DateThresholdReached { get; private set; }

    /// <summary>Whether the caller holds the contextual Ferramentas capability (create affordance).</summary>
    public bool CanCreateTool { get; private set; }

    /// <summary>The action region of this surface.</summary>
    public DecisionBarPresentation Actions { get; private set; } = null!;

    /// <summary>The contracted machine codes available for selection.</summary>
    public static IReadOnlyList<string> Machines { get; } =
        MachineCode.All.Select(machine => machine.Value).ToList();

    /// <inheritdoc />
    public async Task<IActionResult> OnGetAsync(Guid jobonId, CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);
        ViewData["DmoShell"] = await _shell.BuildAsync(current, ShellTitle, ShellContext, cancellationToken);

        var result = await _jobOns.GetAsync(jobonId, cancellationToken);

        if (result is JobOnResult.NotFound)
        {
            return NotFound();
        }

        if (result is not JobOnResult.Ficha(var ficha))
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        Ficha = ficha;
        DateThresholdReached = ficha.ProductionDate is { } date && DateOnly.FromDateTime(DateTime.UtcNow) >= date;
        CanCreateTool = await HasFerramentasAsync(current, cancellationToken);
        Slots = await BuildSlotsAsync(ficha, cancellationToken);

        Actions = DecisionBarPresentation.Create(
            CommonState.Ready,
            [
                DecisionBarActionPresentation.Create(
                    SharedActionPresentation.CreateEnabled("save", "Guardar", "A guardar…"),
                    DecisionBarActionGroup.Primary),
                DecisionBarActionPresentation.Create(
                    SharedActionPresentation.CreateEnabled("delete", "Eliminar", "A eliminar…"),
                    DecisionBarActionGroup.Danger),
                DecisionBarActionPresentation.Create(
                    SharedActionPresentation.CreateEnabled("cancel", "Cancelar"),
                    DecisionBarActionGroup.Secondary),
            ],
            regionLabel: "Ações do Job On");

        return Page();
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

    private async Task<IReadOnlyList<JobOnEditSlotRegion>> BuildSlotsAsync(
        JobOnFicha ficha,
        CancellationToken cancellationToken)
    {
        var requestedSlot = ToolTokens.ParseContextType(Slot?.Trim());
        var regions = new List<JobOnEditSlotRegion>(JobOnSlotTokens.All.Count);

        foreach (var contextType in JobOnSlotTokens.All)
        {
            var context = ficha.Contexts.FirstOrDefault(candidate => candidate.ContextType == contextType);
            var supplied = SuppliedToolId(contextType);
            var associated = supplied ?? context?.ToolId;

            var items = new List<ToolSearchItem>();
            var lookupFailed = false;
            var searched = false;

            if (requestedSlot == contextType)
            {
                searched = true;

                try
                {
                    var query = new ToolSearchQuery(
                        SlotQuery,
                        ToolTokens.RequiredToolType(contextType),
                        ficha.Reference,
                        SlotLot,
                        MachineCode.Parse(SlotMachine?.Trim()),
                        ToolSearchLimit);

                    var result = await _tools.SearchAsync(query, cancellationToken);
                    switch (result)
                    {
                        case ToolResult.SearchResults(var found):
                            items.AddRange(found);
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
            if (associated is { } toolId)
            {
                adapter.Associate(toolId);
            }

            var state = lookupFailed
                ? CommonState.LookupFailed
                : searched && items.Count == 0
                    ? CommonState.Empty
                    : CommonState.Ready;

            var picker = adapter.BuildPresentation(
                state,
                $"Ferramentas do contexto {adapter.SlotToken}",
                SlotQuery,
                lookupFailed
                    ? "Não foi possível obter as ferramentas."
                    : state == CommonState.Empty
                        ? "Sem ferramentas correspondentes."
                        : null,
                resultSummary: searched && !lookupFailed ? $"{items.Count} ferramenta(s) encontradas." : null,
                createAction: CanCreateTool
                    ? SharedActionPresentation.CreateEnabled("create-tool", "Criar ferramenta", "A criar…")
                    : null,
                cancelAction: SharedActionPresentation.CreateEnabled("cancel", "Cancelar"),
                retryAction: lookupFailed
                    ? SharedActionPresentation.CreateEnabled("retry", "Tentar novamente")
                    : null);

            regions.Add(new JobOnEditSlotRegion(
                JobOnSlotTokens.Token(contextType),
                JobOnSlotTokens.HiddenField(contextType),
                JobOnSlotTokens.SearchField(contextType),
                context?.ContextId,
                context?.ToolId,
                associated,
                context is null
                    ? null
                    : $"{ToolTokens.ToToken(context.ToolType)} {context.ToolReference} lote {context.ToolLot}",
                picker,
                adapter.CandidateMap));
        }

        return regions;
    }

    private Guid? SuppliedToolId(ToolContextType contextType) => contextType switch
    {
        ToolContextType.Cm => CmToolId,
        ToolContextType.Mf => MfToolId,
        _ => BqToolId,
    };
}

/// <summary>
/// One rendered association region of the edit surface: the existing context (if any), the slot's
/// original canonical identity and the current selection.
/// </summary>
/// <remarks>
/// The original identity is what makes the explicit Keep/Set/Remove change list possible without
/// conflating "leave as is" with "remove": comparing the two is the surface's own, local decision.
/// </remarks>
public sealed record JobOnEditSlotRegion(
    string Token,
    string HiddenFieldName,
    string SearchFieldName,
    Guid? ContextId,
    Guid? OriginalToolId,
    Guid? AssociatedToolId,
    string? FrozenSummary,
    ToolPickerPresentation Picker,
    IReadOnlyList<ToolCandidateEntry> CandidateMap);
