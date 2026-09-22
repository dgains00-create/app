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
            IsActive: true,
            TemplateId: null,
            Version: 1);

        try
        {
            var repository = new UserRepository(context);
            await repository.CreatedAsync(account, $"subject-{token}", CancellationToken.None);

            // Reads round-trip the account (including the persisted version).
            var byId = await repository.GetByIdAsync(account.AccountId, CancellationToken.None);
            Assert.NotNull(byId);
            Assert.Equal(account.CompanyNumber, byId!.CompanyNumber);
            Assert.Equal("Reparador", byId.RoleLabel);
            Assert.Null(byId.TemplateId);
            Assert.Equal(1, byId.Version);

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
            IsActive: true,
            TemplateId: null,
            Version: 1);

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

    [SkippableFact]
    public async Task GeneralUpdatedAsync_IndependentObservations_StaleWriterFailsClosedWithoutOverwrite()
    {
        // P1-T03 correction test (review §2 / correction plan §2): the general edit path
        // (<see cref="UserRepository.UpdatedAsync"/>, not SetActive/SetTemplate) must reject a
        // stale caller observation with a typed conflict and must never overwrite the
        // concurrent writer's data. Contexts/repositories are independent so EF change
        // tracking cannot mask the database concurrency boundary.
        PersistenceTestDatabase.SkipIfNotConfigured();

        var token = Guid.NewGuid().ToString("N");
        var account = new UserAccount(
            Guid.NewGuid(),
            CompanyNumber: $"cn-{token}",
            DisplayName: "João Silva",
            Email: $"user-{token}@dmo.test",
            RoleLabel: "Reparador",
            IsActive: true,
            TemplateId: null,
            Version: 1);

        try
        {
            // Context A: create the row and observe it (version 1).
            await using var contextA = PersistenceTestDatabase.CreateContext();
            await PersistenceTestDatabase.ApplyMigrationsAsync(contextA);
            var repositoryA = new UserRepository(contextA);
            await repositoryA.CreatedAsync(account, $"subject-{token}", CancellationToken.None);

            var observedA = await repositoryA.GetByIdAsync(account.AccountId, CancellationToken.None);
            Assert.NotNull(observedA);
            Assert.Equal(1, observedA!.Version);

            // Context B: independent observation of the same row (version 1).
            await using var contextB = PersistenceTestDatabase.CreateContext();
            var repositoryB = new UserRepository(contextB);
            var observedB = await repositoryB.GetByIdAsync(account.AccountId, CancellationToken.None);
            Assert.NotNull(observedB);
            Assert.Equal(1, observedB!.Version);

            // B writes first: success, version becomes 2.
            await repositoryB.UpdatedAsync(
                observedB with { DisplayName = "editado por B" },
                CancellationToken.None);

            // A writes with its stale version-1 observation: rejected with a typed conflict,
            // no field is changed and nothing is overwritten. The rejection comes either from
            // the explicit version compare or from the EF concurrency token catching the race
            // between read/compare and save (both surface the same domain contract).
            var conflict = await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
                repositoryA.UpdatedAsync(
                    observedA with { DisplayName = "edit obsoleta de A" },
                    CancellationToken.None));

            Assert.Contains("reload and retry", conflict.Message, StringComparison.OrdinalIgnoreCase);

            // A second stale attempt from a fresh context deterministically exercises the
            // explicit compare branch: the reloaded row is at version 2, the caller still
            // carries version 1 -> mismatch -> typed conflict raised before any change.
            await using var contextC = PersistenceTestDatabase.CreateContext();
            var repositoryC = new UserRepository(contextC);
            var compareConflict = await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
                repositoryC.UpdatedAsync(
                    observedA with { DisplayName = "edit obsoleta de A 2" },
                    CancellationToken.None));

            Assert.Contains("expected version", compareConflict.Message, StringComparison.OrdinalIgnoreCase);

            // Fresh reload: B's values are preserved and the version remains 2; A's stale
            // edits appear nowhere.
            await using var reloadContext = PersistenceTestDatabase.CreateContext();
            var final = await new UserRepository(reloadContext)
                .GetByIdAsync(account.AccountId, CancellationToken.None);

            Assert.NotNull(final);
            Assert.Equal(2, final!.Version);
            Assert.Equal("editado por B", final.DisplayName);
            Assert.Equal(account.CompanyNumber, final.CompanyNumber);
            Assert.Equal(account.Email, final.Email);
            Assert.Equal(account.RoleLabel, final.RoleLabel);
            Assert.Equal(account.IsActive, final.IsActive);
            Assert.Equal(account.TemplateId, final.TemplateId);
            Assert.NotEqual("edit obsoleta de A", final.DisplayName);
            Assert.NotEqual("edit obsoleta de A 2", final.DisplayName);
        }
        finally
        {
            await using var cleanupContext = PersistenceTestDatabase.CreateContext();
            await cleanupContext.Database.ExecuteSqlRawAsync(
                "DELETE FROM users WHERE company_number = @p OR user_id = @q",
                new Npgsql.NpgsqlParameter("p", $"cn-{token}"),
                new Npgsql.NpgsqlParameter("q", account.AccountId));
        }
    }
}