using DMO.Application.Accounts;
using DMO.Application.Persistence;
using DMO.Application.Templates;
using DMO.Infrastructure.Persistence;
using DMO.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMO.IntegrationTests.Persistence;

/// <summary>
/// P1-T03 env-gated integration test — <see cref="UserRepository"/> primitives and the exact
/// optimistic-concurrency rule (stale version write → typed conflict, no overwrite).
/// </summary>
[Collection(PersistenceDatabaseCollection.Name)]
public sealed class UserRepositoryIntegrationTests
{
    [SkippableFact]
    public async Task CrudPrimitives_RoundTrip_AndCompanyNumberMatchesExactly()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        var account = new UserAccount(
            Guid.NewGuid(),
            CompanyNumber: $"cn-{token}",
            DisplayName: "João Silva",
            Email: $"user-{token}@dmo.test",
            RoleLabel: "Reparador",
            IsActive: true);

        try
        {
            var repository = new UserRepository(context);
            await repository.CreatedAsync(account, $"subject-{token}", CancellationToken.None);

            // Reads round-trip the account.
            var byId = await repository.GetByIdAsync(account.AccountId, CancellationToken.None);
            Assert.NotNull(byId);
            Assert.Equal(account.CompanyNumber, byId!.CompanyNumber);
            Assert.Equal("Reparador", byId.RoleLabel);
            Assert.Null(byId.TemplateId);

            var byCompany = await repository.GetByCompanyNumberAsync($"cn-{token}", CancellationToken.None);
            Assert.Equal(account.AccountId, byCompany!.AccountId);

            // Exact match only: a prefix or case-variant is not the same identifier.
            Assert.Null(await repository.GetByCompanyNumberAsync($"cn-{token}-x", CancellationToken.None));
            Assert.Null(await repository.GetByCompanyNumberAsync($"CN-{token}", CancellationToken.None));

            var bySubject = await repository.GetByAuthIdentityAsync($"subject-{token}", CancellationToken.None);
            Assert.Equal(account.AccountId, bySubject!.AccountId);

            var active = await repository.ListActiveAsync(CancellationToken.None);
            Assert.Contains(active, candidate => candidate.AccountId == account.AccountId);

            // Update persists changed display facts (version increments).
            var updated = account with { DisplayName = "João Silva 2", IsActive = false };
            await repository.UpdatedAsync(updated, CancellationToken.None);
            var reloaded = await repository.GetByIdAsync(account.AccountId, CancellationToken.None);
            Assert.Equal("João Silva 2", reloaded!.DisplayName);
            Assert.False(reloaded.IsActive);
            Assert.DoesNotContain(
                await repository.ListActiveAsync(CancellationToken.None),
                candidate => candidate.AccountId == account.AccountId);

            // Delete removes the row.
            await repository.DeleteAsync(account.AccountId, CancellationToken.None);
            Assert.Null(await repository.GetByIdAsync(account.AccountId, CancellationToken.None));
        }
        finally
        {
            await context.Database.ExecuteSqlRawAsync(
                "DELETE FROM users WHERE company_number = @p OR user_id = @q",
                new Npgsql.NpgsqlParameter("p", $"cn-{token}"),
                new Npgsql.NpgsqlParameter("q", account.AccountId));
        }
    }

    [SkippableFact]
    public async Task StaleVersionWrite_IsRejected_WithoutOverwrite()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);

        var token = Guid.NewGuid().ToString("N");
        var account = new UserAccount(
            Guid.NewGuid(),
            CompanyNumber: $"cn-{token}",
            DisplayName: "João Silva",
            Email: $"user-{token}@dmo.test",
            RoleLabel: "Reparador",
            IsActive: true);

        try
        {
            var repository = new UserRepository(context);
            await repository.CreatedAsync(account, $"subject-{token}", CancellationToken.None);

            // First write against the fresh row (version 1) succeeds.
            await repository.SetActiveAsync(account.AccountId, false, expectedVersion: 1, CancellationToken.None);

            // A second write against the stale observed version (1, now 2) is rejected.
            var conflict = await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
                repository.SetActiveAsync(account.AccountId, true, expectedVersion: 1, CancellationToken.None));

            Assert.Contains("concurrently", conflict.Message, StringComparison.OrdinalIgnoreCase);

            // No overwrite: the row still has the first write's state and version 2.
            var row = await context.Users
                .SingleAsync(user => user.UserId == account.AccountId);
            Assert.False(row.Active);
            Assert.Equal(2, row.Version);

            // Template reassignment follows the same rule.
            var templateId = Guid.NewGuid();
            context.Templates.Add(new TemplateEntity
            {
                TemplateId = templateId,
                Name = $"tpl-{token}",
                Version = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
            await context.SaveChangesAsync();

            await repository.SetTemplateAsync(account.AccountId, templateId, expectedVersion: 2, CancellationToken.None);
            var updated = await repository.GetByIdAsync(account.AccountId, CancellationToken.None);
            Assert.Equal(templateId, updated!.TemplateId);

            await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
                repository.SetTemplateAsync(account.AccountId, null, expectedVersion: 2, CancellationToken.None));
        }
        finally
        {
            await context.Database.ExecuteSqlRawAsync(
                "DELETE FROM users WHERE company_number = @p OR user_id = @q",
                new Npgsql.NpgsqlParameter("p", $"cn-{token}"),
                new Npgsql.NpgsqlParameter("q", account.AccountId));
            await context.Database.ExecuteSqlRawAsync(
                "DELETE FROM templates WHERE name LIKE '%' || @t || '%'",
                new Npgsql.NpgsqlParameter("t", token));
        }
    }
}