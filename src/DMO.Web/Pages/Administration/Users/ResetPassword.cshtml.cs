using DMO.Application.Session;
using DMO.Application.UserAdministration;
using DMO.Web.Authorization;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.Administration.Users;

/// <summary>
/// ADMIN-only password-reset confirmation. The reset only initiates the provider recovery
/// flow (the provider emails a link; the USER chooses the new password). The current password
/// is never read or displayed and no token is persisted or logged.
/// </summary>
[Authorize(Policy = AdministrationAuthorizationPolicies.PolicyName)]
public sealed class ResetPasswordModel : PageModel
{
    private const string ShellTitle = "Repor palavra-passe";
    private const string ShellContext = "Inicia o fluxo de recuperação do fornecedor de identidade";

    private readonly IUserAdministrationService _service;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;

    /// <summary>Creates the page over the administration service and the shared shell.</summary>
    public ResetPasswordModel(
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
        var command = new UserAdministrationCommands.RequestedPasswordResetCommand(id, ExpectedVersion);

        var result = await _service.InitiatePasswordResetAsync(command, cancellationToken);
        switch (result)
        {
            case UserAdministrationResult.Success:
                return RedirectToEdit(
                    id,
                    "Password reset initiated: the provider sent a recovery link; the user chooses the new password.");
            case UserAdministrationResult.ValidationFailed(var errors):
                return await WithErrorsAsync(errors, cancellationToken);
            case UserAdministrationResult.Conflict(_, var message):
                return await WithErrorAsync(message, cancellationToken);
            case UserAdministrationResult.NotFound:
                return NotFound();
            case UserAdministrationResult.ProviderFailed:
                return await WithErrorAsync(
                    "The provider could not initiate the reset. Nothing was altered; retry later.",
                    cancellationToken);
            default:
                return await WithErrorAsync("The reset could not be initiated. Reload and retry.", cancellationToken);
        }
    }

    private IActionResult RedirectToEdit(Guid id, string message)
    {
        TempData["message"] = message;
        return RedirectToPage("Edit", new { id });
    }

    private async Task<IActionResult> WithErrorsAsync(
        IReadOnlyList<string> errors,
        CancellationToken cancellationToken)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

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