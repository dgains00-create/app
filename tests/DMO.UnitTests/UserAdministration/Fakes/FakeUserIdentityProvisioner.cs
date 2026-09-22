using DMO.Application.UserAdministration;

namespace DMO.UnitTests.UserAdministration.Fakes;

/// <summary>
/// In-memory <see cref="IUserIdentityProvisioner"/>: records every provider call, owns a
/// small provider-identity store (exact-email lookup) and supports the accepted provider
/// failure/confirmation semantics through deterministic hooks.
/// </summary>
public sealed class FakeUserIdentityProvisioner : IUserIdentityProvisioner
{
    private readonly List<ProviderUser> _providerUsers = [];
    private readonly List<string> _confirmedEmails = [];
    private readonly List<string> _invitedEmails = [];
    private readonly List<string> _deletedSubjects = [];
    private readonly List<(string Subject, string NewEmail)> _updatedEmails = [];
    private readonly List<string> _recoveryEmails = [];
    private int _nextSubject;

    /// <summary>When set, every invite throws this failure.</summary>
    public Exception? InviteFailure { get; set; }

    /// <summary>When set, every email lookup throws this failure (e.g. provider inconsistency fail-closed).</summary>
    public Exception? FindFailure { get; set; }

    /// <summary>When set, every delete throws this failure.</summary>
    public Exception? DeleteFailure { get; set; }

    /// <summary>When set, every email update throws this failure.</summary>
    public Exception? UpdateEmailFailure { get; set; }

    /// <summary>When set, every recovery initiation throws this failure.</summary>
    public Exception? RecoveryFailure { get; set; }

    /// <summary>Invited carrier emails, in order.</summary>
    public IReadOnlyList<string> InvitedEmails => _invitedEmails;

    /// <summary>Deleted provider subjects, in order.</summary>
    public IReadOnlyList<string> DeletedSubjects => _deletedSubjects;

    /// <summary>Email updates as (subject, new email) pairs, in order.</summary>
    public IReadOnlyList<(string Subject, string NewEmail)> UpdatedEmails => _updatedEmails;

    /// <summary>Recovery carrier emails, in order.</summary>
    public IReadOnlyList<string> RecoveryEmails => _recoveryEmails;

    /// <summary>Seeds a provider identity.</summary>
    public void Seed(ProviderUser user) => _providerUsers.Add(user);

    /// <summary>Marks the carrier email as confirmed (invite on a confirmed identity rejects).</summary>
    public void Confirm(string email) => _confirmedEmails.Add(email);

    /// <summary>True when the provider store holds the email.</summary>
    public bool HasEmail(string email) => _providerUsers.Any(user => user.Email == email);

    /// <inheritdoc />
    public Task<ProviderUserInvited> InviteUserAsync(string carrierEmail, string? redirectUrl, CancellationToken cancellationToken)
    {
        _invitedEmails.Add(carrierEmail);

        if (InviteFailure is not null)
        {
            throw InviteFailure;
        }

        var existing = _providerUsers.FirstOrDefault(user => user.Email == carrierEmail);
        if (existing is not null)
        {
            if (_confirmedEmails.Contains(carrierEmail))
            {
                // Confirmed identity: the provider rejects the invite (422 semantics).
                throw new ProviderUserOperationException(
                    ProviderUserOperationFailure.EmailAlreadyInUse,
                    "Provider rejected the invitation (fake, confirmed identity).");
            }

            // Unconfirmed identity: the provider re-sends and returns the same subject.
            return Task.FromResult(new ProviderUserInvited(existing.Subject));
        }

        var subject = $"subject-{++_nextSubject}";
        _providerUsers.Add(new ProviderUser(subject, carrierEmail));
        return Task.FromResult(new ProviderUserInvited(subject));
    }

    /// <inheritdoc />
    public Task<ProviderUser?> GetUserAsync(string subject, CancellationToken cancellationToken) =>
        Task.FromResult(_providerUsers.FirstOrDefault(user => user.Subject == subject));

    /// <inheritdoc />
    public Task<ProviderUser?> FindUserByEmailAsync(string carrierEmail, CancellationToken cancellationToken)
    {
        if (FindFailure is not null)
        {
            throw FindFailure;
        }

        return Task.FromResult(
            _providerUsers.FirstOrDefault(user => user.Email == carrierEmail));
    }

    /// <inheritdoc />
    public Task DeleteUserAsync(string subject, CancellationToken cancellationToken)
    {
        _deletedSubjects.Add(subject);

        if (DeleteFailure is not null)
        {
            throw DeleteFailure;
        }

        _providerUsers.RemoveAll(user => user.Subject == subject);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateUserEmailAsync(string subject, string newCarrierEmail, CancellationToken cancellationToken)
    {
        _updatedEmails.Add((subject, newCarrierEmail));

        if (UpdateEmailFailure is not null)
        {
            throw UpdateEmailFailure;
        }

        var index = _providerUsers.FindIndex(user => user.Subject == subject);
        if (index >= 0)
        {
            _providerUsers[index] = _providerUsers[index] with { Email = newCarrierEmail };
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task InitiatePasswordRecoveryAsync(string carrierEmail, CancellationToken cancellationToken)
    {
        _recoveryEmails.Add(carrierEmail);

        if (RecoveryFailure is not null)
        {
            throw RecoveryFailure;
        }

        return Task.CompletedTask;
    }
}