using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using DMO.Application.Access;
using DMO.Application.Session;
using DMO.IntegrationTests.Host;
using DMO.Web.Authorization;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DMO.IntegrationTests.Navigation;

/// <summary>
/// P1-T07 direct-route negative enforcement. Test:
/// Purpose: prove that navigation hiding is NOT authorization — a link absent from the
/// rendered shell never means the route is unprotected; direct protected routes deny every
/// non-granted caller through the real P1-T04 server-side Module gate.
/// Master behavior being verified: ACCESS_MODEL §12-§14 ("a hidden UI control is never the
/// security boundary"; "backend must enforce the same module decision server-side");
/// request P1-T07 §18/§26.6 (required negative cases; role-label bypass).
/// Preconditions: controlled test host with real authorization handlers + real Module
/// policies, controlled Module access outcome, and test-only protected routes registered
/// ONLY in the test composition (never in production code). A test authentication scheme
/// marks a request as "authenticated" so handler failures surface as HTTP 403 instead of a
/// 401 challenge; the scheme grants no application role.
/// Action: GET the test protected routes under each controlled caller state.
/// Assertions: exact permitted/denied status per state; navigation absence is never treated
/// as a grant.
/// Required non-effects: no production route/Module registration, no authorization-model
/// change, no fake production fixtures.
/// What this proves: direct-route denial for anonymous, ADMIN, inactive/unresolved, USER
/// without Template, denied compositions, role-label non-bypass, shared-destination
/// permission separation, and the positive control (granted Module → served).
/// What this does NOT prove: the resolver's whole-resolution semantics themselves (unit +
/// persistence tests) — this file proves the HTTP boundary enforces them.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class DirectRouteEnforcementTests
{
    private const string JobOnViewRoute = "/__test/protected/job-on-view";
    private const string JobOnCreateRoute = "/__test/protected/job-on-create";

    [Fact]
    public async Task AbsentModule_LinkAbsent_AndDirectRouteDenied()
    {
        // No granted Module at all: the shell renders zero operational links and the direct
        // protected route still denies (link absence is never authorization).
        using var factory = ProtectedHost(current: TestNavigationComposition.User(), outcome: TestNavigationComposition.Granted());
        using var client = factory.CreateClient();

        var shell = await client.GetAsync("/AccessDenied");
        var html = await shell.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Forbidden, shell.StatusCode);
        Assert.DoesNotContain("href=\"/__test/", html, StringComparison.Ordinal);
        Assert.Contains("Sem destinos operacionais disponíveis", html, StringComparison.Ordinal);

        var direct = await GetWithAuthAsync(client, JobOnViewRoute);
        Assert.Equal(HttpStatusCode.Forbidden, direct.StatusCode);
    }

    [Fact]
    public async Task NavHidden_DirectRouteDenied()
    {
        // The Module is granted but its destination route is not registered: navigation is
        // hidden (unrouted) — and the direct route requiring a different (absent) Module is
        // still denied.
        var jobOn = TestNavigationComposition.Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        using var factory = ProtectedHost(
            current: TestNavigationComposition.User(),
            outcome: TestNavigationComposition.Granted(jobOn),
            available: [jobOn],
            routes: new Dictionary<string, string>());
        using var client = factory.CreateClient();

        var shell = await client.GetAsync("/AccessDenied");
        Assert.DoesNotContain("href=\"/__test/", await shell.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var direct = await GetWithAuthAsync(client, JobOnCreateRoute);
        Assert.Equal(HttpStatusCode.Forbidden, direct.StatusCode);
    }

    [Fact]
    public async Task SharedDestinationVisibleThroughModuleA_ActionRequiringModuleB_StillDenied()
    {
        // job-on is visible through Module A (job-on-view, routed); a direct action
        // requiring Module B (job-on-create) is still denied — the shared destination never
        // merges grants.
        var jobOnView = TestNavigationComposition.Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        using var factory = ProtectedHost(
            current: TestNavigationComposition.User(),
            outcome: TestNavigationComposition.Granted(jobOnView),
            available: [jobOnView],
            routes: new Dictionary<string, string> { ["job-on"] = "/implemented/job-on" });
        using var client = factory.CreateClient();

        var shell = await client.GetAsync("/AccessDenied");
        var html = await shell.Content.ReadAsStringAsync();
        Assert.Equal(1, Count(html, "href=\"/implemented/job-on\""));
        Assert.DoesNotContain("href=\"/__test/", html, StringComparison.Ordinal);

        var direct = await GetWithAuthAsync(client, JobOnCreateRoute);
        Assert.Equal(HttpStatusCode.Forbidden, direct.StatusCode);
    }

    [Fact]
    public async Task DirectRoute_WithGrantedModule_IsServed()
    {
        // Positive control: the gate is module-specific — the same composition that denies
        // job-on-create serves job-on-view. This proves the 403s above are module denials,
        // not wiring failures.
        var jobOnView = TestNavigationComposition.Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        using var factory = ProtectedHost(
            current: TestNavigationComposition.User(),
            outcome: TestNavigationComposition.Granted(jobOnView),
            available: [jobOnView],
            routes: new Dictionary<string, string> { ["job-on"] = "/implemented/job-on" });
        using var client = factory.CreateClient();

        var response = await GetWithAuthAsync(client, JobOnViewRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("test:job-on-view", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Admin_OnOperationalModuleRoute_Denied()
    {
        // ADMIN has no operational Module grants and no super-user bypass (ACCESS_MODEL §12).
        var jobOnView = TestNavigationComposition.Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        using var factory = ProtectedHost(
            current: TestNavigationComposition.Admin(),
            outcome: TestNavigationComposition.Granted(jobOnView),
            available: [jobOnView],
            routes: new Dictionary<string, string> { ["job-on"] = "/implemented/job-on" });
        using var client = factory.CreateClient();

        var response = await GetWithAuthAsync(client, JobOnViewRoute);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousCaller_OnOperationalModuleRoute_Denied()
    {
        using var factory = ProtectedHost(current: new DMO.Application.Session.CurrentAccount.None(), outcome: TestNavigationComposition.Granted());
        using var client = factory.CreateClient();

        var response = await client.GetAsync(JobOnViewRoute);

        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InactiveOrUnresolvedAccount_Denied()
    {
        // The session boundary resolves inactive/unresolved identities to None; the HTTP
        // boundary denies the direct route for that state (authenticated mark present, so
        // the handler denial surfaces as 403).
        using var factory = ProtectedHost(current: new DMO.Application.Session.CurrentAccount.None(), outcome: TestNavigationComposition.Granted());
        using var client = factory.CreateClient();

        var response = await GetWithAuthAsync(client, JobOnViewRoute);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserWithoutTemplate_Denied()
    {
        // USER with no Template association: the access outcome is whole-resolution denial.
        using var factory = ProtectedHost(
            current: TestNavigationComposition.User(),
            outcome: new AccessOutcome.Denied(AccessDenialReason.NoTemplate));
        using var client = factory.CreateClient();

        var response = await GetWithAuthAsync(client, JobOnViewRoute);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnavailablePersistedModule_DeniedEntireResolution_AtHttpBoundary()
    {
        // A persisted Module that is canonical but not available in this build denies the
        // ENTIRE resolution (P1-T04) — the HTTP boundary proves denied → 403, with the
        // available-sibling semantics proven at resolver level.
        using var factory = ProtectedHost(
            current: TestNavigationComposition.User(),
            outcome: new AccessOutcome.Denied(AccessDenialReason.UnavailableModule));
        using var client = factory.CreateClient();

        var response = await GetWithAuthAsync(client, JobOnViewRoute);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnknownPersistedModule_FailsClosedEntireResolution_AtHttpBoundary()
    {
        using var factory = ProtectedHost(
            current: TestNavigationComposition.User(),
            outcome: new AccessOutcome.Denied(AccessDenialReason.UnknownModule));
        using var client = factory.CreateClient();

        var response = await GetWithAuthAsync(client, JobOnViewRoute);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RoleLabel_CannotBypassModuleGates()
    {
        // A USER whose free-text role label says "Admin" (or any other text) is still a USER
        // with exactly its effective Modules: job-on-view granted, job-on-create denied.
        var jobOnView = TestNavigationComposition.Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        foreach (var roleLabel in new[] { "Admin", "Responsável", "Chefe de Turno" })
        {
            using var baseFactory = new DmoWebApplicationFactory();
            using var factory = ProtectedHost(
                current: TestNavigationComposition.User(roleLabel),
                outcome: TestNavigationComposition.Granted(jobOnView),
                available: [jobOnView],
                routes: new Dictionary<string, string> { ["job-on"] = "/implemented/job-on" });
            using var client = factory.CreateClient();

            var denied = await GetWithAuthAsync(client, JobOnCreateRoute);
            var allowed = await GetWithAuthAsync(client, JobOnViewRoute);

            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }
    }

    [Fact]
    public async Task UserWithAdminRoleLabel_OnAdministrationSurface_StillDenied()
    {
        // The ADMIN-only dmo.administration gate denies a USER regardless of role label —
        // no role-label bypass in either direction.
        using var factory = ProtectedHost(
            current: TestNavigationComposition.User("Admin"),
            outcome: TestNavigationComposition.Granted());
        using var client = factory.CreateClient();

        var response = await GetWithAuthAsync(client, "/Administration");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- composition -------------------------------------------------------------

    private static async Task<HttpResponseMessage> GetWithAuthAsync(HttpClient client, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(TestAuthenticationHandler.HeaderName, "test-caller");
        return await client.SendAsync(request);
    }

    private static WebApplicationFactory<Program> ProtectedHost(
        DMO.Application.Session.CurrentAccount current,
        DMO.Application.Access.AccessOutcome outcome,
        IReadOnlyList<DMO.Application.Access.ModuleDefinition>? available = null,
        IReadOnlyDictionary<string, string>? routes = null)
    {
        var baseFactory = new DmoWebApplicationFactory();
        return baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICurrentAccountContext>();
                services.RemoveAll<IModuleRegistry>();
                services.RemoveAll<IModuleAccessService>();
                services.RemoveAll<IDestinationRouteRegistry>();
                services.AddSingleton<ICurrentAccountContext>(new TestNavigationComposition.FixedCurrentAccountContext(current));
                services.AddSingleton<IModuleRegistry>(ModuleRegistry.Create(available ?? []));
                services.AddSingleton<IModuleAccessService>(new TestNavigationComposition.FixedAccessService(outcome));
                services.AddSingleton<IDestinationRouteRegistry>(new TestNavigationComposition.DictionaryRouteRegistry(routes ?? new Dictionary<string, string>()));

                // Test-only protected routes + test authentication scheme (this composition
                // only; production Program.cs has neither). The startup filter runs at host
                // startup with the test services applied; its app object is an
                // ApplicationBuilder, so route mapping goes through UseRouting/UseEndpoints,
                // which target the application's own endpoint route builder. The scheme
                // configuration is registered guardedly because the deferred test host
                // applies test-service configuration more than once.
                services.AddSingleton<IStartupFilter, ProtectedRouteStartupFilter>();
                services.Configure<AuthenticationOptions>(options =>
                {
                    if (!options.Schemes.Any(scheme => scheme.Name == TestAuthenticationHandler.SchemeName))
                    {
                        options.AddScheme<TestAuthenticationHandler>(
                            TestAuthenticationHandler.SchemeName, displayName: null);
                    }

                    options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultForbidScheme = TestAuthenticationHandler.SchemeName;
                });
            }));
    }

    private static int Count(string html, string fragment)
    {
        var count = 0;
        var index = 0;
        while ((index = html.IndexOf(fragment, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += fragment.Length;
        }

        return count;
    }

    /// <summary>
    /// Maps the controlled protected routes used by these tests. The routes live only in the
    /// test host composition — production code never registers them — and they consume the
    /// real P1-T04 Module policies.
    /// </summary>
    private sealed class ProtectedRouteStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                // The filter runs before ConfigureApplication: its middleware factories are
                // executed when the destination pipeline is built, by which time the global
                // endpoint route builder property is set. UseRouting/UseEndpoints map the
                // test routes into the application's endpoint data sources, and the
                // authentication/authorization middleware pair satisfies the EndpointMiddleware
                // requirement that authorization runs between routing and endpoints.
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    endpoints
                        .MapGet(JobOnViewRoute, () => Results.Text("test:job-on-view"))
                        .RequireAuthorization(ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.JobOnView));
                    endpoints
                        .MapGet(JobOnCreateRoute, () => Results.Text("test:job-on-create"))
                        .RequireAuthorization(ModuleAuthorizationPolicies.PolicyName(ModuleCatalog.JobOnCreate));
                });

                next(app);
            };
    }

    /// <summary>
    /// Test authentication scheme: marks requests carrying the test header as
    /// "authenticated" so policy failure surfaces as HTTP 403 (otherwise the anonymous
    /// challenge masks the handler denial). It grants no application role and carries no
    /// access claims.
    /// </summary>
    private sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "dmo.test.auth";

        /// <summary>Header carrying the test authentication mark.</summary>
        public const string HeaderName = "X-Dmo-Test-Auth";

        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var header = Request.Headers[HeaderName];
            if (string.IsNullOrWhiteSpace(header))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, header.ToString())],
                    SchemeName));

            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(principal, SchemeName)));
        }
    }
}