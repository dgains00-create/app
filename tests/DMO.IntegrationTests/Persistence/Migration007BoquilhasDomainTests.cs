using System.Data;
using System.Text.RegularExpressions;
using DMO.Infrastructure.Persistence;
using DMO.IntegrationTests.JobOn;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

// Test-only raw SQL: every interpolated value is a fixed, test-owned token (table names and row
// identifiers) against a disposable database. Analyzer EF1003 suppressed.
#pragma warning disable EF1003

namespace DMO.IntegrationTests.Persistence;

/// <summary>
/// P2-T07 env-gated integration test — Migration 007 (<c>BoquilhasDomain</c>): exactly the six new
/// tables with the exact columns/CHECKs/RESTRICT FKs/indexes of §6/§7 (incl. the two ACTIVE partial
/// unique indexes), the exact <c>Down</c> behaviour and the no-drift re-apply, against a disposable
/// PostgreSQL database.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §28 (migration contract: one additive migration, the seventh overall;
/// §7.2/§7.5 the partial unique indexes) and §29 rows MG1–MG3 (AC-MG1…AC-MG3). Every row is
/// <c>[SkippableFact]</c> behind <see cref="PersistenceTestDatabase.SkipIfNotConfigured"/>.</remarks>
[Collection(PersistenceDatabaseCollection.Name)]
public sealed class Migration007BoquilhasDomainTests
{
    /// <summary>EF's own migration bookkeeping table (never a product table).</summary>
    private const string MigrationHistoryTable = "__EFMigrationsHistory";

    /// <summary>The seventh migration (this slice owns it).</summary>
    private const string BoquilhasMigrationId = "20260924031924_BoquilhasDomain";

    /// <summary>The migration the seventh applies ON TOP of (the Down target).</summary>
    private const string ControloApproveMigrationId = "20260923171223_ControloApproveDomain";

    /// <summary>The six contracted Boquilhas tables.</summary>
    private static readonly string[] SixTables =
    [
        "boquilha_close_snapshots",
        "boquilha_machines",
        "boquilha_movement_audit",
        "boquilha_movements",
        "boquilha_reopenings",
        "boquilhas",
    ];

    /// <summary>The seven migrations, in generation order.</summary>
    private static readonly string[] AllMigrationIds =
    [
        "20260922001736_AccountAndTemplateFoundation",
        "20260922001757_TemplateModuleComposition",
        "20260922232349_ToolJobOnDomainCore",
        "20260923045054_ControloCreateDomain",
        "20260923122429_GlassDensitySettings",
        ControloApproveMigrationId,
        BoquilhasMigrationId,
    ];

    /// <summary>
    /// MG1 (AC-MG1) — applying all seven migrations to a reset schema leaves exactly the seven
    /// contracted migrations in <c>__EFMigrationsHistory</c> and exactly the 27 public product
    /// tables; the slice adds exactly SIX tables to the closed 21-table state.
    /// </summary>
    [SkippableFact]
    public async Task MG1_ExactlySixNewTablesAndTheSeventhMigrationAreApplied()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ResetSchemaAsync(context);
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var history = Sorted(await QueryStringsAsync(context, "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\""));
        Assert.Equal(Sorted(AllMigrationIds.ToList()), history);

        var tables = Sorted(await QueryStringsAsync(
            context,
            "SELECT table_name FROM information_schema.tables " +
            "WHERE table_schema = 'public' AND table_type = 'BASE TABLE'"));

