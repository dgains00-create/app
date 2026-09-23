using DMO.Application.Access;
using DMO.Web.Authorization;

namespace DMO.Web.Pages.JobOn;

/// <summary>
/// Compile-time policy names of the P2-T04 surfaces.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §13.2 and §13.7. A Razor page's <c>[Authorize(Policy = …)]</c> argument
/// must be a compile-time constant, so the canonical names — which
/// <see cref="ModuleAuthorizationPolicies.PolicyName"/> derives at runtime from
/// <see cref="ModuleCatalog"/> — are pinned here instead of being spelled out per page.
/// <para>
/// No policy is created here and no policy is modified:
/// <c>ModuleAuthorizationPolicies</c> and <c>ModuleCatalog</c> stay untouched, and the equality of
/// every constant with <see cref="ModuleAuthorizationPolicies.PolicyName"/> of its canonical Module is
/// asserted by test, so the two can never drift apart silently.
/// </para>
/// </remarks>
public static class JobOnPolicyNames
{
    /// <summary>Canonical policy of <see cref="ModuleCatalog.JobOnView"/>.</summary>
    public const string JobOnView = "dmo.module.job-on-view";

    /// <summary>Canonical policy of <see cref="ModuleCatalog.JobOnCreate"/>.</summary>
    public const string JobOnCreate = "dmo.module.job-on-create";

    /// <summary>Canonical policy of <see cref="ModuleCatalog.Ferramentas"/>.</summary>
    public const string Ferramentas = "dmo.module.ferramentas";

    /// <summary>
    /// Whether every pinned name equals the canonical generated policy of its Module, so the pinned
    /// constants are provably the canonical projection and not an independent permission catalogue.
    /// </summary>
    public static bool MatchesCanonicalPolicies() =>
        JobOnView == ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.JobOnView) &&
        JobOnCreate == ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.JobOnCreate) &&
        Ferramentas == ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.Ferramentas);
}
