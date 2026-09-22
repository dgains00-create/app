using DMO.Application.Accounts;
using DMO.Application.Persistence;
using DMO.Application.Repositories;

namespace DMO.UnitTests.UserAdministration.Fakes;

/// <summary>
/// In-memory <see cref="IUserRepository"/> for the administration service tests: enforces the
/// unique company number / unique provider subject rules, bumps versions on every write and
/// compares versions on every versioned write (mirroring the real repository contract), with
/// failure hooks so tests can drive the accepted compensation paths deterministically.
/// </summary>
public sealed class FakeUserRepository : IUserRepository
{
    private sealed class Row
    {
        public UserAccount Account { get; set; } = null!;

        public required string Subject { get; init; }
    }

    private readonly List<Row> _rows = [];

    /// <summary>When set, every create throws this failure (typed persistence failures).</summary>
    public Exception? CreateFailure { get; set; }

    /// <summary>When set, every general update throws this failure.</summary>
    public Exception? UpdateFailure { get; set; }

    /// <summary>
    /// When set, invoked at the start of the versioned delete: tests use it to simulate the
    /// concurrent writer between the delete pre-check and the final row delete (the accepted
    /// delete-race posture).
    /// </summary>
    public Action<Guid>? BeforeVersionedDelete { get; set; }

    /// <summary>All persisted accounts, for assertions.</summary>
    public IReadOnlyList<UserAccount> Accounts => _rows.Select(row => row.Account).ToArray();

    /// <summary>Returns the provider subject mapped to the account, or <c>null</c>.</summary>
    public string? SubjectOf(Guid userId) =>
        _rows.FirstOrDefault(row => row.Account.AccountId == userId)?.Subject;

    /// <inheritdoc />
    public Task<UserAccount?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_rows.FirstOrDefault(row => row.Account.AccountId == userId)?.Account);

    /// <inheritdoc />
    public Task<UserAccount?> GetByCompanyNumberAsync(string companyNumber, CancellationToken cancellationToken) =>
        Task.FromResult(_rows.FirstOrDefault(row => row.Account.CompanyNumber == companyNumber)?.Account);

    /// <inheritdoc />
    public Task<UserAccount?> GetByAuthIdentityAsync(string providerSubject, CancellationToken cancellationToken) =>
        Task.FromResult(_rows.FirstOrDefault(row => row.Subject == providerSubject)?.Account);

    /// <inheritdoc />
    public Task<IReadOnlyList<UserAccount>> ListActiveAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<UserAccount>>(
            _rows.Where(row => row.Account.IsActive).Select(row => row.Account).ToArray());

    /// <inheritdoc />
    public Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<UserAccount>>(
            _rows
                .Select(row => row.Account)
                .OrderBy(account => account.DisplayName)
                .ThenBy(account => account.CompanyNumber)
                .ToArray());

    /// <inheritdoc />
    public Task<IReadOnlyList<UserAccount>> ListByTemplateAsync(Guid templateId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<UserAccount>>(
            _rows
                .Where(row => row.Account.TemplateId == templateId)
                .Select(row => row.Account)
                .OrderBy(account => account.DisplayName)
                .ThenBy(account => account.CompanyNumber)
                .ToArray());

    /// <inheritdoc />
    public Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(_rows.FirstOrDefault(row => row.Account.Email == email)?.Account);

    /// <inheritdoc />
    public Task<string?> GetAuthIdentityIdAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(SubjectOf(userId));

    /// <inheritdoc />
    public Task CreatedAsync(UserAccount account, string authIdentityId, CancellationToken cancellationToken)
    {
        if (CreateFailure is not null)
        {
            throw CreateFailure;
        }

        if (_rows.Any(row => row.Account.CompanyNumber == account.CompanyNumber))
        {
            throw new UserPersistenceException(
                UserPersistenceFailureReason.DuplicateCompanyNumber,
                "Duplicate company number (fake).");
        }

        if (_rows.Any(row => row.Subject == authIdentityId))
        {
            throw new UserPersistenceException(
                UserPersistenceFailureReason.DuplicateProviderSubject,
                "Duplicate provider subject (fake).");
        }

        _rows.Add(new Row { Account = account with { Version = 1 }, Subject = authIdentityId });
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdatedAsync(UserAccount account, CancellationToken cancellationToken)
    {
        if (UpdateFailure is not null)
        {
            throw UpdateFailure;
        }

        var index = FindIndex(account.AccountId);
        var row = _rows[index];

        if (row.Account.Version != account.Version)
        {
            throw new ConcurrencyConflictException(
                $"USER account '{account.AccountId}' was modified concurrently; reload and retry (fake).");
        }

        row.Account = account with { Version = account.Version + 1 };
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetActiveAsync(Guid userId, bool active, int expectedVersion, CancellationToken cancellationToken)
    {
        var index = FindIndex(userId);
        var row = _rows[index];
        EnsureVersion(row, expectedVersion);
        row.Account = row.Account with { IsActive = active, Version = row.Account.Version + 1 };
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetTemplateAsync(Guid userId, Guid? templateId, int expectedVersion, CancellationToken cancellationToken)
    {
        var index = FindIndex(userId);
        var row = _rows[index];
        EnsureVersion(row, expectedVersion);
        row.Account = row.Account with { TemplateId = templateId, Version = row.Account.Version + 1 };
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var index = FindIndex(userId);
        _rows.RemoveAt(index);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid userId, int expectedVersion, CancellationToken cancellationToken)
    {
        BeforeVersionedDelete?.Invoke(userId);

        var row = _rows.FirstOrDefault(candidate => candidate.Account.AccountId == userId);
        if (row is null)
        {
            // Already deleted concurrently: the real repository completes as a no-op.
            return Task.CompletedTask;
        }

        EnsureVersion(row, expectedVersion);
        _rows.Remove(row);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Mirrors the database-layer null-out the atomic delete-with-members performs
    /// (<c>UPDATE users SET template_id = NULL WHERE template_id = @id</c>): every reference
    /// is cleared <b>without</b> a USER version bump — the null-out is not an
    /// optimistic-concurrency write by the USER surface, exactly like the real repository.
    /// </summary>
    public void NullTemplateReferences(Guid templateId)
    {
        foreach (var row in _rows.Where(candidate => candidate.Account.TemplateId == templateId).ToArray())
        {
            var account = row.Account;
            row.Account = account with { TemplateId = null };
        }
    }

    private int FindIndex(Guid userId)
    {
        var index = _rows.FindIndex(row => row.Account.AccountId == userId);
        if (index < 0)
        {
            throw new InvalidOperationException(
                $"USER account '{userId}' was not found (delete or invalid identifier).");
        }

        return index;
    }

    private static void EnsureVersion(Row row, int expectedVersion)
    {
        if (row.Account.Version != expectedVersion)
        {
            throw new ConcurrencyConflictException(
                $"USER account '{row.Account.AccountId}' was modified concurrently " +
                $"(expected version {expectedVersion}, current version {row.Account.Version}); reload and retry.");
        }
    }
}