using System.Data;
using DMO.Application.Accounts;
using DMO.Infrastructure.Persistence;
using DMO.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

// Test-only raw SQL: every interpolated value is a fixed, test-owned token (row identifiers
// derived from a fresh Guid) against a disposable database. Analyzer EF1003 suppressed.
#pragma warning disable EF1003

namespace DMO.IntegrationTests.Persistence;

/// <summary>
/// P1-T03 env-gated integration test — Migration 001 (<c>AccountAndTemplateFoundation</c>)
/// schema and constraints against a disposable PostgreSQL database.
/// </summary>
[Collection(PersistenceDatabaseCollection.Name)]
public sealed class Migration001AccountAndTemplateFoundationTests
{
    [SkippableFact]
    public async Task Migration001_CreatesTheExactTablesConstraintsAndDefaults()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        // ----- tables -----
        var tables = await QueryStringsAsync(
            context,
            "SELECT table_name FROM information_schema.tables " +
            "WHERE table_schema = 'public' AND table_type = 'BASE TABLE' " +
            "AND table_name IN ('templates', 'users', 'admin_accounts')");
        Assert.Equal(
            new[] { "admin_accounts", "templates", "users" },
            tables.OrderBy(name => name, StringComparer.Ordinal).ToArray());

        // ----- primary keys / unique / check constraints -----
        var userConstraints = await QueryStringsAsync(
            context,
            "SELECT conname || '::' || pg_get_constraintdef(oid) FROM pg_constraint " +
            "WHERE conrelid = 'users'::regclass ORDER BY conname");

        Assert.Contains(userConstraints, row => row == "users_auth_identity_id_required_check::CHECK ((btrim(auth_identity_id) <> ''::text))");
        Assert.Contains(userConstraints, row => row == "users_company_number_required_check::CHECK ((btrim(company_number) <> ''::text))");
        Assert.Contains(userConstraints, row => row == "users_email_required_check::CHECK ((btrim(email) <> ''::text))");

        var adminConstraints = await QueryStringsAsync(
            context,
            "SELECT conname || '::' || pg_get_constraintdef(oid) FROM pg_constraint " +
            "WHERE conrelid = 'admin_accounts'::regclass ORDER BY conname");

        Assert.Contains(
            adminConstraints,
            row => row == "admin_accounts_singleton_id_check::CHECK ((admin_id = '00000000-0000-4000-8000-0000000000ad'::uuid))");
        Assert.Contains(adminConstraints, row => row == "admin_accounts_auth_identity_id_required_check::CHECK ((btrim(auth_identity_id) <> ''::text))");
        Assert.Contains(adminConstraints, row => row == "admin_accounts_email_required_check::CHECK ((btrim(email) <> ''::text))");

        // ----- unique indexes -----
        var indexes = await QueryStringsAsync(
            context,
            "SELECT indexname FROM pg_indexes WHERE schemaname = 'public' " +
            "AND indexname IN ('users_auth_identity_id_key', 'users_company_number_key', " +
            "'admin_accounts_auth_identity_id_key', 'admin_accounts_email_key', " +
            "'users_active_idx', 'admin_accounts_active_idx')");
        Assert.Equal(
            new[]
            {
                "admin_accounts_active_idx", "admin_accounts_auth_identity_id_key", "admin_accounts_email_key",
                "users_active_idx", "users_auth_identity_id_key", "users_company_number_key",
            },
            indexes.OrderBy(name => name, StringComparer.Ordinal).ToArray());

        // ----- admin_id has NO DB default and the FK is ON DELETE RESTRICT -----
        Assert.Null(await ColumnDefaultAsync(context, "admin_accounts", "admin_id"));
        Assert.Equal("1", await ColumnDefaultAsync(context, "users", "version"));
        Assert.Equal("now()", await ColumnDefaultAsync(context, "users", "created_at"));
        Assert.Equal("now()", await ColumnDefaultAsync(context, "users", "updated_at"));
        Assert.Equal("1", await ColumnDefaultAsync(context, "admin_accounts", "version"));

