using DMO.Application.Accounts;
using DMO.Infrastructure.Persistence;
using DMO.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DMO.IntegrationTests.Persistence;

/// <summary>
/// P1-T03 env-gated integration test — the required single-ADMIN database invariant:
/// CHECK (all rows must use the fixed <see cref="AdminAccountId"/>) + PK (that UUID only
/// once) = at most one ADMIN row.
/// </summary>
/// <remarks>
/// The four required steps, executed against the disposable database:
/// <list type="number">
/// <item>insert <see cref="AdminAccountId"/> → success;</item>
/// <item>insert another UUID with different email/subject → DB rejects via the singleton CHECK;</item>
/// <item>insert <see cref="AdminAccountId"/> again with different email/subject → DB rejects via the PK;</item>
/// <item>final <c>admin_accounts</c> row count → 1.</item>
/// </list>
/// Each step uses a fresh context so the database — not the EF change tracker — decides.
/// </remarks>
[Collection(PersistenceDatabaseCollection.Name)]
public sealed class AdminSingletonInvariantTests
{
    [SkippableFact]
    public async Task SingletonInvariant_IsEnforcedByCheckAndPrimaryKey()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        try
        {
            // 1. Insert the fixed AdminAccountId -> success.
            await InsertAdminAsync(AdminAccountId.Value, "step1@dmo.test", "step1-subject");

            // 2. Insert ANOTHER UUID with different email and different subject -> the DB
            // rejects via the singleton CHECK (check_violation), regardless of the PK.
            var step2 = await AssertThrowsPostgresAsync(() =>
                InsertAdminAsync(Guid.NewGuid(), "step2@dmo.test", "step2-subject"));
            Assert.Equal("23514", step2.SqlState);
            Assert.Contains("admin_accounts_singleton_id_check", step2.ConstraintName);

            // 3. Insert the fixed AdminAccountId AGAIN with different email/subject -> the DB
            // rejects via the primary key (unique_violation). The second instance in the same
            // session is a NEW row (fresh EF context); only the database knows it collides.
            var step3 = await AssertThrowsPostgresAsync(() =>
                InsertAdminAsync(AdminAccountId.Value, "step3@dmo.test", "step3-subject"));
            Assert.Equal("23505", step3.SqlState);

            // 4. Final row count is exactly one.
            await using var verifyContext = PersistenceTestDatabase.CreateContext();
            var count = await verifyContext.AdminAccounts.CountAsync();
            Assert.Equal(1, count);

            var row = await verifyContext.AdminAccounts.SingleAsync();
            Assert.Equal(AdminAccountId.Value, row.AdminId);
            Assert.Equal("step1@dmo.test", row.Email);
            Assert.Equal("step1-subject", row.AuthIdentityId);
        }
        finally
        {
            await PersistenceTestDatabase.ClearAdminAccountsAsync(context);
        }
    }

    private static async Task InsertAdminAsync(Guid adminId, string email, string subject)
    {
        await using var context = PersistenceTestDatabase.CreateContext();

        context.AdminAccounts.Add(new AdminAccountEntity
        {
            AdminId = adminId,
            AuthIdentityId = subject,
            DisplayName = "X",
            Email = email,
            Active = true,
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync();
    }

    private static async Task<PostgresException> AssertThrowsPostgresAsync(Func<Task> action)
    {
        var exception = await Assert.ThrowsAsync<DbUpdateException>(action);
        return Assert.IsType<PostgresException>(exception.InnerException);
    }
}