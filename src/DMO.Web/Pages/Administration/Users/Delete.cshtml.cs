using DMO.Application.Session;
using DMO.Application.UserAdministration;
using DMO.Web.Authorization;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.Administration.Users;

/// <summary>
/// ADMIN-only delete confirmation: the provider identity is deleted first, then the
/// version-checked application row; a concurrent change leaves the row (retry anchor) and is
/// reported explicitly, never as success.
/// </summary>
[Authorize(Policy = AdministrationAuthorizationPolicies.PolicyName)]
public sealed class DeleteModel : PageModel
{
    private const string ShellTitle = "Eliminar utilizador";
    private const string ShellContext = "Confirmação — a identidade do fornecedor é removida primeiro";

    private readonly IUserAdministrationService _service;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;

    /// <summary>Creates the page over the administration service and the shared shell.</summary>
    public DeleteModel(
        IUserAdministrationService service,
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
    public UserFicha? Ficha { get; private set; }

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
        var command = new UserAdministrationCommands.DeleteUserCommand(id, ExpectedVersion);

        var result = await _service.DeleteAsync(command, cancellationToken);
        switch (result)
        {
            case UserAdministrationResult.Success:
                return Deleted();
            case UserAdministrationResult.NotFound:
                return NotFound();
            case UserAdministrationResult.Conflict(UserConflictReason.StaleVersion, var message):
                return await PartialStateAsync(id, message, cancellationToken);
            case UserAdministrationResult.Conflict(_, var message):
                return await WithErrorAsync(message, cancellationToken);
            case UserAdministrationResult.ProviderFailed:
                return await WithErrorAsync(
                    "The identity provider could not complete the delete. No application row was changed; retry later.",
                    cancellationToken);
            default:
                return await WithErrorAsync("The user could not be deleted. Reload and retry.", cancellationToken);
        }
    }

    private IActionResult Deleted()
    {
        TempData["message"] = "User deleted (provider identity removed; login disabled immediately).";
        return RedirectToPage("List");
    }

    private async Task<IActionResult> PartialStateAsync(
        Guid id,
        string message,
        CancellationToken cancellationToken)
    {
        // Known recoverable partial state: the provider identity was already removed and the
        // application row was kept (retry anchor). Never reported as success; the refreshed
        // ficha carries the current version so a retry can complete the delete.
        var refreshed = await _service.GetAsync(id, cancellationToken);
        Ficha = refreshed ?? Ficha;
        ExpectedVersion = refreshed?.Version ?? ExpectedVersion;
        ModelState.AddModelError(string.Empty, message);
        await SetShellAsync(cancellationToken);
        return Page();
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