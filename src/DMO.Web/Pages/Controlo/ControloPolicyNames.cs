using DMO.Application.Access;
using DMO.Web.Authorization;

namespace DMO.Web.Pages.Controlo;

/// <summary>
/// Compile-time policy names of the P2-T05 surfaces (Create + Definições alike).
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §21.2/§22. A Razor page's <c>[Authorize(Policy = …)]</c> argument
/// must be a compile-time constant, so the canonical name — which
/// <see cref="ModuleAuthorizationPolicies.PolicyName"/> derives at runtime from
/// <see cref="ModuleCatalog"/> — is pinned here instead of being spelled out per page.
/// <para>
/// Every P2-T05 route/action carries EXACTLY this one policy, including every Definições
/// route/action (§21.2: "one capability owns settings"). No policy is created and no policy is
/// modified here; the equality of the constant with the canonical projection is asserted by test
/// (AUT5), so the two can never drift apart silently.</para>
/// </remarks>
public static class ControloPolicyNames
{
    /// <summary>Canonical policy of <see cref="ModuleCatalog.ControloCreate"/>, shared by every
    /// Create route/action and every Definições route/action.</summary>
    public const string ControloCreate = "dmo.module.controlo-create";

    /// <summary>
    /// Whether the pinned name equals the canonical generated policy of the Module, so the pinned
    /// constant is provably the canonical projection and not an independent permission catalogue.
    /// </summary>
    public static bool MatchesCanonicalPolicy() =>
        ControloCreate == ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.ControloCreate);
}