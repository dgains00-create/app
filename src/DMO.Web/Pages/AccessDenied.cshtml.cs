using DMO.Application.Session;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages;

/// <summary>
/// P1-T07 no-access USER surface. Rendered through the accepted A2 shared shell.
/// </summary>
/// <remarks>
/// <para>
/// Exact account behavior (accepted P1-T07 contract):
/// </para>
/// <code>
/// CurrentAccount.None   → 302 → /Login
/// CurrentAccount.Admin  → 302 → /Administration
/// CurrentAccount.User   → HTTP 403 + generic no-access content, inside the shared A2 shell
/// </code>
/// <para>
/// The page renders <b>generic</b> content only: it never exposes <c>AccessDenialReason</c>,
/// Module ids, Template ids/names, the provider subject, the auth identity, database errors
/// or debug state. The page is not the source of the denial reason — reasons remain
/// server-side facts of the accepted resolution/projection path. The USER shell render is
/// the normal A2 shell render (identity + honest navigation projection + status), with the
/// shared page content below it.
/// </para>
/// </remarks>
public sealed class AccessDeniedModel : PageModel
{
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;

    /// <summary>Creates the page over the current-account boundary and the shared shell.</summary>
    public AccessDeniedModel(ICurrentAccountContext currentAccount, ShellPresentationService shell)
    {
        ArgumentNullException.ThrowIfNull(currentAccount);
        ArgumentNullException.ThrowIfNull(shell);
        _currentAccount = currentAccount;
        _shell = shell;
    }

    /// <inheritdoc />
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);

        return current switch
        {
            CurrentAccount.None => RedirectToPage("/Login"),
            CurrentAccount.Admin => RedirectToPage("/Administration/Index"),
            CurrentAccount.User _ => await RenderNoAccessAsync(current, cancellationToken),
            _ => RedirectToPage("/Login"),
        };
    }

    private async Task<IActionResult> RenderNoAccessAsync(
        CurrentAccount current,
        CancellationToken cancellationToken)
    {
        ViewData["DmoShell"] = await _shell.BuildAsync(
            current,
            "Acesso negado",
            "Sem acesso operacional configurado",
            cancellationToken);

        // HTTP 403 + generic content: no operational grant exists for this USER.
        HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Page();
    }
}