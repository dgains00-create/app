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

[Collection(ProcessEnvironmentCollection.Name)]
public sealed class SharedShellTests
{
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
            [jobOnView, jobOnCreate, ferramentas],
            new Dictionary<string, string> { ["job-on"] = "/implemented/job-on" });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Maria Operadora", html, StringComparison.Ordinal);
        Assert.Contains("1042", html, StringComparison.Ordinal);
        Assert.Contains("Turno A", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/implemented/job-on\"", html, StringComparison.Ordinal);
        Assert.Equal(1, Count(html, "href=\"/implemented/job-on\""));
        Assert.DoesNotContain(">Ferramentas<", html, StringComparison.Ordinal);
        Assert.Contains("dmo-page-heading", html, StringComparison.Ordinal);
        Assert.Contains("dmo-production-slot", html, StringComparison.Ordinal);
        Assert.Contains("dmo-work-surface", html, StringComparison.Ordinal);
        Assert.Contains("role=\"status\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Shell_RejectsUnauthenticatedRequest()
    {
        using var baseFactory = new DmoWebApplicationFactory();
        using var factory = ConfigureFactory(baseFactory, new CurrentAccount.None(), [], [], new Dictionary<string, string>());
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StaticAssetsResolveAndContainDesktopTabletFoundation()
    {
        using var baseFactory = new DmoWebApplicationFactory();
        using var factory = ConfigureFactory(baseFactory, new CurrentAccount.None(), [], [], new Dictionary<string, string>());
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
        IReadOnlyList<ModuleDefinition> granted,
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
                services.AddSingleton<IModuleAccessService>(new FixedAccessService(new AccessOutcome.Granted(granted)));
                services.AddSingleton<IDestinationRouteRegistry>(new DictionaryRouteRegistry(routes));
            });
        });
    }

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
