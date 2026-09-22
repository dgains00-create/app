using DMO.Application.Migrations;
using DMO.Infrastructure.Database;
using DMO.Infrastructure.Persistence;
using DMO.IntegrationTests.Host;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DMO.IntegrationTests;

/// <summary>
/// P1-T01/P1-T03 test — the migration mechanism works and Phase 1 models exactly the
/// persistence-foundation schema.
/// </summary>
/// <remarks>
/// PROPOSED — NOT EXECUTED. Awaiting Architect review before first execution.
/// See <c>docs/PROPOSED_TESTS_P1-T01.md</c> for the full test protocol record.
/// </remarks>
public sealed class MigrationRunnerTests
{
    [Fact]
    public void PersistenceContext_DeclaresExactlyTheFoundationEntities()
    {
        // Preconditions: the single application context, built without touching a database.
        var options = new DbContextOptionsBuilder<DmoDbContext>()
            .UseNpgsql(DmoWebApplicationFactory.PlaceholderConnectionString)
            .Options;

        using var context = new DmoDbContext(options);

        // P1-T03 assertion: the context models exactly the four persistence-foundation
        // tables (Migration 001 + 002). No other Phase 1 table is implied.
        var modelled = context.Model.GetEntityTypes()
            .Select(e => e.GetTableName())
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(
            new[] { "admin_accounts", "template_modules", "templates", "users" },
            modelled);

        // Required non-effect: none of the forbidden Phase 1 tables is modelled.
        foreach (var forbidden in new[]
                 {
                     "permissions", "capabilities", "roles", "settings", "admin_audit_events",
                 })
        {
            Assert.DoesNotContain(forbidden, modelled);
        }
    }

    [Fact]
    public async Task ListPendingAsync_WithNoConfiguredConnection_Throws()
    {
        // Preconditions: infrastructure registered without a connection string.
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<DatabaseOptions>(_ => { });
        services.AddSingleton<DatabaseConnectionResolver>(provider =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>().Value;
            return new DatabaseConnectionResolver(options);
        });
        services.AddDbContext<DmoDbContext>((provider, builder) =>
        {
            var resolver = provider.GetRequiredService<DatabaseConnectionResolver>();
            builder.UseNpgsql(resolver.GetConnectionString());
        });
        services.AddScoped<IMigrationRunner, EfCoreMigrationRunner>();

        await using var provider = services.BuildServiceProvider();

        // Action + assertion: the runner refuses loudly instead of targeting a default database.
        //
        // The WHOLE attempted migration-path operation is inside the asserted delegate —
        // creating the scope, resolving IMigrationRunner and calling ListPendingAsync — because
        // database configuration is validated when the persistence services are resolved, not
        // only when the runner method executes. The accepted contract is "attempting the
        // migration path with no database configuration raises DatabaseConfigurationException";
        // it does not require the exception to originate specifically inside ListPendingAsync.
        var exception = await Assert.ThrowsAsync<DatabaseConfigurationException>(async () =>
        {
            await using var scope = provider.CreateAsyncScope();
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
            await runner.ListPendingAsync();
        });

        // Required non-effect: the failure names the missing configuration rather than
        // silently targeting an implicit or default database.
        Assert.Contains(DatabaseOptions.ConnectionStringKey, exception.Message, StringComparison.Ordinal);
    }
}
