using DMO.Application.Session;
using DMO.Web.Authorization;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.Administration;

/// <summary>
/// P1-T07 ADMIN-only landing page. Reuses the accepted <c>dmo.administration</c> policy
/// gate; no Admin Module and no role-based permission are introduced.
/// </summary>
/// <remarks>
/// <para>
/// The page renders only the real administration destinations currently available in the
/// build (USER administration — P1-T05; Template administration — P1-T06). No future Admin
/// feature is invented or advertised (no settings/audit placeholders: P1-T08/P1-T09 are not
/// started).
/// </para>
/// <para>
/// ADMIN has no operational Template, receives no USER navigation and is never routed into
/// operational surfaces from here (<c>ACCESS_MODEL</c> §12 / ADMIN contract §10).
/// </para>
/// </remarks>
[Authorize(Policy = AdministrationAuthorizationPolicies.PolicyName)]
public sealed class IndexModel : PageModel
{
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;

    /// <summary>Creates the page over the current-account boundary and the shared shell.</summary>
    public IndexModel(ICurrentAccountContext currentAccount, ShellPresentationService shell)
    {
        ArgumentNullException.ThrowIfNull(currentAccount);
        ArgumentNullException.ThrowIfNull(shell);
        _currentAccount = currentAccount;
        _shell = shell;
    }

    /// <inheritdoc />
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);
        ViewData["DmoShell"] = await _shell.BuildAsync(
            current,
            "Administração",
            "Painel de administração do sistema",
            cancellationToken);
    }
}