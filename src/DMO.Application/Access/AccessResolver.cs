using DMO.Application.Accounts;
using DMO.Application.Repositories;
using DMO.Application.Templates;

namespace DMO.Application.Access;

/// <summary>
/// Fail-closed implementation of <see cref="IAccessResolver"/> over the canonical registry and
/// the accepted P1-T03 repository contracts.
/// </summary>
/// <remarks>
/// <para>
/// Resolution flow (exact):
/// </para>
/// <code>
/// AccountResolution
///   Admin | NoAccess            → Denied(NotOperationalUser)
///   User(user):
///     user.TemplateId is null   → Denied(NoTemplate)
///     template = ITemplateRepository.GetByIdAsync(templateId)   // exception → Denied(ResolutionFailure)
///     template is null          → Denied(TemplateMissing)
///     composition = ITemplateModuleRepository.GetByTemplateAsync(templateId)  // exception → Denied(ResolutionFailure)
///     per persisted module_id (persisted PresentationOrder):
///       Unknown          → Denied(UnknownModule)       // entire resolution denied; STOP
///       KnownUnavailable → Denied(UnavailableModule)   // entire resolution denied; STOP
///       Available(def)   → effective.Add(def)          // persisted order preserved
///     → Granted(effective)  // ONLY when every entry is valid AND available
/// </code>
/// <para>
/// No repository besides the two P1-T03 contracts is used; no new query is introduced. The
/// active state of the USER was already established by the accepted account boundary
/// (P1-T02); this resolver does not re-evaluate it.
/// </para>
/// </remarks>
public sealed class AccessResolver : IAccessResolver
{
    private readonly IModuleRegistry _registry;
    private readonly ITemplateRepository _templates;
    private readonly ITemplateModuleRepository _templateModules;

    /// <summary>Creates the resolver over the registry and the P1-T03 repository contracts.</summary>
    public AccessResolver(
        IModuleRegistry registry,
        ITemplateRepository templates,
        ITemplateModuleRepository templateModules)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(templates);
        ArgumentNullException.ThrowIfNull(templateModules);
        _registry = registry;
        _templates = templates;
        _templateModules = templateModules;
    }

    /// <inheritdoc />
    public async Task<AccessOutcome> ResolveAccessAsync(
        AccountResolution resolution,
        CancellationToken cancellationToken)
    {
        if (resolution is not AccountResolution.User(var user))
        {
            // ADMIN (and NoAccess states) presented to operational resolution: deny. ADMIN is
            // a separate account type with no operational Template and no implicit super-user
            // bypass (ACCESS_MODEL §12 / ADMIN contract §10).
            return new AccessOutcome.Denied(AccessDenialReason.NotOperationalUser);
        }

        if (user.TemplateId is not Guid templateId)
        {
            return new AccessOutcome.Denied(AccessDenialReason.NoTemplate);
        }

        // Load the Template (missing row → TemplateMissing; infrastructure failure →
        // ResolutionFailure — never leaks as a grant).
        Template? template;
        try
        {
            template = await _templates.GetByIdAsync(templateId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return new AccessOutcome.Denied(AccessDenialReason.ResolutionFailure);
        }

        if (template is null)
        {
            return new AccessOutcome.Denied(AccessDenialReason.TemplateMissing);
        }

        // Load the persisted composition in presentation order (P1-T03 contract).
        IReadOnlyList<TemplateModule> composition;
        try
        {
            composition = await _templateModules.GetByTemplateAsync(templateId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return new AccessOutcome.Denied(AccessDenialReason.ResolutionFailure);
        }

        var effective = new List<ModuleDefinition>(composition.Count);
        foreach (var entry in composition)
        {
            switch (_registry.Resolve(entry.ModuleId))
            {
                // Fail-closed, whole-resolution denial: an unknown or known-but-unavailable
                // persisted Module denies the ENTIRE resolution. No partial grants, no
                // silently skipped entries (request §7 / corrected plan §7–8).
                case ModuleResolve.Unknown _:
                    return new AccessOutcome.Denied(AccessDenialReason.UnknownModule);

                case ModuleResolve.KnownUnavailable _:
                    return new AccessOutcome.Denied(AccessDenialReason.UnavailableModule);

                // Duplicates are structurally impossible (template_modules PK is
                // (template_id, module_id)); a corrupt state iterates each id once.
                case ModuleResolve.Available(var definition):
                    effective.Add(definition);
                    break;
            }
        }

        // Full Template composition interpreted and valid → Granted (persisted order).
        return new AccessOutcome.Granted(effective);
    }
}