using DMO.Application.Accounts;
using DMO.Application.Repositories;
using DMO.IntegrationTests.Startup.Fakes;
using DMO.Web.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DMO.IntegrationTests.Startup;

/// <summary>
/// P1-T03 tests — the <c>bootstrap-admin</c> command detection and the
/// <see cref="AdminBootstrapCommand"/> handler (idempotency, conflict, configuration) with
/// fakes; no database and no provider call.
/// </summary>
public sealed class AdminBootstrapCommandTests
{
    [Theory]
    [InlineData("bootstrap-admin")]
    [InlineData("BOOTSTRAP-ADMIN")]
    [InlineData("--bootstrap-admin")]
    [InlineData("--BOOTSTRAP-ADMIN")]
    public void IsBootstrapAdminCommand_WhenFirstArgument_IsTrue(string argument)
    {
        Assert.True(StartupCommands.IsBootstrapAdminCommand([argument]));
    }

    [Theory]
    [InlineData("migrate")]
    [InlineData("--migrate")]
    [InlineData("")]
    [InlineData("--environment", "Production")]
    public void IsBootstrapAdminCommand_WhenNotFirst_IsFalse(params string[] args)
    {
        Assert.False(StartupCommands.IsBootstrapAdminCommand(args));
        Assert.False(StartupCommands.IsBootstrapAdminCommand([]));
        Assert.False(StartupCommands.IsBootstrapAdminCommand(null));
    }

    [Fact]
    public async Task MissingConfiguration_FailsWithoutTouchingTheRepository()
    {
        var repository = new FakeAdminAccountRepository();
        var command = CommandWith(repository, email: null, "DMO Admin", "subject-1");

        var result = await command.ExecuteAsync(CancellationToken.None);

        Assert.Equal(StartupCommands.FailureExitCode, result);
        Assert.Equal(0, repository.CreateCount);
        Assert.Null(repository.SingleRow);
    }

    [Fact]
    public async Task AbsentRow_InsertsUsingTheFixedAdminAccountId()
    {
        var repository = new FakeAdminAccountRepository();
        var command = CommandWith(repository, "admin@dmo.test", "DMO Admin", "subject-1");

        var result = await command.ExecuteAsync(CancellationToken.None);

        Assert.Equal(StartupCommands.SuccessExitCode, result);
        Assert.Equal(1, repository.CreateCount);
        Assert.NotNull(repository.CreatedRow);
        Assert.Equal(AdminAccountId.Value, repository.CreatedRow!.AccountId);
        Assert.Equal("admin@dmo.test", repository.CreatedRow.Email);
        Assert.Equal("DMO Admin", repository.CreatedRow.DisplayName);
        Assert.True(repository.CreatedRow.IsActive);
        Assert.Equal("subject-1", repository.CreatedSubject);
    }

    [Fact]
    public async Task SameEmailAndSameSubject_IsAnIdempotentNoOp()
    {
        var existing = new AdminAccount(AdminAccountId.Value, "DMO Admin", "admin@dmo.test", IsActive: true);
        var repository = new FakeAdminAccountRepository().WithRow(existing, "subject-1");
        var command = CommandWith(repository, "admin@dmo.test", "DMO Admin", "subject-1");

        var result = await command.ExecuteAsync(CancellationToken.None);

        Assert.Equal(StartupCommands.SuccessExitCode, result);
        Assert.Equal(0, repository.CreateCount);
        Assert.Same(existing, repository.SingleRow);
    }

    [Fact]
    public async Task DifferentSubject_FailsExplicitly_WithoutOverwriting()
    {
        var existing = new AdminAccount(AdminAccountId.Value, "DMO Admin", "admin@dmo.test", IsActive: true);
        var repository = new FakeAdminAccountRepository().WithRow(existing, "subject-1");
        var command = CommandWith(repository, "admin@dmo.test", "DMO Admin", "subject-2");

        var result = await command.ExecuteAsync(CancellationToken.None);

        Assert.Equal(StartupCommands.FailureExitCode, result);
        Assert.Equal(0, repository.CreateCount);
        Assert.Same(existing, repository.SingleRow);
    }

    [Fact]
    public async Task DifferentEmail_FailsExplicitly_WithoutOverwriting()
    {
        var existing = new AdminAccount(AdminAccountId.Value, "DMO Admin", "admin@dmo.test", IsActive: true);
        var repository = new FakeAdminAccountRepository().WithRow(existing, "subject-1");
        var command = CommandWith(repository, "other@dmo.test", "Other", "subject-1");

        var result = await command.ExecuteAsync(CancellationToken.None);

        Assert.Equal(StartupCommands.FailureExitCode, result);
        Assert.Equal(0, repository.CreateCount);
        Assert.Same(existing, repository.SingleRow);
    }

    [Fact]
    public async Task RacingConcurrentInsert_FailsCleanlyAtTheDatabaseBarrier()
    {
        // Two concurrent runs can both observe "absent" before either insert commits. The
        // repository must then reject the second insert — the command catches the database
        // conflict and fails cleanly (failure exit code, no partial state) while the
        // invariant stays intact. The fake simulates the database final barrier: reads say
        // absent, but every create is rejected as a duplicate.
        var repository = new DatabaseBarrierRepository();
        var command = CommandWith(repository, "admin@dmo.test", "DMO Admin", "subject-1");

        var result = await command.ExecuteAsync(CancellationToken.None);

        Assert.Equal(StartupCommands.FailureExitCode, result);
        Assert.Equal(1, repository.CreateAttempts);
        Assert.Null(repository.SingleRow);
    }

    private static AdminBootstrapCommand CommandWith(
        IAdminAccountRepository repository,
        string? email,
        string? displayName,
        string? authIdentityId) =>
        new(
            repository,
            Options.Create(new AdminBootstrapOptions
            {
                Email = email,
                DisplayName = displayName,
                AuthIdentityId = authIdentityId,
            }),
            NullLogger<AdminBootstrapCommand>.Instance);

    /// <summary>
    /// Simulates the concurrent-bootstrap database barrier: every read observes "absent" but
    /// every create is rejected as a duplicate (PK/CHECK), exactly like a second concurrent
    /// run inserting the fixed admin_id.
    /// </summary>
    private sealed class DatabaseBarrierRepository : IAdminAccountRepository
    {
        /// <summary>Number of create attempts.</summary>
        public int CreateAttempts { get; private set; }

        /// <summary>No row is ever created through this fake.</summary>
        public AdminAccount? SingleRow => null;

        public Task<AdminAccount?> GetByAuthIdentityAsync(string providerSubject, CancellationToken cancellationToken) =>
            Task.FromResult<AdminAccount?>(null);

        public Task<AdminAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult<AdminAccount?>(null);

        public Task<AdminAccount?> GetSingleAsync(CancellationToken cancellationToken) =>
            Task.FromResult<AdminAccount?>(null);

        public Task CreatedAsync(AdminAccount account, string authIdentityId, CancellationToken cancellationToken)
        {
            CreateAttempts++;
            throw new DbUpdateException(
                "Database conflict: the singleton ADMIN row already exists (PK/CHECK).");
        }
    }
}