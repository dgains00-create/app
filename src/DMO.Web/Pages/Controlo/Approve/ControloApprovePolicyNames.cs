using DMO.Application.Access;
using DMO.Web.Authorization;

namespace DMO.Web.Pages.Controlo.Approve;

/// <summary>
/// Compile-time policy names of the P2-T06 surfaces (Aprovar + Histórico de Pesos).
/// </summary>
/// <remarks>
/// Authority: P2-T06 contract §14. A Razor page's <c>[Authorize(Policy = …)]</c> argument must be
/// a compile-time constant, so the canonical name — which
/// <see cref="ModuleAuthorizationPolicies.PolicyName"/> derives at runtime from
/// <see cref="ModuleCatalog"/> — is pinned here instead of being spelled out per page.
/// <para>
/// Every P2-T06 route/action carries EXACTLY this one policy (the accepted
/// <c>ControloPolicyNames</c> pattern); the equality of the constant with the canonical
/// projection is asserted by test (A5), so the two can never drift apart silently. Create and
/// Approve remain independent grants: <c>controlo-create</c> grants no P2-T06 route and
/// <c>controlo-approve</c> grants no P2-T05 route (incl. every Definições route/action).</para>
/// </remarks>
public static class ControloApprovePolicyNames
{
    /// <summary>Canonical policy of <see cref="ModuleCatalog.ControloApprove"/>, shared by every
    /// P2-T06 page and endpoint.</summary>
    public const string ControloApprove = "dmo.module.controlo-approve";

    /// <summary>
    /// Whether the pinned name equals the canonical generated policy of the Module, so the pinned
    /// constant is provably the canonical projection and not an independent permission catalogue.
    /// </summary>
    public static bool MatchesCanonicalPolicy() =>
        ControloApprove == ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.ControloApprove);
}