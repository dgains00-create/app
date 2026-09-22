using DMO.Application.Accounts;
using DMO.Application.Persistence;
using DMO.Application.Repositories;

namespace DMO.Application.UserAdministration;

/// <summary>
/// P1-T05 USER administration orchestration: validation → application persistence primitives
/// ↔ privileged provider provisioning, with the accepted compensation/idempotency posture.
/// </summary>
/// <remarks>
/// <para>
/// <b>Create</b> (invite flow only): validate → app-side pre-checks (company number, carrier
/// email) → provider email lookup (proves whether an identity pre-exists) → invite (when the
/// carrier is provably free) → persist with the provider subject. DB failure after a fresh
/// invite compensates by deleting <b>only</b> the identity provably created by this operation
/// (never one referenced by an application USER). A provider identity with an exact-match
/// carrier email and no application row is the provably orphaned provisioning adopted back.
/// </b>IdempotentReplay</b> is returned only when the existing row provably equals the same
/// completed logical create result (Correction D).
/// </para>
/// <para>
/// <b>Delete</b>: non-destructive version pre-check (no provider call, no DB delete on
/// mismatch) → provider identity delete (404 = success) → version-checked row delete. The race
/// between pre-check and final row delete is a <b>known recoverable partial state</b>: the row
/// survives with its provider linkage intact (retry anchor) and the result is never success.
/// </para>
/// <para>
/// <b>Email edit</b> coordinates provider + DB; a stale DB write after a successful provider
/// update compensates by restoring the former carrier email at the provider (best effort) and
/// returns the conflict explicitly.
/// </para>
/// <para>
/// No audit of any kind is implemented in P1-T05; results are typed so P1-T09 can instrument
/// later.
/// </para>
/// </remarks>
public sealed class UserAdministrationService : IUserAdministrationService
{
    private readonly IUserRepository _users;
    private readonly ITemplateRepository _templates;
    private readonly IUserIdentityProvisioner _provisioner;

