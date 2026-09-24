using DMO.Application.Access;
using DMO.Web.Authorization;

namespace DMO.Web.Pages.Boquilhas;

/// <summary>
/// Compile-time policy names of the P2-T07 surfaces (Registo + Novo + Histórico).
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §14. A Razor page's <c>[Authorize(Policy = …)]</c> argument must be
/// a compile-time constant, so the canonical name — which
/// <see cref="ModuleAuthorizationPolicies.PolicyName"/> derives at runtime from
/// <see cref="ModuleCatalog"/> — is pinned here instead of being spelled out per page.
/// <para>
/// Every P2-T07 route/action carries EXACTLY this one policy (the accepted
/// <c>JobOnPolicyNames</c>/<c>ControloPolicyNames</c> pattern); the equality of the constant with
/// the canonical projection is asserted by test (A1), so the two can never drift apart silently.
/// Boquilhas is one assignable module with no sibling and no shared destination: no other grant
/// (`job-on-*`, `controlo-*`, `ferramentas`, …) ever satisfies a P2-T07 route and ADMIN gains no
/// operational access.</para>
/// </remarks>
public static class BoquilhasPolicyNames
{
    /// <summary>Canonical policy of <see cref="ModuleCatalog.Boquilhas"/>, shared by every P2-T07
    /// page and endpoint.</summary>
    public const string Boquilhas = "dmo.module.boquilhas";

    /// <summary>
    /// Whether the pinned name equals the canonical generated policy of the Module, so the pinned
    /// constant is provably the canonical projection and not an independent permission catalogue.
    /// </summary>
    public static bool MatchesCanonicalPolicy() =>
        Boquilhas == ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.Boquilhas);
}