using DMO.Application.JobOn;
using DMO.Application.Session;
using DMO.Application.Tools;
using DMO.Web.Authorization;
using DMO.Web.Frontend.Shared.Contracts;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.JobOn;

/// <summary>
/// Job On sheet: the occurrence plus its existing contexts, each with its frozen values.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §13.2 route 3, §8.4 and §13.4. Read only. The frozen triple is presented
/// as read-only historical data and is never refreshed from the current Tool; the live Tool facts are
/// a separate read-time projection that is never persisted.
/// </remarks>
[Authorize(Policy = JobOnPolicyNames.JobOnView)]
public sealed class ViewModel : PageModel
{
    private const string ShellTitle = "Job On";
    private const string ShellContext = "Ficha da ocorrência de produção";

    private readonly IJobOnService _service;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;

    /// <summary>Creates the page over the Job On service and the shared shell.</summary>
    public ViewModel(
        IJobOnService service,
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

    /// <summary>The read ficha, or <c>null</c> when it does not exist.</summary>
    public JobOnFicha? Ficha { get; private set; }

    /// <summary>One region per existing context.</summary>
    public IReadOnlyList<JobOnContextRegion> Contexts { get; private set; } = [];

    /// <inheritdoc />
    public async Task<IActionResult> OnGetAsync(Guid jobonId, CancellationToken cancellationToken)
    {
        await SetShellAsync(cancellationToken);

        var result = await _service.GetAsync(jobonId, cancellationToken);

        if (result is JobOnResult.NotFound)
        {
            // A missing occurrence is 404: never an empty page and never a fabricated record.
            return NotFound();
        }

        if (result is not JobOnResult.Ficha(var ficha))
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        Ficha = ficha;
        Contexts = ficha.Contexts.Select(JobOnContextRegion.From).ToList();

        return Page();
    }

    private async Task SetShellAsync(CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);
        ViewData["DmoShell"] = await _shell.BuildAsync(current, ShellTitle, ShellContext, cancellationToken);
    }
}

/// <summary>
/// One rendered context region: the frozen triple plus the live Tool projection.
/// </summary>
/// <remarks>
/// The two fact classes are kept separately visible so the surface can never present frozen history as
/// current Tool state, or the other way round.
/// </remarks>
public sealed record JobOnContextRegion(
    string Token,
    string FrozenType,
    string FrozenReference,
    string FrozenLot,
    ToolSummaryRowPresentation Live)
{
    /// <summary>Builds the region from a context ficha.</summary>
    public static JobOnContextRegion From(ToolContextFicha context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var token = ToolTokens.ToToken(context.ContextType);
        var live = context.Tool;

        return new JobOnContextRegion(
            token,
            ToolTokens.ToToken(context.ToolType),
            context.ToolReference,
            context.ToolLot,
            ToolSummaryRowPresentation.Create(
                CommonState.Ready,
                $"context-{token}",
                $"Ferramenta atual do contexto {token}",
                type: ToolSummaryFactPresentation.Create("Tipo atual", ToolTokens.ToToken(live.Type)),
                reference: ToolSummaryFactPresentation.Create("Referência atual", live.Reference),
                lot: ToolSummaryFactPresentation.Create("Lote atual", live.Lot),
                machines: live.CompatibleMachines.Count > 0
                    ? ToolSummaryFactPresentation.Create(
                        "Máquinas",
                        string.Join(", ", live.CompatibleMachines.Select(machine => machine.Value)))
                    : null,
                quantity: live.Quantity is { } quantity
                    ? ToolSummaryFactPresentation.Create("Quantidade", quantity.ToString())
                    : null,
                process: ToolTokens.ToToken(live.Processo) is { } processo
                    ? ToolSummaryFactPresentation.Create("Processo", processo)
                    : null));
    }
}
