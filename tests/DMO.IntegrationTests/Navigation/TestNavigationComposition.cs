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

namespace DMO.IntegrationTests.Navigation;

/// <summary>
/// P1-T07 test-host composition: the real web application with the navigation-related seams
/// replaced by controlled test fakes (current account, Module access outcome, build registry,
/// destination-route registry). Same posture as the A2 shell tests; this helper is shared by
/// the P1-T07 routing/no-access/login tests. Production composition is untouched.
/// </summary>
public static class TestNavigationComposition
{
    public static WebApplicationFactory<Program> ConfigureFactory(
        DmoWebApplicationFactory factory,
        CurrentAccount current,
        IReadOnlyList<ModuleDefinition> available,
        AccessOutcome outcome,
        IReadOnlyDictionary<string, string> routes,
        Action<IServiceCollection>? additional = null)
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

                additional?.Invoke(services);
            });
        });
    }

    public static AccessOutcome Granted(params ModuleDefinition[] modules) =>
        new AccessOutcome.Granted(null, modules);

    public static AccessOutcome GrantedWithLanding(string? landing, params ModuleDefinition[] modules) =>
        new AccessOutcome.Granted(landing, modules);

    public static ModuleDefinition Definition(
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

    public static CurrentAccount.User User(
        string roleLabel = "Turno A",
        Guid? templateId = null,
        Guid? accountId = null) =>
        new(new UserAccount(
            accountId ?? Guid.NewGuid(),
            "1042",
            "Maria Operadora",
            "maria@example.test",
            roleLabel,
            IsActive: true,
            templateId ?? Guid.NewGuid(),
            Version: 1));

    public static CurrentAccount.Admin Admin(string displayName = "Ana Administradora") =>
        new(new AdminAccount(Guid.NewGuid(), displayName, "admin@example.test", IsActive: true));

    public sealed class FixedCurrentAccountContext : ICurrentAccountContext
    {
        private readonly CurrentAccount _current;

        public FixedCurrentAccountContext(CurrentAccount current) => _current = current;

        public Task<CurrentAccount> GetCurrentAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_current);
    }

    public sealed class FixedAccessService : IModuleAccessService
    {
        private readonly AccessOutcome _outcome;
        private readonly bool _failsWhenInvoked;

        public FixedAccessService(AccessOutcome outcome, bool failsWhenInvoked = false)
        {
            _outcome = outcome;
            _failsWhenInvoked = failsWhenInvoked;
        }

        public int ResolveCallCount { get; private set; }

        public Task<AccessOutcome> ResolveUserAccessAsync(AccountResolution resolution, CancellationToken cancellationToken)
        {
            ResolveCallCount++;
            ThrowWhenInvocationIsForbidden();
            return Task.FromResult(_outcome);
        }

        public Task<bool> HasModuleAsync(AccountResolution resolution, ModuleId requiredModule, CancellationToken cancellationToken)
        {
            ThrowWhenInvocationIsForbidden();
            return Task.FromResult(_outcome is AccessOutcome.Granted(_, var modules) && modules.Any(module => module.Id == requiredModule));
        }

        private void ThrowWhenInvocationIsForbidden()
        {
            if (_failsWhenInvoked)
            {
                throw new InvalidOperationException(
                    "Operational USER access resolution must not be invoked for a non-USER account.");
            }
        }
    }

    public sealed class DictionaryRouteRegistry : IDestinationRouteRegistry
    {
        private readonly IReadOnlyDictionary<string, string> _routes;

        public DictionaryRouteRegistry(IReadOnlyDictionary<string, string> routes) => _routes = routes;

        public bool TryGetRoute(string destinationId, out string route) =>
            _routes.TryGetValue(destinationId, out route!);
    }
}