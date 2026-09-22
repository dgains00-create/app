namespace DMO.Application.Access;

/// <summary>
/// Explicit, reviewable registration of the Modules available in the current build.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CurrentBuildAvailable"/> is the <b>only</b> source of build availability. It is
/// an explicit code list whose diff is the audit trail: future phases register a Module here
/// <b>only when its real functional surface and enforcement are implemented</b>
/// (ACCESS_MODEL §6: "A planned but not-yet-implemented module is not a usable Template
/// choice until its real surface is registered as available"; request §14).
/// </para>
/// <para>
/// Phase 1 (P1-T04) has no implemented industrial operational surface, so this list is
/// empty. No future operational Module is marked available to make tests or UI easier:
/// production registration stays honest, and any operational challenge resolves to denied
/// (nothing implemented ⇒ nothing granted).
/// </para>
/// </remarks>
public static class ModuleRegistrations
{
    /// <summary>
    /// Module definitions available in the current build. Phase 1: empty — no operational
    /// surface is implemented yet. Tests instantiate their own controlled test registries.
    /// </summary>
    public static IReadOnlyList<ModuleDefinition> CurrentBuildAvailable { get; } = [];
}