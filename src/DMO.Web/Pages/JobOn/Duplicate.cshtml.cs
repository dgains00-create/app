using DMO.Application.JobOn;
using DMO.Application.Session;
using DMO.Domain.Tools;
using DMO.Web.Authorization;
using DMO.Web.Frontend.Shared.Contracts;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.JobOn;

/// <summary>
/// Job On duplication surface: the read-only preview of an explicitly chosen source plus the new
/// occurrence's own facts.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §13.2 route 9, §10.1/§10.2 and §13.4.
/// <para>
/// The source is always explicit and is never inferred: no "latest", no "previous" and no recency
/// filter exists. Any historical source may be previewed. Opening this page writes nothing, touches no
/// version and creates no draft Job On.
/// </para>
/// </remarks>
[Authorize(Policy = JobOnPolicyNames.JobOnCreate)]
public sealed class DuplicateModel : PageModel
{
    private const string ShellTitle = "Duplicar Job On";
    private const string ShellContext = "Nova ocorrência a partir de uma produção existente";

    private readonly IJobOnService _service;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;

    /// <summary>Creates the page over the Job On service and the shared shell.</summary>
    public DuplicateModel(
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

    /// <summary>The previewed source ficha.</summary>
    public JobOnFicha? Source { get; private set; }

    /// <summary>The previewed source version (the concurrency contract of the duplication).</summary>
    public int SourceVersion { get; private set; }

    /// <summary>The source's contexts, each with its frozen triple and live projection.</summary>
    public IReadOnlyList<JobOnContextRegion> Contexts { get; private set; } = [];

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

        var result = await _service.PreviewDuplicateAsync(jobonId, cancellationToken);

        if (result is JobOnResult.NotFound)
        {
            return NotFound();
        }

        if (result is not JobOnResult.DuplicationPreview(_, var sourceVersion, var source))
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        Source = source;
        SourceVersion = sourceVersion;
        Contexts = source.Contexts.Select(JobOnContextRegion.From).ToList();

        Actions = DecisionBarPresentation.Create(
            CommonState.Ready,
            [
                DecisionBarActionPresentation.Create(
                    SharedActionPresentation.CreateEnabled("duplicate", "Duplicar", "A duplicar…"),
                    DecisionBarActionGroup.Primary),
                DecisionBarActionPresentation.Create(
                    SharedActionPresentation.CreateEnabled("cancel", "Cancelar"),
                    DecisionBarActionGroup.Secondary),
            ],
            regionLabel: "Ações da duplicação");

        return Page();
    }
}
