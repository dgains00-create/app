using DMO.Application.Session;
using DMO.Application.TemplateAdministration;
using DMO.Web.Authorization;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.Administration.Templates;

/// <summary>ADMIN-only Template list.</summary>
[Authorize(Policy = AdministrationAuthorizationPolicies.PolicyName)]
public sealed class ListModel : PageModel
{
    private readonly ITemplateAdministrationService _service;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;

    /// <summary>Creates the page over the administration service and the shared shell.</summary>
    public ListModel(
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

    /// <summary>The listed Templates.</summary>
    public IReadOnlyList<TemplateListItem> Templates { get; private set; } = [];

    /// <inheritdoc />
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Templates = await _service.ListAsync(cancellationToken);
        await SetShellAsync(
            "Templates",
            "Administração — composições de módulos e destinos atribuídos aos utilizadores",
            cancellationToken);
    }

    private async Task SetShellAsync(string title, string context, CancellationToken cancellationToken)
    {
        var current = await _currentAccount.GetCurrentAsync(cancellationToken);
        ViewData["DmoShell"] = await _shell.BuildAsync(current, title, context, cancellationToken);
    }
}