using System.Net;
using DMO.Application.Access;
using DMO.IntegrationTests.Host;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DMO.IntegrationTests.Navigation;

/// <summary>
/// P1-T07 no-access surface. Test:
/// Purpose: prove the exact /AccessDenied account behavior (None → /Login; ADMIN →
/// /Administration; USER → HTTP 403 with generic no-access content inside the shared A2
/// shell) and that no diagnostic fact leaks into the rendered page.
/// Master behavior being verified: ACCESS_MODEL §13 (fail-closed list) and request P1-T07
/// §16 (§31.1 settled fail-closed mapping); the page is never the source of the denial
/// reason — reasons remain server-side facts.
/// Preconditions: controlled test composition; USER states include denied resolution and
/// normal-empty grant.
/// Action: GET /AccessDenied (no redirect following where noted).
/// Assertions: exact status code and rendered generic content; absence of denial enums,
/// Module ids, Template value, provider/identity facts.
/// Required non-effects: no reason leakage, no module/template/identity disclosure, no
/// fake destinations, logout form preserved.
/// What this proves: the accepted no-access contract and non-disclosure.
/// What this does NOT prove: which server-side reason occurred (never rendered) or
/// authorization enforcement (P1-T04 tests).
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class NoAccessPageTests
{
    /// <summary>The only generic USER no-access message rendered on the page.</summary>
    private const string GenericNoAccess = "Sem acesso operacional configurado";

    private static readonly string[] ConfidentialFacts =
    [
        "NotOperationalUser",
        "NoTemplate",
        "TemplateMissing",
        "UnknownModule",
        "UnavailableModule",
        "ResolutionFailure",
        "job-on",
        "controlo",
        "maria@example.test",
        "provider",
        "subject-",
    ];

    [Fact]
    public async Task AccessDenied_NoSession_RedirectsToLogin()
    {
        using var factory = TestNavigationComposition.ConfigureFactory(
            new DmoWebApplicationFactory(),
            new DMO.Application.Session.CurrentAccount.None(),
            [],
            TestNavigationComposition.Granted(),
            new Dictionary<string, string>());
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/AccessDenied");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task AccessDenied_Admin_RedirectsToAdministration()
    {
        using var factory = TestNavigationComposition.ConfigureFactory(
            new DmoWebApplicationFactory(),
            TestNavigationComposition.Admin(),
            [],
            TestNavigationComposition.Granted(),
            new Dictionary<string, string>());
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/AccessDenied");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Administration", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task AccessDenied_User_DeniedResolution_Renders403WithGenericContentOnly()
    {
        using var factory = TestNavigationComposition.ConfigureFactory(
            new DmoWebApplicationFactory(),
            TestNavigationComposition.User(),
            [],
            new AccessOutcome.Denied(AccessDenialReason.NoTemplate),
            new Dictionary<string, string>());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/AccessDenied");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(GenericNoAccess, html, StringComparison.Ordinal);
        // The page carries the generic no-access section but none of the server-side facts.
        Assert.DoesNotContain("NoTemplate", html, StringComparison.Ordinal);
        Assert.All(ConfidentialFacts, fact =>
            Assert.DoesNotContain(fact, html, StringComparison.Ordinal));
    }

    [Fact]
    public async Task AccessDenied_User_NoTemplateState_IsIndistinguishableOnThePage()
    {
        // NoTemplate denial and any other denial render identical generic content: the page
        // is not the source of the reason.
        using var factory = TestNavigationComposition.ConfigureFactory(
            new DmoWebApplicationFactory(),
            TestNavigationComposition.User(),
            [],
            new AccessOutcome.Denied(AccessDenialReason.TemplateMissing),
            new Dictionary<string, string>());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/AccessDenied");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(GenericNoAccess, html, StringComparison.Ordinal);
        Assert.DoesNotContain("TemplateMissing", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccessDenied_User_GrantedEmpty_FailClosedStateIsHonestAndRendersTheSharedShell()
    {
        using var factory = TestNavigationComposition.ConfigureFactory(
            new DmoWebApplicationFactory(),
            TestNavigationComposition.User(),
            [],
            TestNavigationComposition.Granted(),
            new Dictionary<string, string>());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/AccessDenied");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(GenericNoAccess, html, StringComparison.Ordinal);
        // The accepted A2 shell renders honestly: identity, empty navigation, no
        // fail-closed status for a normal empty grant.
        Assert.Contains("Maria Operadora", html, StringComparison.Ordinal);
        Assert.Contains("Sem destinos operacionais disponíveis", html, StringComparison.Ordinal);
        Assert.DoesNotContain("A navegação operacional está indisponível", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccessDenied_User_LogoutFormUsesTheExistingLogoutEndpoint()
    {
        using var factory = TestNavigationComposition.ConfigureFactory(
            new DmoWebApplicationFactory(),
            TestNavigationComposition.User(),
            [],
            new AccessOutcome.Denied(AccessDenialReason.NoTemplate),
            new Dictionary<string, string>());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/AccessDenied");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        // The page reuses the logged-in logout form pattern (POST /auth/logout, unchanged
        // endpoint; endpoint behavior itself is covered by the existing auth tests).
        Assert.Contains("form method=\"post\" action=\"/auth/logout\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccessDenied_User_NavigationProjectsHonestlyWithoutDisclosingReasons()
    {
        // Even with a granted (but inaccessible) landing state, the no-access page exposes
        // no navigation/landing facts.
        var jobOnView = TestNavigationComposition.Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        var jobOnCreate = TestNavigationComposition.Definition(ModuleCatalog.JobOnCreate, "Job On Create", "job-on", "Job On");
        using var factory = TestNavigationComposition.ConfigureFactory(
            new DmoWebApplicationFactory(),
            TestNavigationComposition.User(),
            [jobOnView, jobOnCreate],
            TestNavigationComposition.GrantedWithLanding("job-on", jobOnView, jobOnCreate),
            new Dictionary<string, string> { ["job-on"] = "/implemented/job-on" });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/AccessDenied");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(GenericNoAccess, html, StringComparison.Ordinal);
        // The shared shell projects the real navigation for this USER, but the no-access
        // page itself discloses no reason and no landing fact.
        Assert.Contains("href=\"/implemented/job-on\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidExplicitLanding", html, StringComparison.Ordinal);
        Assert.DoesNotContain("LandingDestinationId", html, StringComparison.Ordinal);
    }
}