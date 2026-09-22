using System.Net;
using DMO.Application.Access;
using DMO.IntegrationTests.Host;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DMO.IntegrationTests.Navigation;

/// <summary>
/// P1-T07 account-aware root routing. Test:
/// Purpose: prove the exact GET / dispatch contract (unauthenticated → /Login; ADMIN →
/// /Administration; USER → landing route or /AccessDenied) with one navigation projection.
/// Master behavior being verified: ACCESS_MODEL §12/§13 (navigation projection, landing
/// validity, fail closed) and request P1-T07 §13 (root routing); corrected plan §15.
/// Preconditions: controlled test composition (fake current account, fixed access outcome,
/// controlled registry + route registry) exactly like the A2 shell tests.
/// Action: GET / without following redirects.
/// Assertions: exact 302 target per account state.
/// Required non-effects: no session creation, no access resolution for ADMIN/none, no
/// rendering at /, no role strings / Template names / provider claims in the decision.
/// What this proves: the accepted root contract, fail-closed entries included.
/// What this does NOT prove: permission semantics (P1-T04) or landing selection internals
/// (unit tests) — only the routing dispatch.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class RootRoutingTests
{
    [Fact]
    public async Task Root_NoSession_RedirectsToLogin()
    {
        using var factory = TestNavigationComposition.ConfigureFactory(
            new DmoWebApplicationFactory(),
            new DMO.Application.Session.CurrentAccount.None(),
            [],
            TestNavigationComposition.Granted(),
            new Dictionary<string, string>());
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Root_InactiveOrUnresolvedSessionState_FailsClosedToLogin()
    {
        // The session boundary resolves inactive/unresolved identities to CurrentAccount.None
        // (P1-T02 accepted behavior); the root sees exactly the same state as no session.
        using var factory = TestNavigationComposition.ConfigureFactory(
            new DmoWebApplicationFactory(),
            new DMO.Application.Session.CurrentAccount.None(),
            [],
            TestNavigationComposition.Granted(),
            new Dictionary<string, string>());
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Root_Admin_RedirectsToAdministration()
    {
        // A route and available definitions exist, so an ADMIN that wrongly became
        // operational would visibly redirect into an operational route.
        var jobOn = TestNavigationComposition.Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        using var factory = TestNavigationComposition.ConfigureFactory(
            new DmoWebApplicationFactory(),
            TestNavigationComposition.Admin(),
            [jobOn],
            TestNavigationComposition.GrantedWithLanding("job-on", jobOn),
            new Dictionary<string, string> { ["job-on"] = "/implemented/job-on" });
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Administration", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Root_Admin_DoesNotInvokeUserAccessResolution()
    {
        var access = new TestNavigationComposition.FixedAccessService(
            TestNavigationComposition.Granted(), failsWhenInvoked: true);
        using var baseFactory = new DmoWebApplicationFactory();
        using var factory = TestNavigationComposition.ConfigureFactory(
            baseFactory,
            TestNavigationComposition.Admin(),
            [],
            TestNavigationComposition.Granted(),
            new Dictionary<string, string>(),
            additional: services =>
            {
                services.RemoveAll<IModuleAccessService>();
                services.AddSingleton<IModuleAccessService>(access);
            });
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Administration", response.Headers.Location?.ToString());
        Assert.Equal(0, access.ResolveCallCount);
    }

    [Fact]
    public async Task Root_User_ExplicitValidLanding_RedirectsToTheLandingRoute()
    {
        var controlo = TestNavigationComposition.Definition(ModuleCatalog.ControloCreate, "Controlo Create", "controlo", "Controlo");
        var jobOn = TestNavigationComposition.Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        using var factory = TestNavigationComposition.ConfigureFactory(
            new DmoWebApplicationFactory(),
            TestNavigationComposition.User(),
            [controlo, jobOn],
            TestNavigationComposition.GrantedWithLanding("job-on", controlo, jobOn),
            new Dictionary<string, string>
            {
                ["controlo"] = "/implemented/controlo",
                ["job-on"] = "/implemented/job-on",
            });
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/implemented/job-on", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Root_User_NullLanding_RedirectsToFirstDestinationInTemplateOrder()
    {
        // Template presentation order is controlo first, job-on second; the null landing
        // selects the first valid destination.
        var controlo = TestNavigationComposition.Definition(ModuleCatalog.ControloCreate, "Controlo Create", "controlo", "Controlo");
        var jobOn = TestNavigationComposition.Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        using var factory = TestNavigationComposition.ConfigureFactory(
            new DmoWebApplicationFactory(),
            TestNavigationComposition.User(),
            [controlo, jobOn],
            TestNavigationComposition.GrantedWithLanding(null, controlo, jobOn),
            new Dictionary<string, string>
            {
                ["controlo"] = "/implemented/controlo",
                ["job-on"] = "/implemented/job-on",
            });
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/implemented/controlo", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Root_User_SharedDestinationLanding_RedirectsToTheCollapsedRoute()
    {
        var jobOnView = TestNavigationComposition.Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        var jobOnCreate = TestNavigationComposition.Definition(ModuleCatalog.JobOnCreate, "Job On Create", "job-on", "Job On");
        using var factory = TestNavigationComposition.ConfigureFactory(
            new DmoWebApplicationFactory(),
            TestNavigationComposition.User(),
            [jobOnView, jobOnCreate],
            TestNavigationComposition.GrantedWithLanding("job-on", jobOnView, jobOnCreate),
            new Dictionary<string, string> { ["job-on"] = "/implemented/job-on" });
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/implemented/job-on", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Root_User_GrantedWithoutRoutableDestinations_RedirectsToAccessDenied()
    {
        using var factory = TestNavigationComposition.ConfigureFactory(
            new DmoWebApplicationFactory(),
            TestNavigationComposition.User(),
            [],
            TestNavigationComposition.Granted(),
            new Dictionary<string, string>());
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/AccessDenied", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Root_User_ContextualOnlyComposition_RedirectsToAccessDenied()
    {
        // Ferramentas is contextual-only: zero top-level destinations, zero landing.
        var ferramentas = TestNavigationComposition.Definition(ModuleCatalog.Ferramentas, "Ferramentas", null, "Ferramentas", contextual: true);
        using var factory = TestNavigationComposition.ConfigureFactory(
            new DmoWebApplicationFactory(),
            TestNavigationComposition.User(),
            [ferramentas],
            TestNavigationComposition.Granted(ferramentas),
            new Dictionary<string, string> { ["ferramentas"] = "/implemented/ferramentas" });
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/AccessDenied", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Root_User_DeniedResolution_RedirectsToAccessDenied_ForEveryDenialReason()
    {
        var reasons = new[]
        {
            AccessDenialReason.NotOperationalUser,
            AccessDenialReason.NoTemplate,
            AccessDenialReason.TemplateMissing,
            AccessDenialReason.UnknownModule,
            AccessDenialReason.UnavailableModule,
            AccessDenialReason.ResolutionFailure,
        };

        foreach (var reason in reasons)
        {
            using var baseFactory = new DmoWebApplicationFactory();
            using var factory = TestNavigationComposition.ConfigureFactory(
                baseFactory,
                TestNavigationComposition.User(),
                [],
                new AccessOutcome.Denied(reason),
                new Dictionary<string, string>());
            using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/AccessDenied", response.Headers.Location?.ToString());
        }
    }

    [Fact]
    public async Task Root_User_InvalidExplicitLanding_FailsClosedToAccessDenied()
    {
        // Architect decision (§31.1 settled): an explicit persisted landing not present in
        // the valid composed/routed destinations → InvalidExplicitLanding → NoAccess. No
        // fallback to the first valid destination.
        var controlo = TestNavigationComposition.Definition(ModuleCatalog.ControloCreate, "Controlo Create", "controlo", "Controlo");
        using var factory = TestNavigationComposition.ConfigureFactory(
            new DmoWebApplicationFactory(),
            TestNavigationComposition.User(),
            [controlo],
            TestNavigationComposition.GrantedWithLanding("nao-existe", controlo),
            new Dictionary<string, string> { ["controlo"] = "/implemented/controlo" });
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/AccessDenied", response.Headers.Location?.ToString());
    }
}