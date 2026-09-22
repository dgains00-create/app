using DMO.Application.Session;
using DMO.Application.TemplateAdministration;
using DMO.Web.Authorization;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.Administration.Templates;

/// <summary>
/// ADMIN-only delete confirmation: the ficha (including the associated-USER count and the
/// warning that those USERs lose their Template and fail closed) is shown with an explicit
/// confirmation; a concurrent change is never silently overwritten (stale → conflict).
/// </summary>
[Authorize(Policy = AdministrationAuthorizationPolicies.PolicyName)]
public sealed class DeleteModel : PageModel
{
    private const string ShellTitle = "Eliminar template";
    private const string ShellContext = "Confirmação — os utilizadores associados perdem o template e fecham por omissão";

    private readonly ITemplateAdministrationService _service;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;

    /// <summary>Creates the page over the administration service and the shared shell.</summary>
    public DeleteModel(
        ITemplateAdministrationService service,
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

    /// <summary>Expected version from the confirmed ficha (hidden).</summary>
    [BindProperty]
    public int ExpectedVersion { get; set; }

    /// <summary>The loaded ficha for the confirmation.</summary>
    public TemplateFicha? Ficha { get; private set; }

    /// <inheritdoc />
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        Ficha = await _service.GetAsync(id, cancellationToken);
        if (Ficha is null)
        {
            return NotFound();
        }

        ExpectedVersion = Ficha.Version;
        await SetShellAsync(cancellationToken);
        return Page();
    }

    /// <inheritdoc />
    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        var command = new TemplateAdministrationCommands.DeleteTemplateCommand(id, ExpectedVersion);

        var result = await _service.DeleteAsync(command, cancellationToken);
        switch (result)
        {
            case TemplateAdministrationResult.Success:
                return Deleted();
            case TemplateAdministrationResult.NotFound:
                return NotFound();
            case TemplateAdministrationResult.Conflict(_, var message):
                return await WithErrorAsync(message, cancellationToken);
            default:
                return await WithErrorAsync("The template could not be deleted. Reload and retry.", cancellationToken);
        }
    }

    private IActionResult Deleted()
    {
        TempData["message"] =
            "Template deleted. All associated users keep their accounts and remain active, but now have " +
            "no Template — access fails closed until a new Template is assigned.";
        return RedirectToPage("List");
    }

    private async Task<IActionResult> WithErrorAsync(string message, CancellationToken cancellationToken)
    {
        ModelState.AddModelError(string.Empty, message);
        await SetShellAsync(cancellationToken);
        return Page();
    }

    private async Task SetShellAsync(CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);
        ViewData["DmoShell"] = await _shell.BuildAsync(current, ShellTitle, ShellContext, cancellationToken);
    }
}