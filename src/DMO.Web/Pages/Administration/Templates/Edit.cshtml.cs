using DMO.Application.Access;
using DMO.Application.Session;
using DMO.Application.TemplateAdministration;
using DMO.Application.UserAdministration;
using DMO.Web.Authorization;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DMO.Web.Pages.Administration.Templates;

/// <summary>
/// ADMIN-only Template ficha: facts + ordered composition (available Modules reorderable;
/// persisted unavailable/invalid Modules surfaced locked with explicit removal), landing
/// (validity surfaced; an invalid persisted landing requires an explicit correction/removal —
/// never silently cleared), and the associated USERs with assign/remove/reassign over the
/// single <c>users.template_id</c> relation.
/// </summary>
[Authorize(Policy = AdministrationAuthorizationPolicies.PolicyName)]
public sealed class EditModel : PageModel
{
    private const string ShellTitle = "Detalhes do template";
    private const string ShellContext = "Composição de módulos, destino de entrada e utilizadores associados";

    private readonly ITemplateAdministrationService _service;
    private readonly IUserAdministrationService _users;
    private readonly IModuleRegistry _registry;
    private readonly ICurrentAccountContext _currentAccount;
    private readonly ShellPresentationService _shell;

    /// <summary>Creates the page over the administration services, the registry and the shared shell.</summary>
    public EditModel(
        ITemplateAdministrationService service,
        IUserAdministrationService users,
        IModuleRegistry registry,
        ICurrentAccountContext currentAccount,
        ShellPresentationService shell)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(currentAccount);
        ArgumentNullException.ThrowIfNull(shell);
        _service = service;
        _users = users;
        _registry = registry;
        _currentAccount = currentAccount;
        _shell = shell;
    }

    /// <summary>Display name (edit form).</summary>
    [BindProperty]
    public string EditName { get; set; } = string.Empty;

    /// <summary>Selected Module ids (checkbox values + hidden preserves, ordered by <see cref="ModuleOrder"/>).</summary>
    [BindProperty]
    public List<string> SelectedModuleIds { get; set; } = [];

    /// <summary>Per-Module order input (1..n; dense order is normalized by the service).</summary>
    [BindProperty]
    public Dictionary<string, int> ModuleOrder { get; set; } = [];

    /// <summary>Ids explicitly checked "remove" among the locked persisted unavailable/invalid Modules.</summary>
    [BindProperty]
    public List<string> RemoveModuleIds { get; set; } = [];

    /// <summary>Selected landing destination, or <c>null</c> for no landing.</summary>
    [BindProperty]
    public string? EditLandingDestinationId { get; set; }

    /// <summary>Expected Template version (edit form, hidden — never silently overwritten).</summary>
    [BindProperty]
    public int EditExpectedVersion { get; set; }

    /// <summary>User id for the assign form (candidate pickers use row forms with versions instead).</summary>
    [BindProperty]
    public Guid AssignUserId { get; set; }

    /// <summary>Expected USER version (assign form, hidden).</summary>
    [BindProperty]
    public int AssignUserVersion { get; set; }

    /// <summary>User id for the remove form (hidden).</summary>
    [BindProperty]
    public Guid RemoveUserId { get; set; }

    /// <summary>Expected USER version (remove form, hidden).</summary>
    [BindProperty]
    public int RemoveUserVersion { get; set; }

    /// <summary>User id for the reassign form (hidden).</summary>
    [BindProperty]
    public Guid ReassignUserId { get; set; }

    /// <summary>Expected USER version (reassign form, hidden).</summary>
    [BindProperty]
    public int ReassignUserVersion { get; set; }

    /// <summary>Target Template for the reassign form.</summary>
    [BindProperty]
    public Guid? ReassignTargetTemplateId { get; set; }

    /// <summary>The loaded ficha for rendering.</summary>
    public TemplateFicha? Ficha { get; private set; }

    /// <summary>The available Modules offered by the editor (registry order).</summary>
    public IReadOnlyList<ModuleDefinition> AvailableModules { get; private set; } = [];

    /// <summary>Distinct landing destinations of the available non-contextual Modules.</summary>
    public IReadOnlyList<string> LandingOptions { get; private set; } = [];

    /// <summary>Candidate USERs for the assign form (not currently members of this Template).</summary>
    public IReadOnlyList<UserListItem> AssignCandidates { get; private set; } = [];

    /// <summary>Templates for the reassign selector.</summary>
    public IReadOnlyList<TemplateOption> TemplateOptions { get; private set; } = [];

    /// <inheritdoc />
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken))
        {
            return NotFound();
        }

        PopulateFromFicha();
        await SetShellAsync(cancellationToken);
        return Page();
    }

    /// <summary>Applies the factual edit (name + composition + landing, expected version).</summary>
    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken))
        {
            return NotFound();
        }

        var command = new TemplateAdministrationCommands.UpdateTemplateCommand(
            id,
            EditName,
            OrderSelectedModules(),
            EditLandingDestinationId,
            EditExpectedVersion);

        var result = await _service.UpdateAsync(command, cancellationToken);
        switch (result)
        {
            case TemplateAdministrationResult.Success:
                return OkWithMessage("Template saved.");
            case TemplateAdministrationResult.ValidationFailed(var errors):
                return await WithErrorsAsync(errors, cancellationToken);
            case TemplateAdministrationResult.Conflict(_, var message):
                return RedirectToSelfWith(message);
            case TemplateAdministrationResult.NotFound:
                return NotFound();
            default:
                return await WithErrorAsync("The template could not be saved. Reload and retry.", cancellationToken);
        }
    }

    /// <summary>Assigns a candidate USER to this Template.</summary>
    public async Task<IActionResult> OnPostAssignAsync(Guid id, CancellationToken cancellationToken)
    {
        return await RunMembershipAsync(id, targetTemplateId: id, AssignUserId, AssignUserVersion, cancellationToken);
    }

    /// <summary>Removes the USER's Template association (template_id → null).</summary>
    public async Task<IActionResult> OnPostRemoveUserAsync(Guid id, CancellationToken cancellationToken)
    {
        return await RunMembershipAsync(id, targetTemplateId: null, RemoveUserId, RemoveUserVersion, cancellationToken);
    }

    /// <summary>Reassigns the USER from this Template to another.</summary>
    public async Task<IActionResult> OnPostReassignAsync(Guid id, CancellationToken cancellationToken)
    {
        return await RunMembershipAsync(
            id, ReassignTargetTemplateId, ReassignUserId, ReassignUserVersion, cancellationToken);
    }

    private async Task<IActionResult> RunMembershipAsync(
        Guid id,
        Guid? targetTemplateId,
        Guid userId,
        int userVersion,
        CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken))
        {
            return NotFound();
        }

        if (targetTemplateId is null && Ficha!.Users.All(member => member.UserId != userId))
        {
            // UX convenience only: a stone-stale ficha is rejected before round-tripping to the
            // service. The authoritative remove invariant (the USER must currently belong to
            // this Template) lives in TemplateAdministrationService.SetTemplateUserAsync, so the
            // minimal API DELETE route has exactly the same semantics.
            return await WithErrorAsync(
                "The selected USER is not associated with this Template; reload and retry.",
                cancellationToken);
        }

        var command = new TemplateAdministrationCommands.SetTemplateUserCommand(
            id, userId, targetTemplateId, userVersion);

        var result = await _service.SetTemplateUserAsync(command, cancellationToken);
        switch (result)
        {
            case TemplateAdministrationResult.Success:
                return OkWithMessage(DescribeMembership(targetTemplateId));
            case TemplateAdministrationResult.ValidationFailed(var errors):
                return await WithErrorsAsync(errors, cancellationToken);
            case TemplateAdministrationResult.Conflict(_, var message):
                return RedirectToSelfWith(message);
            case TemplateAdministrationResult.NotFound:
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

        AvailableModules = _registry.AvailableModules;
        LandingOptions = AvailableModules
            .Where(definition => definition.DestinationId is not null)
            .Select(definition => definition.DestinationId!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var allUsers = await _users.ListAsync(cancellationToken);
        var memberIds = Ficha.Users.Select(member => member.UserId).ToHashSet();
        AssignCandidates = allUsers
            .Where(user => !memberIds.Contains(user.UserId))
            .ToArray();

        TemplateOptions = await _service.ListTemplateOptionsAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Populates the hidden version carriers and form defaults from the loaded ficha. Locked
    /// persisted unavailable/invalid Modules are preserved by default (hidden inputs); the
    /// Admin removes them only by explicitly checking the remove box.
    /// </summary>
    private void PopulateFromFicha()
    {
        EditName = Ficha!.Name;
        EditExpectedVersion = Ficha.Version;
        EditLandingDestinationId = Ficha.LandingDestinationId;

        ModuleOrder = new Dictionary<string, int>(StringComparer.Ordinal);
        SelectedModuleIds = [];
        foreach (var module in Ficha.Modules)
        {
            SelectedModuleIds.Add(module.ModuleId);
            ModuleOrder[module.ModuleId] = 1 + Array.IndexOf(Ficha.Modules.ToArray(), module);
        }
    }

    /// <summary>
    /// Orders the submitted composition: available selections + preserved locked Modules in
    /// the Admin order, minus the explicitly-removed locked ids; the service normalizes to
    /// dense 1..n and re-validates every id (new unavailable selections rejected).
    /// </summary>
    private IReadOnlyList<string> OrderSelectedModules()
    {
        var removed = RemoveModuleIds.ToHashSet(StringComparer.Ordinal);
        var registryIndex = AvailableModules
            .Select((definition, index) => (definition.Id.Value, index))
            .ToDictionary(pair => pair.Value, pair => pair.index, StringComparer.Ordinal);

        return SelectedModuleIds
            .Distinct(StringComparer.Ordinal)
            .Where(moduleId => !removed.Contains(moduleId))
            .OrderBy(moduleId => ModuleOrder.GetValueOrDefault(moduleId, int.MaxValue))
            .ThenBy(moduleId => registryIndex.GetValueOrDefault(moduleId, int.MaxValue))
            .ToArray();
    }

    private static string DescribeMembership(Guid? targetTemplateId) => targetTemplateId is null
        ? "USER removed from this Template (no Template assigned; access fails closed until a new Template is assigned)."
        : "USER association updated.";

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