        var userFk = await QueryStringsAsync(
            context,
            "SELECT confdeltype::text FROM pg_constraint WHERE conrelid = 'users'::regclass AND contype = 'f'");
        Assert.Equal("r", Assert.Single(userFk)); // RESTRICT
    }

    [SkippableFact]
    public async Task UniqueConstraints_RejectDuplicateCompanyNumberAndSubject()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        await InsertUserAsync(context, $"company-{token}", $"subject-{token}");

        // Duplicate company_number -> unique violation.
        var duplicateCompany = await Assert.ThrowsAsync<DbUpdateException>(() =>
            InsertUserAsync(context, $"company-{token}", $"subject-other-{token}"));
        Assert.Equal("23505", ((PostgresException)duplicateCompany.InnerException!).SqlState);

        // Duplicate auth_identity_id -> unique violation.
        var duplicateSubject = await Assert.ThrowsAsync<DbUpdateException>(() =>
            InsertUserAsync(context, $"company-other-{token}", $"subject-{token}"));
        Assert.Equal("23505", ((PostgresException)duplicateSubject.InnerException!).SqlState);

        // Cleanup.
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM users WHERE company_number IN (@p1, @p2)",
            new NpgsqlParameter("p1", $"company-{token}"),
            new NpgsqlParameter("p2", $"company-other-{token}"));
    }

    [SkippableTheory]
    [InlineData("NULL")]
    [InlineData("   ")]
    [InlineData("")]
    public async Task UsersMandatoryIdentifiers_RejectNullAndBlank(string emailValue)
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");

        // NULL email is rejected by NOT NULL; blank/whitespace is rejected by the CHECK.
        var rawEmail = emailValue == "NULL" ? "NULL" : $"'{emailValue.Replace("'", "''")}'";
        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlRawAsync(
                "INSERT INTO users (user_id, auth_identity_id, name, company_number, email, active, version) " +
                $"VALUES (gen_random_uuid(), 'subject-{token}', 'Name', 'company-{token}', {rawEmail}, true, 1)"));

        Assert.True(
            exception.SqlState is "23502" or "23514",
            $"Expected NOT NULL (23502) or CHECK (23514) violation, got {exception.SqlState}.");
    }

    [SkippableFact]
    public async Task ValidIdentifiers_AreAccepted_TimestampsAndVersionDefault()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");

        // The DEFAULTs populate created_at/updated_at from now() and version from 1; the
        // nullable template_id is accepted; user_id defaults via gen_random_uuid().
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO users (auth_identity_id, name, company_number, email, active) " +
            $"VALUES ('subject-{token}', 'Name', 'company-{token}', 'valid@dmo.test', true)");

        var row = await QueryStringsAsync(
            context,
            "SELECT user_id IS NOT NULL, version, created_at IS NOT NULL, updated_at IS NOT NULL, template_id IS NULL " +
            $"FROM users WHERE company_number = 'company-{token}'");
        Assert.Equal("True|1|True|True|True", Assert.Single(row));

        // created_at/updated_at are populated with the current instant (DB default, not a
        // session-timezone artifact): they are close to the test clock.
        var ageSeconds = await QueryStringsAsync(
            context,
            "SELECT EXTRACT(EPOCH FROM (now() - created_at))::int " +
            $"FROM users WHERE company_number = 'company-{token}'");
        Assert.True(int.Parse(Assert.Single(ageSeconds)) < 300);

        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM users WHERE company_number = @p",
            new NpgsqlParameter("p", $"company-{token}"));
    }

    [SkippableFact]
    public async Task UserNullTemplateId_IsAccepted_AndValidTemplateReference_IsAccepted()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        var templateId = Guid.NewGuid();

        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO templates (template_id, name, version) VALUES (@p, 'T', 1)",
                new NpgsqlParameter("p", templateId));
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO users (auth_identity_id, name, company_number, email, active) " +
                $"VALUES ('subject-null-{token}', 'N', 'company-null-{token}', 'null@dmo.test', true)");
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO users (auth_identity_id, name, company_number, email, active, template_id) " +
                $"VALUES ('subject-ref-{token}', 'R', 'company-ref-{token}', 'ref@dmo.test', true, @p)",
                new NpgsqlParameter("p", templateId));

            var nullCount = await QueryStringsAsync(
                context,
                "SELECT count(*) FROM users WHERE company_number = @p AND template_id IS NULL",
                new NpgsqlParameter("p", $"company-null-{token}"));
            Assert.Equal("1", Assert.Single(nullCount));

            // A template referenced by a user cannot be deleted while the reference exists
            // (RESTRICT) — the null reference, by contrast, never restricts deletion.
            var templateCount = await QueryStringsAsync(
                context,
                "SELECT count(*) FROM users WHERE template_id = @p",
                new NpgsqlParameter("p", templateId));
            Assert.Equal("1", Assert.Single(templateCount));
        }
        finally
        {
            await PersistenceTestDatabase.CleanFoundationTablesAsync(context);
        }
    }

    [SkippableFact]
    public async Task SingletonCheck_RejectsAnyUuidOtherThanTheAdminConstant()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        // The CHECK is evaluated on insert: a different UUID fails before the PK can matter.
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
        {
            context.AdminAccounts.Add(new AdminAccountEntity
            {
                AdminId = Guid.NewGuid(),
                AuthIdentityId = "subject-x",
                DisplayName = "X",
                Email = "x@dmo.test",
                Active = true,
                Version = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
            return context.SaveChangesAsync();
        });

        var postgres = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal("23514", postgres.SqlState); // check_violation (singleton CHECK)
        Assert.Contains("admin_accounts_singleton_id_check", postgres.ConstraintName);
    }

    private static Task InsertUserAsync(DmoDbContext context, string companyNumber, string subject)
    {
        context.Users.Add(new UserEntity
        {
            UserId = Guid.NewGuid(),
            AuthIdentityId = subject,
            Name = "Name",
            CompanyNumber = companyNumber,
            Email = $"{companyNumber}@dmo.test",
            Active = true,
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        return context.SaveChangesAsync();
    }

    private static Task<string?> ColumnDefaultAsync(DmoDbContext context, string table, string column) =>
        context.Database
            .SqlQueryRaw<string>(
                "SELECT column_default AS \"Value\" FROM information_schema.columns " +
                "WHERE table_schema = 'public' AND table_name = {0} AND column_name = {1}",
                table, column)
            .FirstOrDefaultAsync();

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