        Assert.Equal(27, tables.Count); // 21 closed tables + exactly six Boquilhas tables
        foreach (var table in SixTables)
        {
            Assert.Contains(table, tables);
        }
    }

    /// <summary>
    /// MG1 (AC-MG1) — the migration source creates exactly the six contracted tables and NO other
    /// table/column/statement; <c>Down</c> drops exactly those six in a referentially safe order.
    /// </summary>
    [SkippableFact]
    public async Task MG1_TheMigrationSourceCreatesExactlyTheSixContractedTables()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        var source = Framework_Read("src/DMO.Infrastructure/Migrations/20260924031924_BoquilhasDomain.cs");

        var created = Regex.Matches(source, @"CreateTable\(\s*name:\s*""(?<name>[a-z_]+)""")
            .Select(match => match.Groups["name"].Value)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
        Assert.Equal(Sorted(SixTables.ToList()), created);

        var dropped = Regex.Matches(source, @"DropTable\(\s*name:\s*""(?<name>[a-z_]+)""")
            .Select(match => match.Groups["name"].Value)
            .ToList();
        Assert.Equal(SixTables.Length, dropped.Count);
        Assert.Equal(Sorted(SixTables.ToList()), Sorted(dropped));

        Assert.DoesNotContain("AddColumn", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DropColumn", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RenameTable", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InsertData", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateSequence", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// MG1 (AC-MG1) — the two ACTIVE partial unique indexes exist with the EXACT predicates:
    /// <c>WHERE status = 'active' AND bq_id IS NOT NULL</c> / <c>WHERE status = 'active' AND
    /// tool_id IS NOT NULL</c> (§7.2, B1 correction).
    /// </summary>
    [SkippableFact]
    public async Task MG1_TheTwoActivePartialUniqueIndexesExistWithTheExactPredicates()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ResetSchemaAsync(context);
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var indexes = await QueryStringsAsync(
            context,
            "SELECT indexname, indexdef FROM pg_indexes WHERE tablename = 'boquilhas'");

        var bq = indexes.FirstOrDefault(line => line.StartsWith("IX_boquilhas_active_bq_id|", StringComparison.Ordinal));
        Assert.NotNull(bq);
        Assert.Contains("CREATE UNIQUE INDEX", bq, StringComparison.Ordinal);
        Assert.Contains("(bq_id)", bq, StringComparison.Ordinal);
        Assert.Contains("(status = 'active'::text) AND (bq_id IS NOT NULL)", bq, StringComparison.Ordinal);

        var tool = indexes.FirstOrDefault(line => line.StartsWith("IX_boquilhas_active_tool_id|", StringComparison.Ordinal));
        Assert.NotNull(tool);
        Assert.Contains("CREATE UNIQUE INDEX", tool, StringComparison.Ordinal);
        Assert.Contains("(tool_id)", tool, StringComparison.Ordinal);
        Assert.Contains("(status = 'active'::text) AND (tool_id IS NOT NULL)", tool, StringComparison.Ordinal);

        // The two contracted FK-supporting/traversal indexes are present as well (§7.5).
        Assert.Contains(indexes, line => line.StartsWith("IX_boquilhas_bq_id|", StringComparison.Ordinal));
        Assert.Contains(indexes, line => line.StartsWith("IX_boquilhas_tool_id|", StringComparison.Ordinal));
    }

    /// <summary>
    /// MG2 (AC-MG2) — the six tables' columns/CHECKs/FKs match §6/§7 exactly: the exact constraint
    /// names, <c>confdeltype='r'</c> (no cascade), the anchor-exclusive CHECK.
    /// </summary>
    [SkippableFact]
    public async Task MG2_TheBoquilhasConstraintsMatchTheContractExactly()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ResetSchemaAsync(context);
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        // Columns of boquilhas (exact set, exact nullability).
        var boquilhasColumns = await QueryStringsAsync(
            context,
            "SELECT column_name, is_nullable FROM information_schema.columns " +
            "WHERE table_schema = 'public' AND table_name = 'boquilhas'");
        var expectedBoquilhasColumns = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["boquilhas_id"] = false,
            ["bq_id"] = true,
            ["tool_id"] = true,
            ["status"] = false,
            ["opening_date"] = false,
            ["utilisation_percent"] = true,
            ["observations"] = true,
            ["created_by_user_id"] = false,
            ["version"] = false,
            ["created_at"] = false,
            ["updated_at"] = false,
        };
        var expectedColumns = expectedBoquilhasColumns
            .Select(entry => $"{entry.Key}|{(entry.Value ? "YES" : "NO")}")
            .ToList();
        Assert.Equal(
            Sorted(expectedColumns),
            Sorted(boquilhasColumns));

        // CHECK constraints of boquilhas.
        var boquilhasChecks = await QueryStringsAsync(
            context,
            "SELECT conname FROM pg_constraint WHERE conrelid = 'boquilhas'::regclass AND contype = 'c'");
        foreach (var expected in new[]
                 {
                     "boquilhas_anchor_exclusive_check",
                     "boquilhas_status_check",
                     "boquilhas_utilisation_range_check",
                     "boquilhas_observations_check",
                 })
        {
            Assert.Contains(expected, boquilhasChecks);
        }

        Assert.Equal(4, boquilhasChecks.Count);

        // All fourteen FKs are RESTRICT (confdeltype 'r'), never cascade.
        var fks = await QueryStringsAsync(
            context,
            "SELECT conrelid::regclass::text, conname, confdeltype FROM pg_constraint " +
            "WHERE contype = 'f' AND conrelid IN ('boquilhas'::regclass, 'boquilha_machines'::regclass, " +
            "'boquilha_movements'::regclass, 'boquilha_movement_audit'::regclass, " +
            "'boquilha_close_snapshots'::regclass, 'boquilha_reopenings'::regclass)");
        Assert.Equal(14, fks.Count);
        Assert.All(fks, line => Assert.EndsWith("|r", line, StringComparison.Ordinal));

        // The machine-set unique key (EF creates a UNIQUE INDEX, never a constraint-backed UQ).
        var uniqueKeys = await QueryStringsAsync(
            context,
            "SELECT indexname FROM pg_indexes WHERE tablename = 'boquilha_machines' " +
            "AND indexdef ILIKE '%UNIQUE%'");
        Assert.Contains("boquilha_machines_boquilhas_machine_key", uniqueKeys);
    }

    /// <summary>
    /// MG2 (AC-MG2) — direct-violation inserts map 23514 (the accepted backstop, never a 500): the
    /// anchor CHECK (both anchors / none), the movement-type CHECK (a fifth type) and the reopen
    /// reason CHECK.
    /// </summary>
    [SkippableFact]
    public async Task MG2_DirectViolationsAreSqlState23514()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ResetSchemaAsync(context);
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var connection = (NpgsqlConnection)context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        // Anchor CHECK: both anchors violate the exclusive-anchor rule (23514).
        await AssertThrowsSqlStateAsync(
            connection,
            "INSERT INTO boquilhas (boquilhas_id, bq_id, tool_id, status, opening_date, created_by_user_id, version) " +
            "VALUES (@id, @bq, @tool, 'active', '2026-09-10', @user, 1)",
            ("id", (object)Guid.NewGuid()),
            ("bq", (object)Guid.NewGuid()),
            ("tool", (object)Guid.NewGuid()),
            ("user", (object)Guid.NewGuid()));

        // Anchor CHECK: no anchor at all (23514).
        await AssertThrowsSqlStateAsync(
            connection,
            "INSERT INTO boquilhas (boquilhas_id, bq_id, tool_id, status, opening_date, created_by_user_id, version) " +
            "VALUES (@id, NULL, NULL, 'active', '2026-09-10', @user, 1)",
            ("id", (object)Guid.NewGuid()),
            ("user", (object)Guid.NewGuid()));

        // Movement-type CHECK: a fifth type (23514).
        await AssertThrowsSqlStateAsync(
            connection,
            "INSERT INTO boquilha_movements (movement_id, boquilhas_id, movement_type, quantity, business_date, " +
            "recorded_at, recorded_by_user_id, version) " +
            "VALUES (@id, @bq, 'contagem', 1, '2026-09-10', now(), @user, 1)",
            ("id", (object)Guid.NewGuid()),
            ("bq", (object)Guid.NewGuid()),
            ("user", (object)Guid.NewGuid()));

        // Reopen reason CHECK: blank reason (23514).
        await AssertThrowsSqlStateAsync(
            connection,
            "INSERT INTO boquilha_reopenings (reopen_id, boquilhas_id, close_snapshot_id, reopened_by_user_id, " +
            "reopened_at, reason) VALUES (@id, @bq, @close, @user, now(), '')",
            ("id", (object)Guid.NewGuid()),
            ("bq", (object)Guid.NewGuid()),
            ("close", (object)Guid.NewGuid()),
            ("user", (object)Guid.NewGuid()));
    }

    /// <summary>
    /// MG3 (AC-MG3) — the REAL <c>Down</c> (EF migrator to the previous migration) removes ONLY the
    /// P2-T07 state — the 21-table state is restored; re-applying is idempotent.
    /// </summary>
    [SkippableFact]
    public async Task MG3_DownRemovesOnlyP2T07StateAndReApplyIsIdempotent()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ResetSchemaAsync(context);
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        // The REAL EF migrator applies migration 007's Down (back to the P2-T06 migration).
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(ControloApproveMigrationId);

        var tables = await QueryStringsAsync(
            context,
            "SELECT table_name FROM information_schema.tables " +
            "WHERE table_schema = 'public' AND table_type = 'BASE TABLE'");
        // Physical raw count: 20 pre-P2-T07 product tables + __EFMigrationsHistory = 21 after
        // Down (the contract's "21 current" narrative counts product tables only; the physical
        // product count is 20 before P2-T07 — recorded in the implementation response).
        Assert.Equal(21, tables.Count);
        foreach (var table in SixTables)
        {
            Assert.DoesNotContain(table, tables);
        }

        var history = await QueryStringsAsync(context, "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\"");
        Assert.DoesNotContain(history, line => line == BoquilhasMigrationId);
        Assert.Contains(history, line => line == ControloApproveMigrationId);

        // Re-apply forward and again — idempotent (26 product tables + history = 27 raw).
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var reapplied = await QueryStringsAsync(
            context,
            "SELECT table_name FROM information_schema.tables " +
            "WHERE table_schema = 'public' AND table_type = 'BASE TABLE'");
        Assert.Equal(27, reapplied.Count);
    }

    // ------------------------------------------------------------------ helpers

    private static string Framework_Read(string path) => P2T04ProductionScan.Read(path);

    private static List<string> Sorted(List<string> values)
    {
        values.Sort(StringComparer.Ordinal);
        return values;
    }

    private static async Task<List<string>> QueryStringsAsync(DmoDbContext context, string sql)
    {
        var rows = new List<string>();
        var connection = context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var values = new string[reader.FieldCount];
            for (var index = 0; index < reader.FieldCount; index++)
            {
                values[index] = reader.GetValue(index)?.ToString() ?? string.Empty;
            }

            rows.Add(string.Join('|', values));
        }

        return rows;
    }

    private static async Task AssertThrowsSqlStateAsync(
        NpgsqlConnection connection,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.Add(new NpgsqlParameter(name, value));
        }

        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal("23514", exception.SqlState);
    }
}