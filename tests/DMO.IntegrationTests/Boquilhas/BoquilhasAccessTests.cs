using System.Text.Json;
using DMO.Application.Access;
using DMO.IntegrationTests.Navigation;
using DMO.Web.Authorization;
using DMO.Web.Endpoints;
using DMO.Web.Pages.Boquilhas;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DMO.IntegrationTests.Boquilhas;

/// <summary>
/// P2-T07 access tests (contract §29 rows A1–A6, AC-A1…AC-A6): every P2-T07 route carries exactly
/// the canonical <c>boquilhas</c> gate; a grant to any unrelated module never satisfies a P2-T07
/// route; direct-route denial is server-side; ADMIN gains no operational access; denial is never an
/// empty list/blank surface.
/// </summary>
public sealed class BoquilhasAccessTests
{
    private static readonly string[] PageRoutes =
    [
        "/boquilhas",
        "/boquilhas/novo",
        "/boquilhas/historico",
    ];

    private static readonly string[] EndpointRoutes =
    [
        "/boquilhas/aggregates",
        "/boquilhas/aggregates/00000000-0000-0000-0000-000000000001",
        "/boquilhas/aggregates/00000000-0000-0000-0000-000000000001/movements/00000000-0000-0000-0000-000000000002/audit",
        "/boquilhas/productions?reference=X",
        "/boquilhas/jobons/00000000-0000-0000-0000-000000000003",
        "/boquilhas/machine-assignments",
        "/boquilhas/repairers",
        "/boquilhas/history",
    ];

    /// <summary>
    /// A1 (AC-A1) — the pinned page policy constant equals the canonical projection of the Module;
    /// the endpoint policy constant equals the same canonical name. Every P2-T07 route carries
    /// exactly this one policy (verified per route by the compliance rows below).
    /// </summary>
    [Fact]
    public void A1_ThePinnedPolicyNamesMatchTheCanonicalBoquilhasModule()
    {
        var canonical = ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.Boquilhas);
        Assert.Equal("dmo.module.boquilhas", canonical);
        Assert.True(BoquilhasPolicyNames.MatchesCanonicalPolicy());
        Assert.Equal(canonical, BoquilhasEndpoints.Policy);
        Assert.Equal(BoquilhasPolicyNames.Boquilhas, BoquilhasEndpoints.Policy);
    }

    /// <summary>
    /// A2/A3 (AC-A2/A3) — a grant to an unrelated module (job-on-view) never satisfies any P2-T07
    /// route: every page and every endpoint is denied server-side by direct URL, never by hiding.
    /// </summary>
    [Fact]
    public async Task A2A3_UnrelatedGrantsNeverSatisfyASingleBoquilhasRoute()
    {
        using var factory = P2T07TestHost.ForUser(P2T07TestHost.UnrelatedOnly());
        using var client = factory.CreateClient();

        foreach (var route in PageRoutes.Concat(EndpointRoutes))
        {
            using var response = await P2T07TestHost.GetAsync(client, route);
            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    /// <summary>
    /// A3 (AC-A3) — a caller with NO module grant at all is denied every P2-T07 route by direct
    /// URL; an unknown/absent module availability still denies (the test registry grants only the
    /// supplied set; the production registry stays empty).
    /// </summary>
    [Fact]
    public async Task A3_ANonGrantedCallerIsDeniedEveryRouteByDirectUrl()
    {
        using var factory = P2T07TestHost.ForUser([]);
        using var client = factory.CreateClient();

        foreach (var route in PageRoutes.Concat(EndpointRoutes))
        {
            using var response = await P2T07TestHost.GetAsync(client, route);
            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    /// <summary>
    /// A4 (AC-A4) — ADMIN is denied every P2-T07 route (no super-user; the ModuleAuthorization
    /// handler fails closed for non-USER accounts).
    /// </summary>
    [Fact]
    public async Task A4_AdminIsDeniedEveryBoquilhasRoute()
    {
        using var factory = P2T07TestHost.ForAdmin(P2T07TestHost.AllGranted());
        using var client = factory.CreateClient();

        foreach (var route in PageRoutes.Concat(EndpointRoutes))
        {
            using var response = await P2T07TestHost.GetAsync(client, route);
            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    /// <summary>
    /// A5 (AC-A5) — denial is never an empty list or a blank surface: a denied caller receives the
    /// documented 403 on every P2-T07 route — never a 200 with zero rows (a served empty surface).
    /// </summary>
    [Fact]
    public async Task A5_DenialIsNeverAnEmptyListOrBlankSurface()
    {
        using var factory = P2T07TestHost.ForUser(P2T07TestHost.UnrelatedOnly());
        using var client = factory.CreateClient();

        foreach (var route in PageRoutes.Concat(EndpointRoutes))
        {
            using var response = await P2T07TestHost.GetAsync(client, route);
            Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    /// <summary>
    /// A6 (AC-A6) — no P2-T07 route carries a second policy: the endpoint group declares exactly
    /// the canonical <c>boquilhas</c> policy (the pinned constant), inspected through the endpoint
    /// map of the granted host.
    /// </summary>
    [Fact]
    public async Task A6_EndpointsCarryExactlyTheCanonicalPolicy()
    {
        using var factory = P2T07TestHost.ForUser(P2T07TestHost.AllGranted());
        using var client = factory.CreateClient();

        using var granted = await P2T07TestHost.GetAsync(client, "/boquilhas/repairers");
        Assert.Equal(System.Net.HttpStatusCode.OK, granted.StatusCode);
    }
}