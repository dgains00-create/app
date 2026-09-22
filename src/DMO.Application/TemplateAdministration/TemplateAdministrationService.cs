using DMO.Application.Access;
using DMO.Application.Persistence;
using DMO.Application.Repositories;
using DMO.Application.Templates;
using DMO.Application.UserAdministration;

namespace DMO.Application.TemplateAdministration;

/// <summary>
/// P1-T06 Template administration orchestration: validation (availability/landing against the
/// Module Registry) → application persistence primitives, with the accepted atomic
/// delete-with-members transaction and the exact single <c>users.template_id</c> relation.
/// </summary>
/// <remarks>
/// <para>
/// <b>Create/Update</b> persist the ordered <c>template_modules</c> composition atomically
/// with the <c>templates</c> row; every committed write keeps the dense order 1..n and bumps
/// the Template version. A stale expected version is rejected (never a silent overwrite).
/// </para>
/// <para>
/// <b>Delete</b> runs the accepted atomic sequence in one database transaction
/// (<see cref="ITemplateRepository.DeleteWithMembersAsync"/>): the expected version is
/// verified <b>inside</b> the destructive transaction, every <c>users.template_id</c>
/// reference is nulled in the same transaction, the Template row is removed (composition
/// cascade), and <b>no</b> USER row is ever cascade-deleted. USERs remain, keep their active
/// state and fail closed until a new Template is assigned.
/// </para>
/// <para>
/// <b>Membership</b> reuses <see cref="IUserRepository.SetTemplateAsync"/>, the exact same
/// versioned primitive the USER ficha (P1-T05) uses: assign/remove/reassign are one single
/// column write (<c>users.template_id</c>), never <c>A + B</c>. There is exactly one
/// relationship; no membership table, no secondary model.
/// </para>
/// <para>
/// No audit of any kind is implemented in P1-T06; results are typed so P1-T09 can instrument
/// later.
/// </para>
/// </remarks>
public sealed class TemplateAdministrationService : ITemplateAdministrationService
{
    private readonly ITemplateRepository _templates;
    private readonly ITemplateModuleRepository _templateModules;
    private readonly IUserRepository _users;
    private readonly IModuleRegistry _registry;

