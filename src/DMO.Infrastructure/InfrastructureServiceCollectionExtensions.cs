using DMO.Application.Migrations;
using DMO.Infrastructure.Database;
using DMO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DMO.Infrastructure;

/// <summary>
/// Registers the shared infrastructure required by the runtime.
/// </summary>
/// <remarks>
/// Infrastructure plumbing only: connection configuration, the persistence context and the
/// migration-runner boundary. No Phase 1 domain schema and no industrial behaviour is
/// registered here.
/// </remarks>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers PostgreSQL connection configuration, the persistence context and the
    /// migration runner.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <remarks>
    /// The connection string is validated eagerly here so a missing or invalid database
    /// configuration fails the startup loudly instead of surfacing later as an obscure
    /// runtime error. No default connection is substituted.
    /// </remarks>
    public static IServiceCollection AddDmoInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        // Fail fast at startup, not lazily on first database use.
        //
        // The runtime host must not report itself as started when the database configuration
        // it will need is absent or invalid. Validating here means the process fails during
        // composition instead of appearing healthy and failing later on the first request
        // that touches persistence.
        var startupOptions = new DatabaseOptions();
        configuration.GetSection(DatabaseOptions.SectionName).Bind(startupOptions);
        _ = new DatabaseConnectionResolver(startupOptions).GetConnectionString();

        services.AddSingleton<DatabaseConnectionResolver>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            return new DatabaseConnectionResolver(options);
        });

        services.AddDbContext<DmoDbContext>((provider, builder) =>
        {
            var resolver = provider.GetRequiredService<DatabaseConnectionResolver>();
            builder.UseNpgsql(resolver.GetConnectionString(), npgsql => npgsql.MigrationsAssembly(
                typeof(DmoDbContext).Assembly.GetName().Name));
        });

        services.AddScoped<IMigrationRunner, EfCoreMigrationRunner>();

        return services;
    }
}
