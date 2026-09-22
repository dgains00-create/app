using DMO.Application.Accounts;
using DMO.Application.Session;
using DMO.Application.UserAdministration;
using DMO.Web.Authorization;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.Administration.Users;

/// <summary>
/// ADMIN-only USER creation: the provider invitation flow. No password field exists — the
/// USER chooses their own password through the provider setup link.
/// </summary>
[Authorize(Policy = AdministrationAuthorizationPolicies.PolicyName)]
public sealed class CreateModel : PageModel
{
    private const string ShellTitle = "Criar utilizador";
    private const string ShellContext = "O convite do fornecedor de identidade estabelece a palavra-passe";

    private readonly IUserAdministrationService _service;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;

    /// <summary>Creates the page over the administration service and the shared shell.</summary>
    public CreateModel(
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

    /// <summary>Display name.</summary>
    [BindProperty]
    public string CreateName { get; set; } = string.Empty;

    /// <summary>Canonical USER login identifier.</summary>
    [BindProperty]
    public string CreateCompanyNumber { get; set; } = string.Empty;

    /// <summary>Provider carrier email.</summary>
    [BindProperty]
    public string CreateEmail { get; set; } = string.Empty;

    /// <summary>Presentation-only free-text role label.</summary>
    [BindProperty]
    public string CreateRole { get; set; } = string.Empty;

    /// <summary>Explicit initial active state (checkbox, default true — never silent).</summary>
    [BindProperty]
    public bool CreateActive { get; set; } = true;

    /// <summary>Optional Template association.</summary>
    [BindProperty]
    public Guid? CreateTemplateId { get; set; }

    /// <summary>Optional absolute http(s) URL for the provider invitation link.</summary>
    [BindProperty]
    public string? CreateInviteRedirectUrl { get; set; }

    /// <summary>Templates for the read-only selector.</summary>
    public IReadOnlyList<TemplateOption> TemplateOptions { get; private set; } = [];

    /// <inheritdoc />
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        TemplateOptions = await _service.ListTemplateOptionsAsync(cancellationToken);
        await SetShellAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        TemplateOptions = await _service.ListTemplateOptionsAsync(cancellationToken);

        var command = new UserAdministrationCommands.CreateUserCommand(
            CreateName, CreateCompanyNumber, CreateEmail, CreateRole,
            CreateActive, CreateTemplateId, CreateInviteRedirectUrl);

        var result = await _service.CreateAsync(command, cancellationToken);
        switch (result)
        {
            case UserAdministrationResult.Created(var account):
                return RedirectToCreated(account);
            case UserAdministrationResult.RecoveredProvisioning(var account):
                return RedirectToCreated(account);
            case UserAdministrationResult.IdempotentReplay(var account):
                return RedirectToCreated(account);
            case UserAdministrationResult.ValidationFailed(var errors):
                return await WithErrorsAsync(errors, cancellationToken);
            case UserAdministrationResult.Conflict(_, var message):
                return await WithErrorAsync(message, cancellationToken);
            case UserAdministrationResult.ProviderFailed(_):
                return await WithErrorAsync(
                    "The identity provider could not complete the invitation. No account was changed; retry later.",
                    cancellationToken);
            default:
                return await WithErrorAsync("The user could not be created. Reload and retry.", cancellationToken);
        }
    }

    private IActionResult RedirectToCreated(UserAccount account)
    {
        TempData["message"] =
            "User created. The setup invitation was sent; the user chooses their own password through the provider link.";
        return RedirectToPage("Edit", new { id = account.AccountId });
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