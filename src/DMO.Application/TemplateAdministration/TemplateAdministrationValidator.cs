using DMO.Application.Access;

namespace DMO.Application.TemplateAdministration;

/// <summary>
/// Server-side validation of the Template administration commands (P1-T06).
/// </summary>
/// <remarks>
/// <para>
/// Validation is deliberately a pure static surface: no EF, no HTTP, no provider call. The
/// Module Registry is the availability authority (<see cref="IModuleRegistry"/>); the
/// database constraints and the persisted relation remain backstops.
/// </para>
/// <para>
/// Composition rules (accepted plan §8): only canonical <see cref="ModuleId"/> values are
/// persisted (display names are never identity); duplicates are rejected; an unknown id fails
/// closed; a known-but-unavailable id can never be <b>newly</b> selected. Unavailable/unknown
/// ids that are <b>already persisted</b> are surfaced in the ficha as locked rows and may be
/// preserved or explicitly removed — the system never silently repairs persisted state.
/// </para>
/// <para>
/// Landing rule (accepted plan §10): <c>null</c> is valid; a non-null landing must be
/// represented by at least one <b>selected, currently available, non-contextual</b> Module
/// destination in the final composition. An edit that removes the last Module supporting the
/// landing is rejected unless the Admin explicitly changes/removes the landing — no silent
/// clearing, no auto-fix of invalid persisted landing.
/// </para>
/// <para>
/// Optimistic-concurrency carriers (accepted plan §17, <c>ExpectedVersion</c> válido
/// <c>&gt;0</c>): <c>UpdateTemplateCommand.ExpectedVersion</c>,
/// <c>DeleteTemplateCommand.ExpectedVersion</c> and
/// <c>SetTemplateUserCommand.UserExpectedVersion</c> must all be positive. A malformed
/// (<c>&lt;= 0</c>) carrier is rejected as <c>ValidationFailed</c> before any write — it is
/// never reinterpreted as a stale version and never reaches the repository.
/// </para>
/// </remarks>
public static class TemplateAdministrationValidator
{
    /// <summary>Reasonable bound for the free-text Template name (no uniqueness rule is invented).</summary>
    public const int MaxNameLength = 200;

    /// <summary>Validates a create command against the current-build registry.</summary>
    public static IReadOnlyList<string> Validate(
        TemplateAdministrationCommands.CreateTemplateCommand command,
        IModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(registry);

        var errors = new List<string>();
        ValidateName(errors, command.Name);
        ValidateComposition(errors, command.ModuleIds, persistedModuleIds: null, registry);
        ValidateLanding(errors, command.LandingDestinationId, command.ModuleIds, registry);
        return errors;
    }

    /// <summary>
    /// Validates an update command against the current-build registry and the persisted
    /// composition ids (so unavailable/unknown persisted entries can be preserved or
    /// explicitly removed, but never newly selected).
    /// </summary>
    public static IReadOnlyList<string> Validate(
        TemplateAdministrationCommands.UpdateTemplateCommand command,
        IReadOnlySet<string> persistedModuleIds,
        IModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(persistedModuleIds);
        ArgumentNullException.ThrowIfNull(registry);

        var errors = new List<string>();
        ValidateVersion(errors, command.ExpectedVersion, "ExpectedVersion");
        ValidateName(errors, command.Name);
        ValidateComposition(errors, command.ModuleIds, persistedModuleIds, registry);
        ValidateLanding(errors, command.LandingDestinationId, command.ModuleIds, registry);
        return errors;
    }

    /// <summary>
    /// Validates a delete command: the optimistic-concurrency carrier must be a positive
    /// version (accepted plan §17: <c>ExpectedVersion</c> válido <c>&gt;0</c>). Existence and
    /// the authoritative version comparison remain repository/transaction responsibilities.
    /// </summary>
    public static IReadOnlyList<string> Validate(TemplateAdministrationCommands.DeleteTemplateCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();
        ValidateVersion(errors, command.ExpectedVersion, "ExpectedVersion");
        return errors;
    }

    /// <summary>
    /// Validates a USER association command: the optimistic-concurrency carrier must be a
    /// positive version. Existence and version comparison remain repository responsibilities.
    /// </summary>
    public static IReadOnlyList<string> Validate(TemplateAdministrationCommands.SetTemplateUserCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();
        ValidateVersion(errors, command.UserExpectedVersion, "UserExpectedVersion");
        return errors;
    }

    /// <summary>
    /// Returns the landing-representation check: <c>null</c> landing is valid; a non-null
    /// landing must equal the destination of at least one selected available non-contextual
    /// Module in the final composition.
    /// </summary>
    public static bool IsLandingRepresented(
        string? landingDestinationId,
        IReadOnlyList<string> moduleIds,
        IModuleRegistry registry)
    {
        if (landingDestinationId is null)
        {
            return true;
        }

        foreach (var moduleId in moduleIds)
        {
            var definition = FindAvailableDefinition(moduleId, registry);
            if (definition is null)
            {
                continue;
            }

            // Non-contextual: a contextual-only Module (ferramentas / ferramentas-approve)
            // has no top-level destination and can never be a landing.
            if (!definition.Surface.IsContextualOnly
                && string.Equals(definition.DestinationId, landingDestinationId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidateName(List<string> errors, string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLength)
        {
            errors.Add($"Name must be non-blank and at most {MaxNameLength} characters.");
        }
    }

    /// <summary>
    /// The optimistic-concurrency carrier must be a real version: persisted versions start at
    /// 1 and only increase. A non-positive value is malformed input, not a stale version, so it
    /// fails closed as validation (never surfaces as a conflict and never reaches a write).
    /// </summary>
    private static void ValidateVersion(List<string> errors, int version, string fieldName)
    {
        if (version <= 0)
        {
            errors.Add(
                $"{fieldName} must be a positive version (received {version}); reload the form and retry.");
        }
    }

    private static void ValidateComposition(
        List<string> errors,
        IReadOnlyList<string> moduleIds,
        IReadOnlySet<string>? persistedModuleIds,
        IModuleRegistry registry)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var moduleId in moduleIds)
        {
            if (!seen.Add(moduleId))
            {
                errors.Add($"Module '{moduleId}' is selected more than once; duplicate selection is invalid.");
                continue;
            }

            // A newly submitted id is a selection intent: it must be canonical and
            // available. Persisted unavailable/unknown entries are allowed to stay (the
            // ficha surfaces them as locked rows) or to be explicitly removed.
            if (persistedModuleIds?.Contains(moduleId) == true)
            {
                continue;
            }

            if (registry.Resolve(moduleId) is not ModuleResolve.Available)
            {
                errors.Add(
                    $"Module '{moduleId}' is not available in this build and cannot be newly " +
                    "selected. Persisted unavailable/invalid modules must be removed explicitly.");
            }
        }
    }

    private static void ValidateLanding(
        List<string> errors,
        string? landingDestinationId,
        IReadOnlyList<string> moduleIds,
        IModuleRegistry registry)
    {
        if (landingDestinationId is null)
        {
            return;
        }

        if (!IsLandingRepresented(landingDestinationId, moduleIds, registry))
        {
            errors.Add(
                $"Landing destination '{landingDestinationId}' is not represented by any selected, " +
                "currently available Module destination. Select a Module with that destination or " +
                "clear the landing explicitly.");
        }
    }

    private static ModuleDefinition? FindAvailableDefinition(string moduleId, IModuleRegistry registry) =>
        registry.Resolve(moduleId) is ModuleResolve.Available(var definition) ? definition : null;
}