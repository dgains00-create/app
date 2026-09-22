using DMO.Application.Session;
using DMO.Application.UserAdministration;
using DMO.Web.Authorization;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.Administration.Users;

/// <summary>
/// ADMIN-only resend-invite confirmation — a distinct action from password reset. The provider
/// re-sends the setup invitation while the identity is unconfirmed and rejects once confirmed
/// (surfaced safely; the reset action is offered instead).
/// </summary>
[Authorize(Policy = AdministrationAuthorizationPolicies.PolicyName)]
public sealed class ResendInviteModel : PageModel
{
    private const string ShellTitle = "Reenviar convite";
    private const string ShellContext = "Convite de configuração reenviado pelo fornecedor de identidade";

    private readonly IUserAdministrationService _service;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;

    /// <summary>Creates the page over the administration service and the shared shell.</summary>
    public ResendInviteModel(
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
        var command = new UserAdministrationCommands.ResendInviteCommand(id, ExpectedVersion);

        var result = await _service.ResendInviteAsync(command, cancellationToken);
        switch (result)
        {
            case UserAdministrationResult.Success:
                return RedirectToEdit(id, "Setup invitation re-sent: the provider emailed a fresh setup link.");
            case UserAdministrationResult.ValidationFailed(var errors):
                return await WithErrorsAsync(errors, cancellationToken);
            case UserAdministrationResult.Conflict(_, var message):
                return await WithErrorAsync(message, cancellationToken);
            case UserAdministrationResult.NotFound:
                return NotFound();
            case UserAdministrationResult.ProviderFailed:
                return await WithErrorAsync(
                    "The provider could not re-send the invitation. Nothing was altered; retry later.",
                    cancellationToken);
            default:
                return await WithErrorAsync("The invitation could not be re-sent. Reload and retry.", cancellationToken);
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