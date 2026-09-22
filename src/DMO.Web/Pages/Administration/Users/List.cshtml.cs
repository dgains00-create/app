using DMO.Application.Session;
using DMO.Application.UserAdministration;
using DMO.Web.Authorization;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.Administration.Users;

/// <summary>ADMIN-only USER list (active and inactive).</summary>
[Authorize(Policy = AdministrationAuthorizationPolicies.PolicyName)]
public sealed class ListModel : PageModel
{
    private readonly IUserAdministrationService _service;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;

    /// <summary>Creates the page over the administration service and the shared shell.</summary>
    public ListModel(
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

    /// <summary>The listed USERs.</summary>
    public IReadOnlyList<UserListItem> Users { get; private set; } = [];

    /// <inheritdoc />
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Users = await _service.ListAsync(cancellationToken);
        await SetShellAsync(
            "Utilizadores",
            "Administração — contas de utilizador do sistema",
            cancellationToken);
    }

    private async Task SetShellAsync(string title, string context, CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);
        ViewData["DmoShell"] = await _shell.BuildAsync(current, title, context, cancellationToken);
    }
}