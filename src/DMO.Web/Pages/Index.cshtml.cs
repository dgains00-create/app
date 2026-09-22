using DMO.Application.Session;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages;

public sealed class IndexModel : PageModel
{
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;

    public IndexModel(ICurrentAccountContext currentAccount, ShellPresentationService shell)
    {
        _currentAccount = currentAccount;
        _shell = shell;
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);
        if (current is CurrentAccount.None)
        {
            return Unauthorized();
        }

        ViewData["DmoShell"] = await _shell.BuildAsync(
            current,
            "Área operacional",
            "Base partilhada para os fluxos industriais DMO",
            cancellationToken);

        return Page();
    }
}
