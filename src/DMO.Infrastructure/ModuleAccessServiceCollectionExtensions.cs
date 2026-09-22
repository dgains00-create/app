using DMO.Application.Access;
using Microsoft.Extensions.DependencyInjection;

namespace DMO.Infrastructure;

/// <summary>
/// Registers the P1-T04 Module access foundation: one immutable Module Registry built from
/// the explicit current-build registration, plus the scoped access resolver and access
/// service.
/// </summary>
/// <remarks>
/// <para>
/// The registry is a singleton: an immutable, code-owned view of the current build
/// (vocabulary + explicitly registered availability). The resolver and the access service are
/// scoped because they consume the scoped P1-T03 repository contracts.
/// </para>
/// <para>
/// This is the single registration call-site; <c>AddDmoInfrastructure</c> composes it
/// following the existing <c>AddPersistenceFoundation</c> pattern.
/// </para>
/// </remarks>
public static class ModuleAccessServiceCollectionExtensions
{
    /// <summary>Registers the Module registry, access resolver and access service.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddModuleAccess(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IModuleRegistry>(_ => ModuleRegistry.Create(ModuleRegistrations.CurrentBuildAvailable));
        services.AddScoped<IAccessResolver, AccessResolver>();
        services.AddScoped<IModuleAccessService, ModuleAccessService>();

        return services;
    }
}