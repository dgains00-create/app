using DMO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DMO.IntegrationTests.Persistence;

/// <summary>
/// Shared helper for the env-gated persistence integration tests.
/// </summary>
/// <remarks>
/// Like the P1-T01 <c>DatabaseConnectivityTests</c> posture, these tests run only against a
/// <b>disposable</b> PostgreSQL database supplied through <c>DMO_TEST_POSTGRES_CONNECTION</c>;
/// without it every test is skipped. The disposable database receives the real product
/// migrations (001 + 002) so the schema evidence matches the migrations to be applied to
/// DEV/TEST.
/// </remarks>
public static class PersistenceTestDatabase
{
    /// <summary>Environment variable holding the disposable test database connection string.</summary>
    public const string ConnectionEnvironmentVariable = "DMO_TEST_POSTGRES_CONNECTION";

    /// <summary>The configured disposable connection string, or <c>null</c>.</summary>
    public static string? ConnectionString =>
        Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable);

    /// <summary>Skips the current test when no disposable database is configured.</summary>
    public static void SkipIfNotConfigured() =>
        Skip.If(
            string.IsNullOrWhiteSpace(ConnectionString),
            $"Set {ConnectionEnvironmentVariable} to a disposable PostgreSQL database to run this test.");

    /// <summary>Creates a context over the disposable database.</summary>
    public static DmoDbContext CreateContext()
    {
        var connectionString = ConnectionString
            ?? throw new InvalidOperationException($"Set {ConnectionEnvironmentVariable} first.");

        var options = new DbContextOptionsBuilder<DmoDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new DmoDbContext(options);
    }

    /// <summary>Applies every pending migration (idempotent; applies 001 then 002 in order).</summary>
    public static Task ApplyMigrationsAsync(DmoDbContext context) =>
        context.Database.MigrateAsync();

    /// <summary>Empties the given table (disposable database only).</summary>
    public static Task ClearTableAsync(DmoDbContext context, string table)
    {
        // The table name is a fixed, test-owned constant; the database is disposable.
#pragma warning disable EF1002
        return context.Database.ExecuteSqlRawAsync($"DELETE FROM \"{table}\"");
#pragma warning restore EF1002
    }

    /// <summary>
    /// Empties <c>admin_accounts</c> so the singleton/bootstrapping tests start from the
    /// canonical state (disposable database only).
    /// </summary>
    public static Task ClearAdminAccountsAsync(DmoDbContext context) =>
        ClearTableAsync(context, "admin_accounts");

    /// <summary>
    /// Empties every phase-foundation table in FK-safe order: users (which may reference
    /// templates) first, then admin_accounts, then templates (cascade into template_modules).
    /// Disposable database only.
    /// </summary>
    public static async Task CleanFoundationTablesAsync(DmoDbContext context)
    {
        await ClearTableAsync(context, "users");
        await ClearAdminAccountsAsync(context);
        await ClearTableAsync(context, "templates");
    }

    /// <summary>
    /// Resets the public schema to empty so the migration mechanism runs from a fresh state
    /// (disposable database only — the schema belongs exclusively to these tests).
    /// </summary>
    public static async Task ResetSchemaAsync(DmoDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("DROP SCHEMA public CASCADE");
        await context.Database.ExecuteSqlRawAsync("CREATE SCHEMA public");
    }
}