    /// <summary>Creates the service over the persistence primitives and the privileged provider.</summary>
    public UserAdministrationService(
        IUserRepository users,
        ITemplateRepository templates,
        IUserIdentityProvisioner provisioner)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(templates);
        ArgumentNullException.ThrowIfNull(provisioner);
        _users = users;
        _templates = templates;
        _provisioner = provisioner;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserListItem>> ListAsync(CancellationToken cancellationToken)
    {
        var users = await _users.ListAsync(cancellationToken);
        var templates = await _templates.ListAsync(cancellationToken);
        var templateNames = templates.ToDictionary(
            template => template.TemplateId,
            template => template.Name);

        return users
            .Select(account => new UserListItem(
                account.AccountId,
                account.DisplayName,
                account.CompanyNumber,
                account.Email,
                account.RoleLabel,
                account.IsActive,
                account.TemplateId,
                account.TemplateId is { } id && templateNames.TryGetValue(id, out var name) ? name : null,
                account.Version))
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<UserFicha?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var account = await _users.GetByIdAsync(userId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        string? templateName = null;
        if (account.TemplateId is { } templateId)
        {
            var template = await _templates.GetByIdAsync(templateId, cancellationToken);
            templateName = template?.Name;
        }

        return new UserFicha(
            account.AccountId,
            account.DisplayName,
            account.CompanyNumber,
            account.Email,
            account.RoleLabel,
            account.IsActive,
            account.TemplateId,
            templateName,
            account.Version);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TemplateOption>> ListTemplateOptionsAsync(CancellationToken cancellationToken)
    {
        var templates = await _templates.ListAsync(cancellationToken);
        return templates
            .Select(template => new TemplateOption(template.TemplateId, template.Name))
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<UserAdministrationResult> CreateAsync(
        UserAdministrationCommands.CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = UserAdministrationValidator.Validate(command);
        if (validationErrors.Count > 0)
        {
            return new UserAdministrationResult.ValidationFailed(validationErrors);
        }

        // Template existence is checked before any provider side effect.
        if (command.TemplateId is { } templateId)
        {
            var template = await _templates.GetByIdAsync(templateId, cancellationToken);
            if (template is null)
            {
                return new UserAdministrationResult.ValidationFailed(
                    ["The selected Template no longer exists; reload the form."]);
            }
        }

        var companyNumber = NormalizeCompanyNumber(command.CompanyNumber);
        var email = NormalizeEmail(command.Email);
        var name = Trimmed(command.Name);
        var role = Trimmed(command.Role);

        // Deterministic app-side pre-checks (no provider call yet).
        var existingByCompany = await _users.GetByCompanyNumberAsync(companyNumber, cancellationToken);
        if (existingByCompany is not null)
        {
            // Correction D: an existing company number is NOT automatically a replay — it must
            // provably equal the same completed logical create result.
            return existingByCompany is var row && SameLogicalCreateResult(row, companyNumber, email, name, role, command)
                ? new UserAdministrationResult.IdempotentReplay(row)
                : new UserAdministrationResult.Conflict(
                    UserConflictReason.DuplicateCompanyNumber,
                    "The company number is already in use by another USER. An existing row only counts as " +
                    "the same completed create when every stable fact matches the retried command.");
        }

        var existingByEmail = await _users.GetByEmailAsync(email, cancellationToken);
        if (existingByEmail is not null)
        {
            return new UserAdministrationResult.Conflict(
                UserConflictReason.EmailInUse,
                "The carrier email is already in use by another USER account.");
        }

        // Provider-side email lookup: proves whether an identity pre-exists before we invite,
        // and lets a provably orphaned provisioning be adopted back instead of re-inviting.
        ProviderUser? existingProviderUser;
        try
        {
            existingProviderUser = await _provisioner.FindUserByEmailAsync(email, cancellationToken);
        }
        catch (ProviderUserOperationException exception)
        {
            return MapProviderOperationFailure(exception);
        }

        if (existingProviderUser is not null)
        {
            var referenced = await _users.GetByAuthIdentityAsync(existingProviderUser.Subject, cancellationToken);
            if (referenced is not null)
            {
                // The provider identity for this carrier email is already mapped to an
                // application USER (possibly a concurrent winner): never adopt, never invite.
                return new UserAdministrationResult.Conflict(
                    UserConflictReason.EmailInUse,
                    "The carrier email already maps to another USER account; reload the list and retry.");
            }

            return await AdoptOrphanAsync(
                existingProviderUser.Subject,
                companyNumber,
                email,
                name,
                role,
                command,
                cancellationToken);
        }

        // The carrier is provably free: invite. The identity we receive is provably created by
        // this operation (pre-check showed no pre-existing identity for the email), which is
        // the precondition for the accepted compensation rule.
        ProviderUserInvited invited;
        try
        {
            invited = await _provisioner.InviteUserAsync(email, command.InviteRedirectUrl, cancellationToken);
        }
        catch (ProviderUserOperationException exception)
        {
            // 422 on invite: a confirmed identity appeared for the carrier between the lookup
            // and the invite. The lookup/retry path (orphan analysis) decides what happened;
            // here the create surfaces the conflict without adopting blindly.
            return exception.Failure switch
            {
                ProviderUserOperationFailure.EmailAlreadyInUse => new UserAdministrationResult.Conflict(
                    UserConflictReason.EmailInUse,
                    "A confirmed provider identity already exists for the carrier email; reload and retry."),
                _ => MapProviderOperationFailure(exception),
            };
        }

        var account = new UserAccount(
            Guid.NewGuid(),
            companyNumber,
            name,
            email,
            role,
            command.Active,
            command.TemplateId,
            Version: 1);

        try
        {
            await _users.CreatedAsync(account, invited.Subject, cancellationToken);
        }
        catch (UserPersistenceException exception) when (
            exception.Reason is UserPersistenceFailureReason.DuplicateCompanyNumber
                or UserPersistenceFailureReason.DuplicateProviderSubject
                or UserPersistenceFailureReason.InvalidTemplateReference)
        {
            // Accepted compensation: delete the provider identity ONLY when it is provably
            // owned by this operation — that is, when no application row now references it
            // (a concurrent winner's row must never be broken). If cleanup fails, the failure
            // is explicit and create is never reported as success.
            var adoptedElsewhere = await _users.GetByAuthIdentityAsync(invited.Subject, cancellationToken);
            if (adoptedElsewhere is null)
            {
                try
                {
                    await _provisioner.DeleteUserAsync(invited.Subject, cancellationToken);
                }
                catch (ProviderUserOperationException)
                {
                    // The cleanup failed: the failure is explicit (ProviderFailed) and the
                    // create is never reported as success; the retry path uses the provider
                    // lookup instead of blind re-invites.
                    return new UserAdministrationResult.ProviderFailed(ProviderFailureKind.Error);
                }
            }

            return MapCreatePersistenceFailure(exception);
        }

        return new UserAdministrationResult.Created(account);
    }

    /// <inheritdoc />
    public async Task<UserAdministrationResult> ResendInviteAsync(
        UserAdministrationCommands.ResendInviteCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = UserAdministrationValidator.Validate(command);
        if (validationErrors.Count > 0)
        {
            return new UserAdministrationResult.ValidationFailed(validationErrors);
        }

        var loaded = await _users.GetByIdAsync(command.UserId, cancellationToken);
        if (loaded is null)
        {
            return new UserAdministrationResult.NotFound(command.UserId);
        }

        if (loaded.Version != command.ExpectedVersion)
        {
            return StaleConflict(command.UserId, command.ExpectedVersion, loaded.Version);
        }

        var subject = await _users.GetAuthIdentityIdAsync(command.UserId, cancellationToken);
        if (string.IsNullOrWhiteSpace(subject))
        {
            // No provider linkage: the setup invitation cannot be re-sent.
            return new UserAdministrationResult.NotFound(command.UserId);
        }

        try
        {
            // The provider re-sends the invitation while the identity is unconfirmed, and
            // rejects (422) once it is confirmed. Resend is distinct from password reset.
            await _provisioner.InviteUserAsync(loaded.Email, redirectUrl: null, cancellationToken);
        }
        catch (ProviderUserOperationException exception) when (
            exception.Failure is ProviderUserOperationFailure.EmailAlreadyInUse
                or ProviderUserOperationFailure.AlreadyConfirmed)
        {
            return new UserAdministrationResult.ValidationFailed(
                ["The USER has already confirmed the setup; use the password reset action instead."]);
        }
        catch (ProviderUserOperationException exception)
        {
            return MapProviderOperationFailure(exception);
        }

        return new UserAdministrationResult.Success(loaded);
    }

    /// <inheritdoc />
    public async Task<UserAdministrationResult> UpdateAsync(
        UserAdministrationCommands.UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = UserAdministrationValidator.Validate(command);
        if (validationErrors.Count > 0)
        {
            return new UserAdministrationResult.ValidationFailed(validationErrors);
        }

        var loaded = await _users.GetByIdAsync(command.UserId, cancellationToken);
        if (loaded is null)
        {
            return new UserAdministrationResult.NotFound(command.UserId);
        }

        if (loaded.Version != command.ExpectedVersion)
        {
            return StaleConflict(command.UserId, command.ExpectedVersion, loaded.Version);
        }

        var companyNumber = NormalizeCompanyNumber(command.CompanyNumber);
        var email = NormalizeEmail(command.Email);
        var name = Trimmed(command.Name);
        var role = Trimmed(command.Role);

        if (!string.Equals(companyNumber, loaded.CompanyNumber, StringComparison.Ordinal))
        {
            var duplicate = await _users.GetByCompanyNumberAsync(companyNumber, cancellationToken);
            if (duplicate is not null && duplicate.AccountId != command.UserId)
            {
                return new UserAdministrationResult.Conflict(
                    UserConflictReason.DuplicateCompanyNumber,
                    "The company number is already in use by another USER.");
            }
        }

        if (!string.Equals(email, loaded.Email, StringComparison.Ordinal))
        {
            var duplicate = await _users.GetByEmailAsync(email, cancellationToken);
            if (duplicate is not null && duplicate.AccountId != command.UserId)
            {
                return new UserAdministrationResult.Conflict(
                    UserConflictReason.EmailInUse,
                    "The carrier email is already in use by another USER account.");
            }
        }

        var emailChanged = !string.Equals(email, loaded.Email, StringComparison.Ordinal);

        // Provider first for an email (carrier) change; any provider failure aborts with the
        // DB untouched so provider and DB can never drift by this path.
        if (emailChanged)
        {
            var subject = await _users.GetAuthIdentityIdAsync(command.UserId, cancellationToken);
            if (string.IsNullOrWhiteSpace(subject))
            {
                // No provider linkage: the carrier email cannot be changed.
                return new UserAdministrationResult.NotFound(command.UserId);
            }

            try
            {
                await _provisioner.UpdateUserEmailAsync(subject, email, cancellationToken);
            }
            catch (ProviderUserOperationException exception)
            {
                return MapProviderOperationFailure(exception);
            }
        }

        var updated = loaded with
        {
            CompanyNumber = companyNumber,
            DisplayName = name,
            Email = email,
            RoleLabel = role,
        };

        try
        {
            await _users.UpdatedAsync(updated, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            if (emailChanged)
            {
                // Compensation: the provider carrier moved but the DB row was concurrently
                // changed — restore the former carrier email at the provider (best effort).
                var subject = await _users.GetAuthIdentityIdAsync(command.UserId, cancellationToken);
                if (!string.IsNullOrWhiteSpace(subject))
                {
                    try
                    {
                        await _provisioner.UpdateUserEmailAsync(subject, loaded.Email, cancellationToken);
                    }
                    catch (ProviderUserOperationException)
                    {
                        // Compensation restore failed: the provider carrier may be ahead of
                        // the DB row (known drift, recoverable by retrying the edit). The
                        // conflict is explicit either way.
                    }
                }
            }

            return new UserAdministrationResult.Conflict(
                UserConflictReason.StaleVersion,
                "The USER was modified after the form was loaded; reload the ficha and retry.");
        }
        catch (InvalidOperationException)
        {
            // The row disappeared between load and save (concurrent delete).
            return new UserAdministrationResult.NotFound(command.UserId);
        }
        catch (UserPersistenceException exception) when (
            exception.Reason == UserPersistenceFailureReason.InvalidTemplateReference)
        {
            // The USER's Template disappeared between load and save (concurrent Template action).
            return new UserAdministrationResult.ValidationFailed(
                ["The USER's Template no longer exists; reload the ficha and retry."]);
        }

        return new UserAdministrationResult.Success(updated with { Version = updated.Version + 1 });
    }

    /// <inheritdoc />
    public async Task<UserAdministrationResult> SetActiveAsync(
        UserAdministrationCommands.SetUserActiveCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = UserAdministrationValidator.Validate(command);
        if (validationErrors.Count > 0)
        {
            return new UserAdministrationResult.ValidationFailed(validationErrors);
        }

        var loaded = await _users.GetByIdAsync(command.UserId, cancellationToken);
        if (loaded is null)
        {
            return new UserAdministrationResult.NotFound(command.UserId);
        }

        if (loaded.Version != command.ExpectedVersion)
        {
            return StaleConflict(command.UserId, command.ExpectedVersion, loaded.Version);
        }

        try
        {
            await _users.SetActiveAsync(command.UserId, command.Active, command.ExpectedVersion, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return StaleConflict(command.UserId, command.ExpectedVersion, loaded.Version);
        }
        catch (InvalidOperationException)
        {
            return new UserAdministrationResult.NotFound(command.UserId);
        }

        return new UserAdministrationResult.Success(
            loaded with { IsActive = command.Active, Version = loaded.Version + 1 });
    }

    /// <inheritdoc />
    public async Task<UserAdministrationResult> SetTemplateAsync(
        UserAdministrationCommands.SetUserTemplateCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = UserAdministrationValidator.Validate(command);
        if (validationErrors.Count > 0)
        {
            return new UserAdministrationResult.ValidationFailed(validationErrors);
        }

        var loaded = await _users.GetByIdAsync(command.UserId, cancellationToken);
        if (loaded is null)
        {
            return new UserAdministrationResult.NotFound(command.UserId);
        }

        if (loaded.Version != command.ExpectedVersion)
        {
            return StaleConflict(command.UserId, command.ExpectedVersion, loaded.Version);
        }

        if (command.TemplateId is { } templateId)
        {
            var template = await _templates.GetByIdAsync(templateId, cancellationToken);
            if (template is null)
            {
                return new UserAdministrationResult.ValidationFailed(
                    ["The selected Template no longer exists; reload the form."]);
            }
        }

        try
        {
            await _users.SetTemplateAsync(command.UserId, command.TemplateId, command.ExpectedVersion, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return StaleConflict(command.UserId, command.ExpectedVersion, loaded.Version);
        }
        catch (InvalidOperationException)
        {
            return new UserAdministrationResult.NotFound(command.UserId);
        }
        catch (UserPersistenceException exception) when (
            exception.Reason == UserPersistenceFailureReason.InvalidTemplateReference)
        {
            return new UserAdministrationResult.ValidationFailed(
                ["The selected Template no longer exists; reload the form."]);
        }

        return new UserAdministrationResult.Success(
            loaded with { TemplateId = command.TemplateId, Version = loaded.Version + 1 });
    }

    /// <inheritdoc />
    public async Task<UserAdministrationResult> DeleteAsync(
        UserAdministrationCommands.DeleteUserCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = UserAdministrationValidator.Validate(command);
        if (validationErrors.Count > 0)
        {
            return new UserAdministrationResult.ValidationFailed(validationErrors);
        }

        // 1. Load: missing -> NotFound.
        var loaded = await _users.GetByIdAsync(command.UserId, cancellationToken);
        if (loaded is null)
        {
            return new UserAdministrationResult.NotFound(command.UserId);
        }

        // 2. NON-DESTRUCTIVE version pre-check: mismatch -> Conflict, no provider call, no DB
        // delete. (The old destructive DeleteAsync-as-pre-check is never used here.)
        if (loaded.Version != command.ExpectedVersion)
        {
            return StaleConflict(command.UserId, command.ExpectedVersion, loaded.Version);
        }

        var subject = await _users.GetAuthIdentityIdAsync(command.UserId, cancellationToken);

        // 3. Provider identity delete first: success or already-absent (404) continues; any
        // other provider failure aborts with the DB untouched.
        if (!string.IsNullOrWhiteSpace(subject))
        {
            try
            {
                await _provisioner.DeleteUserAsync(subject, cancellationToken);
            }
            catch (ProviderUserOperationException exception)
            {
                return MapProviderOperationFailure(exception);
            }
        }

        // 4. Final version-checked row delete.
        try
        {
            await _users.DeleteAsync(command.UserId, command.ExpectedVersion, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // Accepted recoverable partial state (delete race): the provider identity is
            // already absent, the row SURVIVES with its provider linkage (retry anchor), and
            // this is never reported as success — retry with the current version completes it.
            return new UserAdministrationResult.Conflict(
                UserConflictReason.StaleVersion,
                "The USER was modified during deletion; the provider identity was already removed and the " +
                "USER row was kept. Reload and retry the delete to finish it.");
        }
        catch (InvalidOperationException)
        {
            // The row does not exist anymore (concurrent delete finished it); the provider
            // identity delete was already performed by this operation.
            return new UserAdministrationResult.Success(loaded);
        }

        return new UserAdministrationResult.Success(loaded);
    }

    /// <inheritdoc />
    public async Task<UserAdministrationResult> InitiatePasswordResetAsync(
        UserAdministrationCommands.RequestedPasswordResetCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = UserAdministrationValidator.Validate(command);
        if (validationErrors.Count > 0)
        {
            return new UserAdministrationResult.ValidationFailed(validationErrors);
        }

        var loaded = await _users.GetByIdAsync(command.UserId, cancellationToken);
        if (loaded is null)
        {
            return new UserAdministrationResult.NotFound(command.UserId);
        }

        if (loaded.Version != command.ExpectedVersion)
        {
            return StaleConflict(command.UserId, command.ExpectedVersion, loaded.Version);
        }

        // Fail closed (registered decision): reset initiation is only offered for active
        // accounts; an inactive USER is not eligible.
        if (!loaded.IsActive)
        {
            return new UserAdministrationResult.ValidationFailed(
                ["Password reset cannot be initiated for an inactive account."]);
        }

        try
        {
            await _provisioner.InitiatePasswordRecoveryAsync(loaded.Email, cancellationToken);
        }
        catch (ProviderUserOperationException exception)
        {
            return MapProviderOperationFailure(exception);
        }

        // The reset only initiates the provider flow; the USER chooses the new password
        // through the provider. No password/token is ever handled by this path.
        return new UserAdministrationResult.Success(loaded);
    }

    // -----------------------------------------------------------------------------------

    private async Task<UserAdministrationResult> AdoptOrphanAsync(
        string subject,
        string companyNumber,
        string email,
        string name,
        string role,
        UserAdministrationCommands.CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        // Re-verify the two racy facts before persisting: the subject must still be unmapped
        // and the company number must still be free.
        var referenced = await _users.GetByAuthIdentityAsync(subject, cancellationToken);
        if (referenced is not null)
        {
            return new UserAdministrationResult.Conflict(
                UserConflictReason.EmailInUse,
                "The provider identity for the carrier email is already mapped to another USER account.");
        }

        var companyRace = await _users.GetByCompanyNumberAsync(companyNumber, cancellationToken);
        if (companyRace is not null)
        {
            return SameLogicalCreateResult(companyRace, companyNumber, email, name, role, command)
                ? new UserAdministrationResult.IdempotentReplay(companyRace)
                : new UserAdministrationResult.Conflict(
                    UserConflictReason.DuplicateCompanyNumber,
                    "The company number is already in use by another USER.");
        }

        var account = new UserAccount(
            Guid.NewGuid(),
            companyNumber,
            name,
            email,
            role,
            command.Active,
            command.TemplateId,
            Version: 1);

        try
        {
            await _users.CreatedAsync(account, subject, cancellationToken);
        }
        catch (UserPersistenceException exception)
        {
            // No compensation here: the provider identity pre-existed this operation and is
            // never deleted. The typed failure maps honestly.
            return MapCreatePersistenceFailure(exception);
        }

        // The existing provider identity for this carrier email is adopted as this USER's
        // linkage (no fresh invite): the invitation/setup link already exists for it.
        return new UserAdministrationResult.RecoveredProvisioning(account);
    }

    /// <summary>
    /// Strict replay proof (Correction D): the existing row must equal every stable fact of the
    /// retried create; a material difference is a duplicate, never a replay.
    /// </summary>
    private static bool SameLogicalCreateResult(
        UserAccount existing,
        string companyNumber,
        string email,
        string name,
        string role,
        UserAdministrationCommands.CreateUserCommand command) =>
        string.Equals(NormalizeCompanyNumber(existing.CompanyNumber), companyNumber, StringComparison.Ordinal)
        && string.Equals(NormalizeEmail(existing.Email), email, StringComparison.Ordinal)
        && string.Equals(existing.DisplayName, name, StringComparison.Ordinal)
        && string.Equals(existing.RoleLabel, role, StringComparison.Ordinal)
        && existing.IsActive == command.Active
        && existing.TemplateId == command.TemplateId;

    private static string Trimmed(string value) => value.Trim();

    /// <summary>
    /// Carrier normalization rule: the persisted <c>users.email</c> is always the normalized
    /// form (trim + lowercase), mirroring the provider's internal email normalization, so the
    /// application carrier never drifts from the provider identity.
    /// </summary>
    internal static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>Company-number normalization: trim (no industrial format is invented).</summary>
    internal static string NormalizeCompanyNumber(string companyNumber) => companyNumber.Trim();

    private static UserAdministrationResult.Conflict StaleConflict(
        Guid userId,
        int expectedVersion,
        int currentVersion) =>
        new(
            UserConflictReason.StaleVersion,
            $"The USER was modified (expected version {expectedVersion}, current version {currentVersion}); " +
            "reload and retry. No change was applied.");

    private UserAdministrationResult MapProviderOperationFailure(ProviderUserOperationException exception)
    {
        // Observability of provider failures lives at the transport layer (the adapter logs
        // statuses without secrets); here the failure is mapped onto the closed result set.
        return new UserAdministrationResult.ProviderFailed(
            exception.Failure == ProviderUserOperationFailure.Unavailable
                ? ProviderFailureKind.Unavailable
                : ProviderFailureKind.Error);
    }

    private UserAdministrationResult MapCreatePersistenceFailure(UserPersistenceException exception)
    {
        return exception.Reason switch
        {
            UserPersistenceFailureReason.DuplicateCompanyNumber => new UserAdministrationResult.Conflict(
                UserConflictReason.DuplicateCompanyNumber,
                "The company number was already taken when the row was persisted; reload and retry."),
            UserPersistenceFailureReason.DuplicateProviderSubject => new UserAdministrationResult.Conflict(
                UserConflictReason.EmailInUse,
                "The carrier email was adopted by another operation while this create was in flight; " +
                "reload the list and retry."),
            _ => new UserAdministrationResult.ValidationFailed(
                ["The selected Template no longer exists; reload the form."]),
        };
    }
}