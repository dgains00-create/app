using DMO.Application.Session;
using DMO.Application.UserAdministration;
using DMO.Web.Authorization;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.Administration.Users;

/// <summary>
/// ADMIN-only USER ficha: details, edit of the application-owned facts, activate/deactivate,
/// Template assignment/reassignment/removal, and the confirmed links to delete, password-reset
/// and resend-invite. The internal provider linkage is never shown.
/// </summary>
[Authorize(Policy = AdministrationAuthorizationPolicies.PolicyName)]
public sealed class EditModel : PageModel
{
    private const string ShellTitle = "Detalhes do utilizador";
    private const string ShellContext = "Edição das fichas de aplicação e estado da conta";

    private readonly IUserAdministrationService _service;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;

    /// <summary>Creates the page over the administration service and the shared shell.</summary>
    public EditModel(
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

    /// <summary>Display name (edit form).</summary>
    [BindProperty]
    public string EditName { get; set; } = string.Empty;

    /// <summary>Company number (edit form).</summary>
    [BindProperty]
    public string EditCompanyNumber { get; set; } = string.Empty;

    /// <summary>Carrier email (edit form).</summary>
    [BindProperty]
    public string EditEmail { get; set; } = string.Empty;

    /// <summary>Role label (edit form).</summary>
    [BindProperty]
    public string EditRole { get; set; } = string.Empty;

    /// <summary>Expected version (edit form, hidden).</summary>
    [BindProperty]
    public int EditExpectedVersion { get; set; }

    /// <summary>Selected Template (Template form).</summary>
    [BindProperty]
    public Guid? EditTemplateId { get; set; }

    /// <summary>Expected version (Template form, hidden).</summary>
    [BindProperty]
    public int TemplateExpectedVersion { get; set; }

    /// <summary>Expected version (activate/deactivate forms, hidden).</summary>
    [BindProperty]
    public int ActionExpectedVersion { get; set; }

    /// <summary>The loaded ficha for rendering.</summary>
    public UserFicha? Ficha { get; private set; }

    /// <summary>Templates for the read-only selector.</summary>
    public IReadOnlyList<TemplateOption> TemplateOptions { get; private set; } = [];

    /// <inheritdoc />
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        Ficha = await _service.GetAsync(id, cancellationToken);
        if (Ficha is null)
        {
            return NotFound();
        }

        TemplateOptions = await _service.ListTemplateOptionsAsync(cancellationToken);
        PopulateFromFicha();
        await SetShellAsync(cancellationToken);
        return Page();
    }

    /// <summary>Applies the general edit (application-owned facts + carrier email coordination).</summary>
    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken))
        {
            return NotFound();
        }

        var command = new UserAdministrationCommands.UpdateUserCommand(
            id, EditName, EditCompanyNumber, EditEmail, EditRole, EditExpectedVersion);

        var result = await _service.UpdateAsync(command, cancellationToken);
        switch (result)
        {
            case UserAdministrationResult.Success:
                return OkWithMessage("User saved.");
            case UserAdministrationResult.ValidationFailed(var errors):
                return await WithErrorsAsync(errors, cancellationToken);
            case UserAdministrationResult.Conflict(_, var message):
                return RedirectToSelfWith(message);
            case UserAdministrationResult.NotFound:
                return NotFound();
            case UserAdministrationResult.ProviderFailed:
                return await WithErrorAsync(
                    "The identity provider could not complete the email change. Nothing was altered; retry later.",
                    cancellationToken);
            default:
                return await WithErrorAsync("The user could not be saved. Reload and retry.", cancellationToken);
        }
    }

    /// <summary>Activates the USER (application state only).</summary>
    public async Task<IActionResult> OnPostActivateAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken))
        {
            return NotFound();
        }

        var command = new UserAdministrationCommands.SetUserActiveCommand(id, Active: true, ActionExpectedVersion);
        var result = await _service.SetActiveAsync(command, cancellationToken);
        switch (result)
        {
            case UserAdministrationResult.Success:
                return OkWithMessage("User activated.");
            case UserAdministrationResult.Conflict(_, var message):
                return RedirectToSelfWith(message);
            case UserAdministrationResult.NotFound:
                return NotFound();
            default:
                return await WithErrorAsync("The user could not be activated. Reload and retry.", cancellationToken);
        }
    }

    /// <summary>Deactivates the USER (application state only).</summary>
    public async Task<IActionResult> OnPostDeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken))
        {
            return NotFound();
        }

        var command = new UserAdministrationCommands.SetUserActiveCommand(id, Active: false, ActionExpectedVersion);
        var result = await _service.SetActiveAsync(command, cancellationToken);
        switch (result)
        {
            case UserAdministrationResult.Success:
                return OkWithMessage("User deactivated. Access fails closed on the next request.");
            case UserAdministrationResult.Conflict(_, var message):
                return RedirectToSelfWith(message);
            case UserAdministrationResult.NotFound:
                return NotFound();
            default:
                return await WithErrorAsync("The user could not be deactivated. Reload and retry.", cancellationToken);
        }
    }

    /// <summary>Assigns, reassigns or removes the single Template association.</summary>
    public async Task<IActionResult> OnPostTemplateAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken))
        {
            return NotFound();
        }

        var command = new UserAdministrationCommands.SetUserTemplateCommand(id, EditTemplateId, TemplateExpectedVersion);
        var result = await _service.SetTemplateAsync(command, cancellationToken);
        switch (result)
        {
            case UserAdministrationResult.Success:
                return OkWithMessage("Template association updated.");
            case UserAdministrationResult.ValidationFailed(var errors):
                return await WithErrorsAsync(errors, cancellationToken);
            case UserAdministrationResult.Conflict(_, var message):
                return RedirectToSelfWith(message);
            case UserAdministrationResult.NotFound:
                return NotFound();
            default:
                return await WithErrorAsync(
                    "The Template association could not be changed. Reload and retry.",
                    cancellationToken);
        }
    }

    private async Task<bool> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        Ficha = await _service.GetAsync(id, cancellationToken);
        if (Ficha is null)
        {
            return false;
        }

        TemplateOptions = await _service.ListTemplateOptionsAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Populates the hidden version carriers and the form defaults from the loaded ficha so a
    /// GET renders the current version instead of zero.
    /// </summary>
    private void PopulateFromFicha()
    {
        EditName = Ficha!.Name;
        EditCompanyNumber = Ficha.CompanyNumber;
        EditEmail = Ficha.Email;
        EditRole = Ficha.Role;
        EditExpectedVersion = Ficha.Version;
        EditTemplateId = Ficha.TemplateId;
        TemplateExpectedVersion = Ficha.Version;
        ActionExpectedVersion = Ficha.Version;
    }

    private IActionResult OkWithMessage(string message)
    {
        TempData["message"] = message;
        return RedirectToPage();
    }

    private IActionResult RedirectToSelfWith(string message)
    {
        // Conflict (usually stale version): reload the ficha so the operator works from the
        // current version; the conflict message states nothing was applied.
        TempData["error"] = message;
        return RedirectToPage();
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