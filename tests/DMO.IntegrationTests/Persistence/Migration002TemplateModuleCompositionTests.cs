using System.Data;
using DMO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

// Test-only raw SQL: every interpolated value is a fixed, test-owned token (row identifiers
// derived from a fresh Guid) against a disposable database. Analyzer EF1003 suppressed.
#pragma warning disable EF1003

namespace DMO.IntegrationTests.Persistence;

/// <summary>
/// P1-T03 env-gated integration test — Migration 002 (<c>TemplateModuleComposition</c>)
/// schema and constraints against a disposable PostgreSQL database.
/// </summary>
[Collection(PersistenceDatabaseCollection.Name)]
public sealed class Migration002TemplateModuleCompositionTests
{
    [SkippableFact]
    public async Task Migration002_CreatesTheExactCompositionTable()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        // The table exists with the exact composite primary key.
        var primaryKey = await QueryStringsAsync(
            context,
            "SELECT conname || '::' || pg_get_constraintdef(oid) FROM pg_constraint " +
            "WHERE conrelid = 'template_modules'::regclass AND contype = 'p'");
        Assert.Equal(
            "PK_template_modules::PRIMARY KEY (template_id, module_id)",
            Assert.Single(primaryKey));

        // The FK to templates is ON DELETE CASCADE.
        var foreignKeys = await QueryStringsAsync(
            context,
            "SELECT 'fk:' || a.attname || ':' || confdeltype::text FROM pg_constraint c " +
            "JOIN unnest(c.conkey) WITH ORDINALITY AS k(attnum, ord) ON true " +
            "JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = k.attnum " +
            "WHERE c.conrelid = 'template_modules'::regclass AND c.contype = 'f'");
        Assert.Equal("fk:template_id:c", Assert.Single(foreignKeys)); // cascade

        // The unique ordering index exists on (template_id, presentation_order).
        var orderIndex = await QueryStringsAsync(
            context,
            "SELECT indexdef FROM pg_indexes WHERE schemaname = 'public' " +
            "AND indexname = 'template_modules_template_order_key'");
        Assert.Contains("UNIQUE", Assert.Single(orderIndex), StringComparison.Ordinal);
        Assert.Contains("template_id", Assert.Single(orderIndex), StringComparison.Ordinal);

        // module_id is plain text: it has no FK anywhere (code-defined identity, not a link).
        var moduleFkColumns = await QueryStringsAsync(
            context,
            "SELECT a.attname FROM pg_constraint c " +
            "JOIN unnest(c.conkey) AS fk_attnum(attnum) ON true " +
            "JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = fk_attnum.attnum " +
            "WHERE c.conrelid = 'template_modules'::regclass AND c.contype = 'f'");
        Assert.Equal(new[] { "template_id" }, moduleFkColumns);
    }

    [SkippableFact]
    public async Task DuplicateTemplateModulePair_IsRejected()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        var templateId = Guid.NewGuid();
        await InsertTemplateAsync(context, templateId, $"tpl-{token}");

        try
        {
            await InsertModuleAsync(context, templateId, "module-core", 1);

            // Same (template_id, module_id) pair -> primary key violation.
            var duplicate = await Assert.ThrowsAsync<PostgresException>(() =>
                InsertModuleAsync(context, templateId, "module-core", 2));
            Assert.Equal("23505", duplicate.SqlState);
        }
        finally
        {
            await PersistenceTestDatabase.CleanFoundationTablesAsync(context);
        }
    }

    [SkippableFact]
    public async Task DuplicatePresentationOrder_PerTemplate_IsRejected()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        var templateId = Guid.NewGuid();
        await InsertTemplateAsync(context, templateId, $"tpl-{token}");

        try
        {
            await InsertModuleAsync(context, templateId, "module-a", 5);

            // Same presentation_order for the same Template -> unique ordering violation.
            var duplicate = await Assert.ThrowsAsync<PostgresException>(() =>
                InsertModuleAsync(context, templateId, "module-b", 5));
            Assert.Equal("23505", duplicate.SqlState);

            // The same order may be reused by a different Template (deterministic per Template).
            var otherTemplateId = Guid.NewGuid();
            await InsertTemplateAsync(context, otherTemplateId, $"tpl-other-{token}");
            await InsertModuleAsync(context, otherTemplateId, "module-c", 5);
            await context.Database.ExecuteSqlRawAsync(
                "DELETE FROM templates WHERE template_id = @p",
                new NpgsqlParameter("p", otherTemplateId));
        }
        finally
        {
            await PersistenceTestDatabase.CleanFoundationTablesAsync(context);
        }
    }

    [SkippableFact]
    public async Task TemplateDeletion_CascadesToCompositionOnly()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        var templateId = Guid.NewGuid();
        await InsertTemplateAsync(context, templateId, $"tpl-{token}");
        await InsertModuleAsync(context, templateId, "module-a", 1);
        await InsertModuleAsync(context, templateId, "module-b", 2);

        // Deleting the Template cascades into template_modules (composition rows disappear).
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM templates WHERE template_id = @p",
            new NpgsqlParameter("p", templateId));

        var remaining = await QueryStringsAsync(
            context,
            "SELECT count(*) FROM template_modules WHERE template_id = @p",
            new NpgsqlParameter("p", templateId));
        Assert.Equal("0", Assert.Single(remaining));

        // Cleanup.
        await PersistenceTestDatabase.CleanFoundationTablesAsync(context);
    }

    private static Task InsertTemplateAsync(DmoDbContext context, Guid templateId, string name)
    {
        context.Database.ExecuteSqlRaw(
            "INSERT INTO templates (template_id, name, version) VALUES (@p, @n, 1)",
            new NpgsqlParameter("p", templateId),
            new NpgsqlParameter("n", name));
        return Task.CompletedTask;
    }

    private static Task InsertModuleAsync(DmoDbContext context, Guid templateId, string moduleId, int order)
    {
        context.Database.ExecuteSqlRaw(
            "INSERT INTO template_modules (template_id, module_id, presentation_order) VALUES (@t, @m, @o)",
            new NpgsqlParameter("t", templateId),
            new NpgsqlParameter("m", moduleId),
            new NpgsqlParameter("o", order));
        return Task.CompletedTask;
    }

    private static async Task<IReadOnlyList<string>> QueryStringsAsync(
        DmoDbContext context,
        string sql,
        params NpgsqlParameter[] parameters)
    {
        var results = new List<string>();
        var connection = context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var values = new string[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                values[i] = reader.GetValue(i)?.ToString() ?? string.Empty;
            }

            results.Add(string.Join("|", values));
        }

        return results;
    }
}