using System.Net;
using DMO.Application.Access;
using DMO.Application.Accounts;
using DMO.Application.Session;
using DMO.IntegrationTests.Host;
using DMO.Web.Frontend.Shell;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DMO.IntegrationTests.Frontend.Shared;

/// <summary>
/// A2 correction: the rendered production shell never advertises provisional/fixture
/// destinations. With no live destination it renders the accepted operational empty state;
/// a denied USER keeps the existing fail-closed status; ADMIN renders identity only.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class SharedShellTests
{
    private const string OperationalEmptyState = "Sem destinos operacionais disponíveis";
    private const string FailClosedStatus = "A navegação operacional está indisponível. O acesso continua fechado.";
    private const string ProvisionalContractMarker = "PROVISIONAL FRONTEND CONTRACT";

    [Fact]
    public async Task Shell_RendersIdentityRegionsAndPublishedNavigationProjection()
    {
        var jobOnView = Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        var jobOnCreate = Definition(ModuleCatalog.JobOnCreate, "Job On Create", "job-on", "Job On");
        var ferramentas = Definition(ModuleCatalog.Ferramentas, "Ferramentas", null, "Ferramentas", contextual: true);

        using var baseFactory = new DmoWebApplicationFactory();
        using var factory = ConfigureFactory(baseFactory,
            new CurrentAccount.User(new UserAccount(
                Guid.NewGuid(), "1042", "Maria Operadora", "maria@example.test", "Turno A", true, Guid.NewGuid(), 1)),
            [jobOnView, jobOnCreate, ferramentas],
            Granted(jobOnView, jobOnCreate, ferramentas),
            new Dictionary<string, string> { ["job-on"] = "/implemented/job-on" });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");
        var html = Decode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Maria Operadora", html, StringComparison.Ordinal);
        Assert.Contains("1042", html, StringComparison.Ordinal);
        Assert.Contains("Turno A", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/implemented/job-on\"", html, StringComparison.Ordinal);
        Assert.Equal(1, Count(html, "href=\"/implemented/job-on\""));
        Assert.DoesNotContain(">Ferramentas<", html, StringComparison.Ordinal);
        Assert.DoesNotContain(OperationalEmptyState, html, StringComparison.Ordinal);
        Assert.DoesNotContain(ProvisionalContractMarker, html, StringComparison.Ordinal);
        Assert.Contains("dmo-page-heading", html, StringComparison.Ordinal);
        Assert.Contains("dmo-production-slot", html, StringComparison.Ordinal);
        Assert.Contains("dmo-work-surface", html, StringComparison.Ordinal);
        Assert.Contains("role=\"status\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Shell_NoLiveDestinations_RendersOperationalEmptyState()
    {
        using var baseFactory = new DmoWebApplicationFactory();
        using var factory = ConfigureFactory(
            baseFactory,
            new CurrentAccount.User(new UserAccount(
                Guid.NewGuid(), "1042", "Maria Operadora", "maria@example.test", "Turno A", true, Guid.NewGuid(), 1)),
            available: [],
            Granted(),
            new Dictionary<string, string>());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");
        var html = Decode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(OperationalEmptyState, html, StringComparison.Ordinal);
        Assert.Contains("Maria Operadora", html, StringComparison.Ordinal);
        Assert.Contains("dmo-nav-empty", html, StringComparison.Ordinal);
        // An empty registry is a normal state, not an access failure.
        Assert.DoesNotContain(FailClosedStatus, html, StringComparison.Ordinal);
        Assert.DoesNotContain(ProvisionalContractMarker, html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Shell_ProductionOutput_DoesNotContainProvisionalContractMarker()
    {
        using var baseFactory = new DmoWebApplicationFactory();
        using var factory = ConfigureFactory(
            baseFactory,
            new CurrentAccount.User(new UserAccount(
                Guid.NewGuid(), "1042", "Maria Operadora", "maria@example.test", "Turno A", true, Guid.NewGuid(), 1)),
            available: [],
            Granted(),
            new Dictionary<string, string>());
        using var client = factory.CreateClient();

        var html = Decode(await (await client.GetAsync("/")).Content.ReadAsStringAsync());

        Assert.DoesNotContain(ProvisionalContractMarker, html, StringComparison.Ordinal);
        Assert.DoesNotContain("PROVISIONAL", html, StringComparison.Ordinal);
        Assert.DoesNotContain("dmo-contract-label", html, StringComparison.Ordinal);
        Assert.DoesNotContain("is-provisional", html, StringComparison.Ordinal);
        // No fixture destination label is advertised as a normal shell destination.
        Assert.DoesNotContain("Reparação Interna", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Boquilhas", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Armazém", html, StringComparison.Ordinal);

        // The production runtime source carries no fixture symbol either.
        var runtime = string.Join('\n', Directory.EnumerateFiles(
                Path.Combine(FindRepositoryRoot(), "src", "DMO.Web"),
                "*.cs",
                SearchOption.AllDirectories)
            .Select(File.ReadAllText));
        Assert.DoesNotContain("A2Fixtures", runtime, StringComparison.Ordinal);
        Assert.DoesNotContain("ProvisionalFixturesWhenNeeded", runtime, StringComparison.Ordinal);
        Assert.DoesNotContain("ProvisionalDestinationPresentation", runtime, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Shell_DeniedAccess_RendersFailClosedStatusWithoutFakeDestinations()
    {
        using var baseFactory = new DmoWebApplicationFactory();
        using var factory = ConfigureFactory(
            baseFactory,
            new CurrentAccount.User(new UserAccount(
                Guid.NewGuid(), "1042", "Maria Operadora", "maria@example.test", "Turno A", true, Guid.NewGuid(), 1)),
            available: [],
            new AccessOutcome.Denied(AccessDenialReason.NoTemplate),
            new Dictionary<string, string>());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");
        var html = Decode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // The fail-closed status is emitted through a Razor model expression, so Razor
        // HTML-encodes its accents; assertions use the decoded rendered text.
        Assert.Contains(FailClosedStatus, html, StringComparison.Ordinal);
        Assert.Contains(OperationalEmptyState, html, StringComparison.Ordinal);
        Assert.Contains("Maria Operadora", html, StringComparison.Ordinal);

        // Fail closed means no advertised destination of any kind.
        Assert.DoesNotContain(ProvisionalContractMarker, html, StringComparison.Ordinal);
        Assert.DoesNotContain("is-provisional", html, StringComparison.Ordinal);
        Assert.DoesNotContain("dmo-contract-label", html, StringComparison.Ordinal);
        Assert.DoesNotContain("dmo-primary-link", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/implemented/", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Shell_Admin_RendersNoOperationalDestinationLinksOrFixtures()
    {
        // A real definition and a route exist, so an ADMIN shell that wrongly projected
        // operational destinations would render a link.
        var jobOnView = Definition(ModuleCatalog.JobOnView, "Job On View", "job-on", "Job On");
        using var baseFactory = new DmoWebApplicationFactory();
        using var factory = ConfigureFactory(
            baseFactory,
            new CurrentAccount.Admin(new AdminAccount(
                Guid.NewGuid(), "Ana Administradora", "admin@example.test", true)),
            [jobOnView],
            Granted(jobOnView),
            new Dictionary<string, string> { ["job-on"] = "/implemented/job-on" });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");
        var html = Decode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Ana Administradora", html, StringComparison.Ordinal);
        Assert.Contains("Administrador", html, StringComparison.Ordinal);
        Assert.Contains(OperationalEmptyState, html, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/implemented/job-on\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain(">Job On<", html, StringComparison.Ordinal);
        Assert.DoesNotContain(ProvisionalContractMarker, html, StringComparison.Ordinal);
        Assert.DoesNotContain("is-provisional", html, StringComparison.Ordinal);
        Assert.DoesNotContain("dmo-contract-label", html, StringComparison.Ordinal);
        // ADMIN is not an access failure.
        Assert.DoesNotContain(FailClosedStatus, html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Shell_RejectsUnauthenticatedRequest()
    {
        using var baseFactory = new DmoWebApplicationFactory();
        using var factory = ConfigureFactory(baseFactory, new CurrentAccount.None(), [], Granted(), new Dictionary<string, string>());
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StaticAssetsResolveAndContainDesktopTabletFoundation()
    {
        using var baseFactory = new DmoWebApplicationFactory();
        using var factory = ConfigureFactory(baseFactory, new CurrentAccount.None(), [], Granted(), new Dictionary<string, string>());
        using var client = factory.CreateClient();

        var tokensResponse = await client.GetAsync("/css/dmo-tokens.css");
        var shellResponse = await client.GetAsync("/css/dmo-shell.css");
        var tokensCss = await tokensResponse.Content.ReadAsStringAsync();
        var shellCss = await shellResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, tokensResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, shellResponse.StatusCode);
        Assert.Contains("--dmo-content-max: 90rem", tokensCss, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 62rem)", shellCss, StringComparison.Ordinal);
        Assert.Contains("overflow-x: auto", shellCss, StringComparison.Ordinal);
        Assert.Contains(".dmo-primary-link", shellCss, StringComparison.Ordinal);
        Assert.Contains(".dmo-nav-empty", shellCss, StringComparison.Ordinal);

        // Fixture-only selectors are gone from the production stylesheet.
        Assert.DoesNotContain("is-provisional", shellCss, StringComparison.Ordinal);
        Assert.DoesNotContain("dmo-contract-label", shellCss, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedFrontendSource_DoesNotImportFeatureServices()
    {
        var root = FindRepositoryRoot();
        var frontendRoot = Path.Combine(root, "src", "DMO.Web", "Frontend");
        var forbiddenNamespaces = new[] { ".JobOn", ".Controlo", ".Peso", ".Boquilhas", ".Ferramentas" };

        var source = string.Join('\n', Directory.EnumerateFiles(frontendRoot, "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText));

        Assert.All(forbiddenNamespaces, forbidden =>
            Assert.DoesNotContain($"using DMO{forbidden}", source, StringComparison.Ordinal));
    }

    private static WebApplicationFactory<Program> ConfigureFactory(
        DmoWebApplicationFactory factory,
        CurrentAccount current,
        IReadOnlyList<ModuleDefinition> available,
        AccessOutcome outcome,
        IReadOnlyDictionary<string, string> routes)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICurrentAccountContext>();
                services.RemoveAll<IModuleRegistry>();
                services.RemoveAll<IModuleAccessService>();
                services.RemoveAll<IDestinationRouteRegistry>();
                services.AddSingleton<ICurrentAccountContext>(new FixedCurrentAccountContext(current));
                services.AddSingleton<IModuleRegistry>(ModuleRegistry.Create(available));
                services.AddSingleton<IModuleAccessService>(new FixedAccessService(outcome));
                services.AddSingleton<IDestinationRouteRegistry>(new DictionaryRouteRegistry(routes));
            });
        });
    }

    private static AccessOutcome Granted(params ModuleDefinition[] modules) =>
        new AccessOutcome.Granted(modules);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DMO.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private static int Count(string value, string fragment) =>
        value.Split(fragment, StringSplitOptions.None).Length - 1;

    /// <summary>
    /// Decodes HTML entities so assertions can be written against rendered text.
    /// </summary>
    /// <remarks>
    /// Razor escapes model expressions (<c>@shell.StatusMessage</c>), so accented status text
    /// reaches the response as numeric entities such as <c>&amp;#xE7;</c>, while literal Razor
    /// markup (for example the navigation empty state) is emitted as raw UTF-8. Decoding the
    /// response makes both forms comparable to the accepted user-visible strings.
    /// </remarks>
    private static string Decode(string html) => WebUtility.HtmlDecode(html);

    private static ModuleDefinition Definition(
        ModuleId id,
        string displayName,
        string? destinationId,
        string surfaceName,
        bool contextual = false) =>
        new(id, displayName, destinationId, !contextual, new ModuleSurfaceDescriptor(
            surfaceName,
            destinationId,
            contextual,
            []));

    private sealed class FixedCurrentAccountContext : ICurrentAccountContext
    {
        private readonly CurrentAccount _current;
        public FixedCurrentAccountContext(CurrentAccount current) => _current = current;
        public Task<CurrentAccount> GetCurrentAsync(CancellationToken cancellationToken) => Task.FromResult(_current);
    }

    private sealed class FixedAccessService : IModuleAccessService
    {
        private readonly AccessOutcome _outcome;
        public FixedAccessService(AccessOutcome outcome) => _outcome = outcome;
        public Task<AccessOutcome> ResolveUserAccessAsync(AccountResolution resolution, CancellationToken cancellationToken) => Task.FromResult(_outcome);
        public Task<bool> HasModuleAsync(AccountResolution resolution, ModuleId requiredModule, CancellationToken cancellationToken) =>
            Task.FromResult(_outcome is AccessOutcome.Granted(var modules) && modules.Any(module => module.Id == requiredModule));
    }

    private sealed class DictionaryRouteRegistry : IDestinationRouteRegistry
    {
        private readonly IReadOnlyDictionary<string, string> _routes;
        public DictionaryRouteRegistry(IReadOnlyDictionary<string, string> routes) => _routes = routes;
        public bool TryGetRoute(string destinationId, out string route) => _routes.TryGetValue(destinationId, out route!);
    }
}
