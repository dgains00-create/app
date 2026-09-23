using DMO.Application.JobOn;
using DMO.Application.Session;
using DMO.Web.Authorization;
using DMO.Web.Frontend.Shared.Contracts;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.JobOn;

/// <summary>
/// Job On consult surface: reference → productions, explicit human selection, open a Job On.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §13.2 route 1 and §13.4. This surface is <b>purely read</b>: it carries
/// the <c>job-on-view</c> policy and exposes no create, edit, duplicate or delete capability, because
/// Job On View never grants Create.
/// </remarks>
[Authorize(Policy = JobOnPolicyNames.JobOnView)]
public sealed class IndexModel : PageModel
{
    private const string ShellTitle = "Job On";
    private const string ShellContext = "Consulta de produções por referência";

    private readonly IJobOnService _service;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>Creates the page over the Job On service and the shared shell.</summary>
    public IndexModel(
        IJobOnService service,
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

    /// <summary>The submitted reference (trimmed).</summary>
    [BindProperty(SupportsGet = true)]
    public string? Reference { get; set; }

    /// <summary>The exact contracted validation error codes of a rejected submission.</summary>
    public IReadOnlyList<string> ValidationErrors { get; private set; } = [];

    /// <summary>Whether the lookup did not complete (never aliased to an empty result).</summary>
    public bool LookupFailed { get; private set; }

    /// <summary>The productions table, or <c>null</c> when no lookup was performed.</summary>
    public DenseTablePresentation? Productions { get; private set; }

    /// <summary>The P2-T04-owned opaque row-key → ficha route map consumed by the page adapter.</summary>
    public IReadOnlyList<JobOnRouteEntry> ProductionRoutes { get; private set; } = [];

    /// <inheritdoc />
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Reference = Reference?.Trim();

        await SetShellAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(Reference))
        {
            return;
        }

        try
        {
            var result = await _service.FindProductionsAsync(
                new FindProductionsQuery(Reference),
                cancellationToken);

            switch (result)
            {
                case JobOnResult.ValidationFailed(var errors):
                    ValidationErrors = errors;
                    break;

                case JobOnResult.ProductionsFound(var productions):
                    BuildProductions(productions);
                    break;

                default:
                    LookupFailed = true;
                    break;
            }
        }
        catch (Exception exception)
        {
            // A lookup failure is reported as a lookup failure and is never aliased to "no results".
            _logger.LogWarning(exception, "Job On productions lookup failed.");
            LookupFailed = true;
        }
    }

    private void BuildProductions(IReadOnlyList<JobOnProductionListItem> productions)
    {
        var columns = new List<DenseTableColumnPresentation>
        {
            DenseTableColumnPresentation.Create("reference", "Referência"),
            DenseTableColumnPresentation.Create("production-number", "Número de produção", widthHint: DenseTableColumnWidthHint.Compact),
            DenseTableColumnPresentation.Create("machine", "Máquina", widthHint: DenseTableColumnWidthHint.Compact),
            DenseTableColumnPresentation.Create("production-date", "Data de produção", widthHint: DenseTableColumnWidthHint.Compact),
        };

        var rows = new List<DenseTableRowPresentation>(productions.Count);
        var routes = new List<JobOnRouteEntry>(productions.Count);

        for (var index = 0; index < productions.Count; index++)
        {
            var production = productions[index];
            var key = $"p-{index}";

            // The row key is opaque: the canonical identity is resolved through the P2-T04 adapter's
            // own route map, never by parsing the key.
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

        // Every matching occurrence is returned, including old ones; the operator selects one
        // explicitly, even when exactly one row is returned — nothing is pre-selected.
        Productions = DenseTablePresentation.Create(
            rows.Count > 0 ? CommonState.Ready : CommonState.Empty,
            "Produções da referência",
            columns,
            rows,
            selectionEnabled: false,
            openEnabled: true,
            message: rows.Count > 0 ? null : "Não existem produções para esta referência.",
            resultSummary: $"{rows.Count} produção(ões) encontradas.",
            actionsColumnHeading: "Abrir",
            regionLabel: "Produções");
    }

    private async Task SetShellAsync(CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);
        ViewData["DmoShell"] = await _shell.BuildAsync(current, ShellTitle, ShellContext, cancellationToken);
    }
}

/// <summary>One P2-T04-owned opaque row-key → route entry of a rendered list.</summary>
public sealed record JobOnRouteEntry(string Key, string Href);
