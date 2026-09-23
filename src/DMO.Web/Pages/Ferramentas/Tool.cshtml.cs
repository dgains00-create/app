using DMO.Application.Session;
using DMO.Application.Tools;
using DMO.Web.Pages.JobOn;
using DMO.Web.Frontend.Shared.Contracts;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.Ferramentas;

/// <summary>
/// Contextual Tool ficha: the canonical Tool's current facts plus the Job On occurrences using it.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §13.2 route 14, §8.5 and §13.6.
/// <para>
/// Ferramentas is contextual-only: this page is reachable only from an operational context and only
/// with the <c>ferramentas</c> grant. It is <b>not</b> a top-level destination, it appears in no
/// navigation, and it carries no edit/create action contract — P2-T04 exposes no Tool mutation.
/// </para>
/// </remarks>
[Authorize(Policy = JobOnPolicyNames.Ferramentas)]
public sealed class ToolModel : PageModel
{
    private const string ShellTitle = "Ferramenta";
    private const string ShellContext = "Ficha contextual da ferramenta canónica";

    private readonly IToolService _service;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;

    /// <summary>Creates the page over the Tool service and the shared shell.</summary>
    public ToolModel(
        IToolService service,
        ICurrentAccountContext currentAccount,
        ShellPresentationService shell)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(currentAccount);
        ArgumentNullException.ThrowIfNull(shell);
        _service = service;
        _currentAccount = currentAccount;
        _shell = shell;
    }

    /// <summary>The contextual Tool ficha, or <c>null</c> when the Tool does not exist.</summary>
    public ToolFicha? Ficha { get; private set; }

    /// <summary>The Tool-owned facts rendered through the accepted shared summary row.</summary>
    public ToolSummaryRowPresentation Facts { get; private set; } = null!;

    /// <summary>The Job On occurrences using this Tool (the contracted reverse read).</summary>
    public DenseTablePresentation Usages { get; private set; } = null!;

    /// <inheritdoc />
    public async Task<IActionResult> OnGetAsync(Guid toolId, CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);
        ViewData["DmoShell"] = await _shell.BuildAsync(current, ShellTitle, ShellContext, cancellationToken);

        var result = await _service.GetAsync(toolId, cancellationToken);

        if (result is ToolResult.NotFound)
        {
            return NotFound();
        }

        if (result is not ToolResult.Found(var ficha))
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        Ficha = ficha;

        Facts = ToolSummaryRowPresentation.Create(
            CommonState.Ready,
            $"tool-{ficha.ToolId}",
            $"Ferramenta {ficha.Reference} lote {ficha.Lot}",
            type: ToolSummaryFactPresentation.Create("Tipo", ToolTokens.ToToken(ficha.Type)),
            reference: ToolSummaryFactPresentation.Create("Referência", ficha.Reference),
            lot: ToolSummaryFactPresentation.Create("Lote", ficha.Lot),
            machines: ficha.CompatibleMachines.Count > 0
                ? ToolSummaryFactPresentation.Create(
                    "Máquinas",
                    string.Join(", ", ficha.CompatibleMachines.Select(machine => machine.Value)))
                : null,
            quantity: ficha.Quantity is { } quantity
                ? ToolSummaryFactPresentation.Create("Quantidade", quantity.ToString())
                : null,
            process: ToolTokens.ToToken(ficha.Processo) is { } processo
                ? ToolSummaryFactPresentation.Create("Processo", processo)
                : null);

        Usages = BuildUsages(ficha);

        return Page();
    }

    private static DenseTablePresentation BuildUsages(ToolFicha ficha)
    {
        var columns = new List<DenseTableColumnPresentation>
        {
            DenseTableColumnPresentation.Create("reference", "Referência"),
            DenseTableColumnPresentation.Create("production-number", "Número de produção"),
            DenseTableColumnPresentation.Create("machine", "Máquina"),
        };

        var rows = ficha.UsageOccurrences
            .Select((usage, index) => DenseTableRowPresentation.Create(
                $"u-{index}",
                $"{usage.Reference} — produção {usage.ProductionNumber}",
                [
                    DenseTableCellPresentation.Create(usage.Reference),
                    DenseTableCellPresentation.Create(usage.ProductionNumber),
                    DenseTableCellPresentation.Create(usage.Machine),
                ]))
            .ToList();

        return DenseTablePresentation.Create(
            rows.Count > 0 ? CommonState.Ready : CommonState.Empty,
            "Ocorrências que usam esta ferramenta",
            columns,
            rows,
            message: rows.Count > 0 ? null : "Esta ferramenta ainda não foi usada em nenhuma produção.",
            resultSummary: $"{rows.Count} ocorrência(s).",
            regionLabel: "Ocorrências");
    }
}
