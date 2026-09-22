using DMO.Application.Accounts;
using DMO.Application.Authentication;
using DMO.Application.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DMO.Infrastructure.Persistence;

/// <summary>
/// Registers the P1-T03 persistence foundation: the persistence-backed account/authentication
/// lookups and the repository primitives.
/// </summary>
/// <remarks>
/// All registrations are scoped to the request and consume the single
/// <see cref="DmoDbContext"/>. <see cref="IAccountLookup"/> now resolves to
/// <see cref="PersistenceAccountLookup"/> (the P1-T02 <c>UnavailableAccountLookup</c> is
/// deleted); the USER authentication boundary consumes the narrow
/// <see cref="IUserAuthenticationLookup"/> contract, never EF directly.
/// </remarks>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers the persistence-backed lookups and repositories.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddPersistenceFoundation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IAccountLookup, PersistenceAccountLookup>();
        services.AddScoped<IUserAuthenticationLookup, UserAuthenticationLookup>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAdminAccountRepository, AdminAccountRepository>();
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<ITemplateModuleRepository, TemplateModuleRepository>();

        return services;
    }
}