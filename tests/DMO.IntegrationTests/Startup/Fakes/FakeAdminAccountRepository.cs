using DMO.Application.Accounts;
using DMO.Application.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DMO.IntegrationTests.Startup.Fakes;

/// <summary>
/// Test-only <see cref="IAdminAccountRepository"/> for the bootstrap command tests.
/// </summary>
/// <remarks>
/// Mirrors the database contract: at most one ADMIN row; a create while a row already exists
/// fails exactly like the database final barrier (<see cref="DbUpdateException"/>).
/// </remarks>
public sealed class FakeAdminAccountRepository : IAdminAccountRepository
{
    private AdminAccount? _single;
    private readonly Dictionary<string, AdminAccount> _bySubject = [];
    private readonly Dictionary<string, AdminAccount> _byEmail = [];

    /// <summary>The ADMIN row the fake currently holds (null = absent).</summary>
    public AdminAccount? SingleRow => _single;

    /// <summary>The created row, if any (records the exact create call).</summary>
    public AdminAccount? CreatedRow { get; private set; }

    /// <summary>The provider subject of the latest create call.</summary>
    public string? CreatedSubject { get; private set; }

    /// <summary>Number of create calls.</summary>
    public int CreateCount { get; private set; }

    /// <summary>Seeds the single row with its provider subject.</summary>
    public FakeAdminAccountRepository WithRow(AdminAccount account, string subject)
    {
        _single = account;
        _bySubject[subject] = account;
        _byEmail[account.Email] = account;
        return this;
    }

    /// <inheritdoc />
    public Task<AdminAccount?> GetByAuthIdentityAsync(string providerSubject, CancellationToken cancellationToken)
    {
        _bySubject.TryGetValue(providerSubject, out var match);
        return Task.FromResult(match);
    }

    /// <inheritdoc />
    public Task<AdminAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        _byEmail.TryGetValue(email, out var match);
        return Task.FromResult(match);
    }

    /// <inheritdoc />
    public Task<AdminAccount?> GetSingleAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_single);

    /// <inheritdoc />
    public Task CreatedAsync(AdminAccount account, string authIdentityId, CancellationToken cancellationToken)
    {
        CreateCount++;
        CreatedRow = account;
        CreatedSubject = authIdentityId;

        // Simulate the database final barrier: the fixed admin_id can exist only once.
        if (_single is not null)
        {
            throw new DbUpdateException(
                "Database conflict: the singleton ADMIN row already exists (PK/CHECK).");
        }

        _single = account;
        _byEmail[account.Email] = account;
        _bySubject[authIdentityId] = account;

        return Task.CompletedTask;
    }
}