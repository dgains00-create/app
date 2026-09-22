using DMO.Application.Access;
using DMO.Application.Session;
using DMO.Application.TemplateAdministration;
using DMO.Web.Authorization;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.Administration.Templates;

/// <summary>
/// ADMIN-only Template creation: name, the ordered composition of <b>currently available</b>
/// Modules (canonical ids only) and an optional landing destination (validated against the
/// final composition). Unavailable Modules are never offered: the availability authority is
/// the Module Registry of this build.
/// </summary>
[Authorize(Policy = AdministrationAuthorizationPolicies.PolicyName)]
public sealed class CreateModel : PageModel
{
    private const string ShellTitle = "Criar template";
    private const string ShellContext = "Composição ordenada de módulos disponíveis e destino de entrada opcional";

    private readonly ITemplateAdministrationService _service;
    private readonly IModuleRegistry _registry;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;

    /// <summary>Creates the page over the administration service, the registry and the shared shell.</summary>
    public CreateModel(
        ITemplateAdministrationService service,
        IModuleRegistry registry,
        ICurrentAccountContext currentAccount,
        ShellPresentationService shell)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(currentAccount);
        ArgumentNullException.ThrowIfNull(shell);
        _service = service;
        _registry = registry;
        _currentAccount = currentAccount;
        _shell = shell;
    }

    /// <summary>Display name.</summary>
    [BindProperty]
    public string CreateName { get; set; } = string.Empty;

    /// <summary>Ordered selected Module ids (checkbox values, order by <see cref="ModuleOrder"/>).</summary>
    [BindProperty]
    public List<string> SelectedModuleIds { get; set; } = [];

    /// <summary>Per-Module order input (1..n; dense order is normalized by the service).</summary>
    [BindProperty]
    public Dictionary<string, int> ModuleOrder { get; set; } = [];

    /// <summary>Selected landing destination, or <c>null</c> for no landing.</summary>
    [BindProperty]
    public string? CreateLandingDestinationId { get; set; }

    /// <summary>The available Modules offered by the editor (registry order).</summary>
    public IReadOnlyList<ModuleDefinition> AvailableModules { get; private set; } = [];

    /// <summary>Distinct landing destinations of the available non-contextual Modules.</summary>
    public IReadOnlyList<string> LandingOptions { get; private set; } = [];

    /// <inheritdoc />
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        LoadEditorOptions();
        await SetShellAsync(cancellationToken);
        return Page();
    }

    /// <inheritdoc />
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        LoadEditorOptions();

        var command = new TemplateAdministrationCommands.CreateTemplateCommand(
            CreateName,
            OrderSelectedModules(),
            CreateLandingDestinationId);

        var result = await _service.CreateAsync(command, cancellationToken);
        switch (result)
        {
            case TemplateAdministrationResult.Created(var templateId):
                TempData["message"] = "Template created.";
                return RedirectToPage("Edit", new { id = templateId });
            case TemplateAdministrationResult.ValidationFailed(var errors):
                return await WithErrorsAsync(errors, cancellationToken);
            case TemplateAdministrationResult.Conflict(_, var message):
                return await WithErrorAsync(message, cancellationToken);
            default:
                return await WithErrorAsync("The template could not be created. Reload and retry.", cancellationToken);
        }
    }

    /// <summary>
    /// Orders the selected canonical Module ids by the Admin-provided order inputs, falling
    /// back to registry order for ids without an input; the service normalizes to dense 1..n.
    /// </summary>
    private IReadOnlyList<string> OrderSelectedModules()
    {
        var registryIndex = AvailableModules
            .Select((definition, index) => (definition.Id.Value, index))
            .ToDictionary(pair => pair.Value, pair => pair.index, StringComparer.Ordinal);

        return SelectedModuleIds
            .Distinct(StringComparer.Ordinal)
            .OrderBy(moduleId => ModuleOrder.GetValueOrDefault(moduleId, int.MaxValue))
            .ThenBy(moduleId => registryIndex.GetValueOrDefault(moduleId, int.MaxValue))
            .ToArray();
    }

    private void LoadEditorOptions()
    {
        AvailableModules = _registry.AvailableModules;
        LandingOptions = AvailableModules
            .Where(definition => definition.DestinationId is not null)
            .Select(definition => definition.DestinationId!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
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