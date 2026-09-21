using DMO.Application.Migrations;
using DMO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DMO.IntegrationTests;

/// <summary>
/// Proposed P1-T01 test — a migration run against a real PostgreSQL database applies nothing.
/// </summary>
/// <remarks>
/// <para>
/// This is the only proposed test that requires a live PostgreSQL database. It is skipped
/// unless <c>DMO_TEST_POSTGRES_CONNECTION</c> is set in the environment, so it never
/// silently targets a development or production database. The Architect decides whether it
/// runs, and against which environment.
/// </para>
/// <para>
/// PROPOSED — NOT EXECUTED. Awaiting Architect review before first execution.
/// See <c>docs/PROPOSED_TESTS_P1-T01.md</c> for the full test protocol record.
/// </para>
/// </remarks>
public sealed class DatabaseConnectivityTests
{
    /// <summary>Environment variable holding the disposable test database connection string.</summary>
    public const string ConnectionEnvironmentVariable = "DMO_TEST_POSTGRES_CONNECTION";

    private static string? ConnectionString =>
        Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable);

    [SkippableFact]
    public async Task ApplyPending_AgainstRealDatabase_AppliesNothingAndLeavesSchemaUntouched()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString),
            $"Set {ConnectionEnvironmentVariable} to a disposable PostgreSQL database to run this test.");

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddDbContext<DmoDbContext>(options => options.UseNpgsql(ConnectionString));
        services.AddScoped<IMigrationRunner, EfCoreMigrationRunner>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<DmoDbContext>();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        // Preconditions: the target schema is fully identified before the run.
        var tablesBefore = await ReadPublicTablesAsync(context);

        // Action: run the migration mechanism.
        var result = await runner.ApplyPendingAsync();

        // Assertions: no product migration exists, so nothing is applied.
        Assert.Equal(0, result.AppliedCount);
        Assert.Empty(result.AppliedMigrations);

        // Required non-effect: the schema is unchanged — no Phase 1 table was created.
        var tablesAfter = await ReadPublicTablesAsync(context);
        Assert.Equal(tablesBefore, tablesAfter);
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
