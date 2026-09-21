using DMO.Application.Migrations;
using DMO.Infrastructure.Database;
using DMO.Infrastructure.Persistence;
using DMO.IntegrationTests.Host;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DMO.IntegrationTests;

/// <summary>
/// Proposed P1-T01 test — the migration mechanism works and Phase 1 creates no schema.
/// </summary>
/// <remarks>
/// PROPOSED — NOT EXECUTED. Awaiting Architect review before first execution.
/// See <c>docs/PROPOSED_TESTS_P1-T01.md</c> for the full test protocol record.
/// </remarks>
public sealed class MigrationRunnerTests
{
    [Fact]
    public void PersistenceContext_DeclaresNoEntityTypes()
    {
        // Preconditions: the single application context, built without touching a database.
        var options = new DbContextOptionsBuilder<DmoDbContext>()
            .UseNpgsql(DmoWebApplicationFactory.PlaceholderConnectionString)
            .Options;

        using var context = new DmoDbContext(options);

        // Assertion: Phase 1 declares no persisted entity, so no product table is implied.
        Assert.Empty(context.Model.GetEntityTypes());

        // Required non-effect: none of the forbidden Phase 1 tables is modelled.
        var modelled = context.Model.GetEntityTypes().Select(e => e.GetTableName()).ToList();
        foreach (var forbidden in new[]
                 {
                     "users", "admin_accounts", "templates", "template_modules",
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
        await using var scope = provider.CreateAsyncScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        // Action + assertion: the runner refuses loudly instead of targeting a default database.
        await Assert.ThrowsAsync<DatabaseConfigurationException>(() => runner.ListPendingAsync());
    }
}