    /// <summary>Creates the service over the persistence primitives and the Module Registry.</summary>
    public TemplateAdministrationService(
        ITemplateRepository templates,
        ITemplateModuleRepository templateModules,
        IUserRepository users,
        IModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(templates);
        ArgumentNullException.ThrowIfNull(templateModules);
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(registry);
        _templates = templates;
        _templateModules = templateModules;
        _users = users;
        _registry = registry;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TemplateListItem>> ListAsync(CancellationToken cancellationToken)
    {
        var templates = await _templates.ListAsync(cancellationToken);
        var items = new List<TemplateListItem>(templates.Count);

        foreach (var template in templates)
        {
            var modules = await _templateModules.GetByTemplateAsync(template.TemplateId, cancellationToken);
            var members = await _users.ListByTemplateAsync(template.TemplateId, cancellationToken);

            items.Add(new TemplateListItem(
                template.TemplateId,
                template.Name,
                PresentModules(modules),
                template.LandingDestinationId,
                ValidatorIsLandingRepresented(template.LandingDestinationId, modules),
                members.Count,
                template.Version));
        }

        return items;
    }

    /// <inheritdoc />
    public async Task<TemplateFicha?> GetAsync(Guid templateId, CancellationToken cancellationToken)
    {
        var template = await _templates.GetByIdAsync(templateId, cancellationToken);
        if (template is null)
        {
            return null;
        }

        var modules = await _templateModules.GetByTemplateAsync(templateId, cancellationToken);
        var members = await _users.ListByTemplateAsync(templateId, cancellationToken);

        return BuildFicha(template, modules, members);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TemplateOption>> ListTemplateOptionsAsync(CancellationToken cancellationToken)
    {
        var templates = await _templates.ListAsync(cancellationToken);
        return templates
            .Select(template => new TemplateOption(template.TemplateId, template.Name))
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<TemplateAdministrationResult> CreateAsync(
        TemplateAdministrationCommands.CreateTemplateCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = TemplateAdministrationValidator.Validate(command, _registry);
        if (validationErrors.Count > 0)
        {
            return new TemplateAdministrationResult.ValidationFailed(validationErrors);
        }

        var name = command.Name.Trim();
        var modules = DenseModules(Guid.Empty, command.ModuleIds);

        var templateId = await _templates.CreatedAsync(
            new Template(Guid.Empty, name, NormalizeLanding(command.LandingDestinationId), Version: 1),
            modules,
            cancellationToken);

        return new TemplateAdministrationResult.Created(templateId);
    }

    /// <inheritdoc />
    public async Task<TemplateAdministrationResult> UpdateAsync(
        TemplateAdministrationCommands.UpdateTemplateCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Load the persisted composition so unavailable/unknown persisted entries can be
        // preserved or explicitly removed (never silently repaired, never newly selected).
        var persistedComposition = await _templateModules.GetByTemplateAsync(command.TemplateId, cancellationToken);
        var persistedIds = persistedComposition
            .Select(module => module.ModuleId)
            .ToHashSet(StringComparer.Ordinal);

        var validationErrors = TemplateAdministrationValidator.Validate(
            command,
            persistedIds,
            _registry);
        if (validationErrors.Count > 0)
        {
            return new TemplateAdministrationResult.ValidationFailed(validationErrors);
        }

        var loaded = await _templates.GetByIdAsync(command.TemplateId, cancellationToken);
        if (loaded is null)
        {
            return new TemplateAdministrationResult.NotFound(command.TemplateId);
        }

        if (loaded.Version != command.ExpectedVersion)
        {
            return StaleTemplateConflict(command.TemplateId, command.ExpectedVersion, loaded.Version);
        }

        var modules = DenseModules(command.TemplateId, command.ModuleIds);
        var template = new Template(
            command.TemplateId,
            command.Name.Trim(),
            NormalizeLanding(command.LandingDestinationId),
            command.ExpectedVersion);

        try
        {
            await _templates.UpdatedAsync(template, modules, cancellationToken);
        }
        catch (ConcurrencyConflictException exception)
        {
            // The optimistic boundary is also enforced inside the repository transaction;
            // a concurrent writer between the pre-check and the write is never overwritten.
            return new TemplateAdministrationResult.Conflict(
                TemplateConflictReason.StaleTemplateVersion,
                exception.Message);
        }
        catch (InvalidOperationException)
        {
            return new TemplateAdministrationResult.NotFound(command.TemplateId);
        }

        return await SuccessFichaAsync(command.TemplateId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TemplateAdministrationResult> DeleteAsync(
        TemplateAdministrationCommands.DeleteTemplateCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = TemplateAdministrationValidator.Validate(command);
        if (validationErrors.Count > 0)
        {
            return new TemplateAdministrationResult.ValidationFailed(validationErrors);
        }

        // 1. Load for confirmation / NotFound (non-destructive).
        var loaded = await _templates.GetByIdAsync(command.TemplateId, cancellationToken);
        if (loaded is null)
        {
            return new TemplateAdministrationResult.NotFound(command.TemplateId);
        }

        // 2. Non-destructive version pre-check (the version is re-verified inside the
        //    destructive transaction: stale → rollback, nothing changed).
        if (loaded.Version != command.ExpectedVersion)
        {
            return StaleTemplateConflict(command.TemplateId, command.ExpectedVersion, loaded.Version);
        }

        var modules = await _templateModules.GetByTemplateAsync(command.TemplateId, cancellationToken);
        var members = await _users.ListByTemplateAsync(command.TemplateId, cancellationToken);
        var fichaBeforeDelete = BuildFicha(loaded, modules, members);

        try
        {
            await _templates.DeleteWithMembersAsync(command.TemplateId, command.ExpectedVersion, cancellationToken);
        }
        catch (ConcurrencyConflictException exception)
        {
            return new TemplateAdministrationResult.Conflict(
                TemplateConflictReason.StaleTemplateVersion,
                exception.Message);
        }
        catch (InvalidOperationException)
        {
            return new TemplateAdministrationResult.NotFound(command.TemplateId);
        }

        // Success carries the ficha as it was before the delete (mirrors P1-T05: the loaded
        // row is returned; the Template no longer exists afterwards).
        return new TemplateAdministrationResult.Success(fichaBeforeDelete);
    }

    /// <inheritdoc />
    public async Task<TemplateAdministrationResult> SetTemplateUserAsync(
        TemplateAdministrationCommands.SetTemplateUserCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = TemplateAdministrationValidator.Validate(command);
        if (validationErrors.Count > 0)
        {
            return new TemplateAdministrationResult.ValidationFailed(validationErrors);
        }

        // The ficha context Template must exist (NotFound anchors the whole operation).
        var contextTemplate = await _templates.GetByIdAsync(command.TemplateId, cancellationToken);
        if (contextTemplate is null)
        {
            return new TemplateAdministrationResult.NotFound(command.TemplateId);
        }

        var user = await _users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return new TemplateAdministrationResult.NotFound(command.UserId);
        }

        if (user.Version != command.UserExpectedVersion)
        {
            return new TemplateAdministrationResult.Conflict(
                TemplateConflictReason.StaleUserVersion,
                $"USER '{command.UserId}' was modified concurrently (expected version " +
                $"{command.UserExpectedVersion}, current version {user.Version}); reload and retry.");
        }

        var targetTemplateId = command.TargetTemplateId;
        if (targetTemplateId is { } targetId)
        {
            var targetTemplate = await _templates.GetByIdAsync(targetId, cancellationToken);
            if (targetTemplate is null)
            {
                return new TemplateAdministrationResult.ValidationFailed(
                    ["The selected Template no longer exists; reload the form."]);
            }
        }

        try
        {
            // The exact same versioned primitive as the USER ficha (P1-T05): one column
            // write; assign (this template), remove (null) and reassign A → B are all here.
            await _users.SetTemplateAsync(command.UserId, targetTemplateId, command.UserExpectedVersion, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return new TemplateAdministrationResult.Conflict(
                TemplateConflictReason.StaleUserVersion,
                $"USER '{command.UserId}' was reassigned concurrently; reload and retry.");
        }
        catch (InvalidOperationException)
        {
            return new TemplateAdministrationResult.NotFound(command.UserId);
        }
        catch (UserPersistenceException exception) when (
            exception.Reason == UserPersistenceFailureReason.InvalidTemplateReference)
        {
            return new TemplateAdministrationResult.ValidationFailed(
                ["The selected Template no longer exists; reload the form."]);
        }

        return await SuccessFichaAsync(command.TemplateId, cancellationToken);
    }

    // ------------------------------------------------------------------ model building

    private async Task<TemplateAdministrationResult> SuccessFichaAsync(
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var ficha = await GetAsync(templateId, cancellationToken);
        if (ficha is null)
        {
            // The context Template disappeared concurrently with the write (e.g. deleted in
            // the membership window); never report success without a ficha.
            return new TemplateAdministrationResult.NotFound(templateId);
        }

        return new TemplateAdministrationResult.Success(ficha);
    }

    private TemplateFicha BuildFicha(
        Template template,
        IReadOnlyList<TemplateModule> modules,
        IReadOnlyList<DMO.Application.Accounts.UserAccount> members) =>
        new(
            template.TemplateId,
            template.Name,
            PresentModules(modules),
            template.LandingDestinationId,
            ValidatorIsLandingRepresented(template.LandingDestinationId, modules),
            members
                .Select(member => new TemplateUserMember(
                    member.AccountId,
                    member.DisplayName,
                    member.CompanyNumber,
                    member.RoleLabel,
                    member.IsActive,
                    member.Version))
                .ToArray(),
            template.Version);

    private IReadOnlyList<TemplateModulePresentation> PresentModules(IReadOnlyList<TemplateModule> modules)
    {
        var presentations = new List<TemplateModulePresentation>(modules.Count);
        foreach (var module in modules)
        {
            switch (_registry.Resolve(module.ModuleId))
            {
                case ModuleResolve.Available(var definition):
                    presentations.Add(new TemplateModulePresentation(
                        definition.Id.Value,
                        definition.DisplayName,
                        definition.DestinationId,
                        TemplateModuleState.Available));
                    break;

                case ModuleResolve.KnownUnavailable(var id):
                    presentations.Add(new TemplateModulePresentation(
                        id.Value,
                        ModuleCatalog.All.FirstOrDefault(entry => entry.Id == id)?.DisplayName ?? id.Value,
                        ModuleCatalog.All.FirstOrDefault(entry => entry.Id == id)?.DestinationId,
                        TemplateModuleState.KnownUnavailable));
                    break;

                case ModuleResolve.Unknown(var persistedModuleId):
                    presentations.Add(new TemplateModulePresentation(
                        persistedModuleId,
                        persistedModuleId,
                        null,
                        TemplateModuleState.Unknown));
                    break;
            }
        }

        return presentations;
    }

    private bool ValidatorIsLandingRepresented(
        string? landingDestinationId,
        IReadOnlyList<TemplateModule> modules) =>
        TemplateAdministrationValidator.IsLandingRepresented(
            landingDestinationId,
            modules.Select(module => module.ModuleId).ToArray(),
            _registry);

    private static IReadOnlyList<TemplateModule> DenseModules(
        Guid templateId,
        IReadOnlyList<string> moduleIds) =>
        moduleIds
            .Select((moduleId, index) => new TemplateModule(templateId, moduleId, index + 1))
            .ToArray();

    private static string? NormalizeLanding(string? landingDestinationId) =>
        string.IsNullOrWhiteSpace(landingDestinationId) ? null : landingDestinationId.Trim();

    private static TemplateAdministrationResult.Conflict StaleTemplateConflict(
        Guid templateId,
        int expectedVersion,
        int currentVersion) =>
        new(
            TemplateConflictReason.StaleTemplateVersion,
            $"Template '{templateId}' was modified concurrently (expected version {expectedVersion}, " +
            $"current version {currentVersion}); reload and retry.");
}