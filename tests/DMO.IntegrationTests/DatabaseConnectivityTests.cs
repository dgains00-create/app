using DMO.Application.Migrations;
using DMO.Infrastructure.Persistence;
using DMO.IntegrationTests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DMO.IntegrationTests;

/// <summary>
/// P1-T01 + P1-T03 connectivity test — the migration mechanism against a real PostgreSQL
/// database: it applies Migration 001 then Migration 002 (in order, in one run), creates the
/// four Phase-1 tables and nothing else, and a second run applies nothing (idempotent).
/// </summary>
/// <remarks>
/// <para>
/// Runs only against a <b>disposable</b> PostgreSQL database supplied through
/// <c>DMO_TEST_POSTGRES_CONNECTION</c>; without it the test is skipped, so it never silently
/// targets a development or production database. It resets the disposable schema first so the
/// "applies 001 then 002 in order" evidence is deterministic.
/// </para>
/// <para>
/// This test is serialized with the persistence integration tests because it mutates the
/// shared disposable schema.
/// </para>
/// </remarks>
[Collection(PersistenceDatabaseCollection.Name)]
public sealed class DatabaseConnectivityTests
{
    /// <summary>Environment variable holding the disposable test database connection string.</summary>
    public const string ConnectionEnvironmentVariable = "DMO_TEST_POSTGRES_CONNECTION";

    private static string? ConnectionString =>
        Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable);

    /// <summary>
    /// P2-T04 (disclosed extension, contract §3/§16): the migration run applies EVERY pending
    /// migration — now 001, 002 and the P2-T04 domain-core migration — and the schema is exactly the
    /// four foundation tables plus the six contracted domain-core tables, nothing more. The
    /// idempotency evidence is unchanged.
    /// </summary>
    [SkippableFact]
    public async Task MigrationRun_Applies001Then002_AndCreatesOnlyTheFoundationTables()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString),
            $"Set {ConnectionEnvironmentVariable} to a disposable PostgreSQL database to run this test.");

        await using var context = PersistenceTestDatabase.CreateContext();

        // Precondition: a truly fresh schema so the run applies every pending migration.
        await PersistenceTestDatabase.ResetSchemaAsync(context);

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton(_ => context); // same context instance across the two calls
        services.AddScoped<IMigrationRunner, EfCoreMigrationRunner>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        // Action: run the migration mechanism from the fresh schema.
        var first = await runner.ApplyPendingAsync();

        // Assertions: the three migrations, in generation order (001, 002, P2-T04 domain core).
        Assert.Equal(3, first.AppliedCount);
        Assert.Equal(
            new[]
            {
                "20260922001736_AccountAndTemplateFoundation",
                "20260922001757_TemplateModuleComposition",
                "20260922232349_ToolJobOnDomainCore",
            },
            first.AppliedMigrations);

        // The schema is exactly the four foundation tables plus the six contracted domain-core
        // tables — nothing more.
        var tables = await ReadPublicTablesAsync(context);
        Assert.Equal(
            new[]
            {
                "__EFMigrationsHistory", "admin_accounts", "bq_contexts", "cm_contexts", "job_ons",
                "mf_contexts", "template_modules", "templates", "tool_machines", "tools", "users",
            },
            tables);

        // Idempotency: a second run applies nothing and changes nothing.
        var second = await runner.ApplyPendingAsync();
        Assert.Equal(0, second.AppliedCount);
        Assert.Empty(second.AppliedMigrations);
        Assert.Equal(tables, await ReadPublicTablesAsync(context));
    }

    private static async Task<IReadOnlyList<string>> ReadPublicTablesAsync(DmoDbContext context)
    {
        var tables = new List<string>();
        var connection = context.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.CommandText =
            "select table_name from information_schema.tables " +
            "where table_schema = 'public' and table_type = 'BASE TABLE' order by table_name";

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }
}