using DMO.Application.Accounts;
using DMO.Application.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DMO.Web.Startup;

/// <summary>
/// Deployment-only, idempotent single-ADMIN bootstrap.
/// </summary>
/// <remarks>
/// <para>
/// Executed only through the <c>bootstrap-admin</c> startup command in DEV/TEST by an
/// operator; it is not part of the runtime login path and holds no service credentials. It
/// performs <b>no provider call at all</b>: the identity is asserted by the operator-supplied
/// subject (<c>AdminBootstrap__AuthIdentityId</c>), no Admin API, no <c>service_role</c>, no
/// management token and no ADMIN password is used.
/// </para>
/// <para>
/// Procedure: validate nonblank configuration → read the existing singleton ADMIN row →
/// absent: insert with the fixed <see cref="AdminAccountId"/>; same email AND same subject:
/// no-op success; conflicting email/subject: explicit failure (never overwrite, never
/// replace). The database is the structural barrier (the <c>admin_accounts_singleton_id_check</c>
/// CHECK plus the <c>admin_id</c> PK); the application checks exist only for clear
/// diagnostics. Replay after a crash-after-commit is idempotent.
/// </para>
/// <para>
/// <c>admin_id</c> is never an input and never derived from the email: it is the fixed
/// code-owned application constant, identical on every run and in every environment.
/// </para>
/// </remarks>
public sealed class AdminBootstrapCommand
{
    private readonly IAdminAccountRepository _repository;
    private readonly AdminBootstrapOptions _options;
    private readonly ILogger<AdminBootstrapCommand> _logger;

    /// <summary>Creates the command over the ADMIN repository, options and logger.</summary>
    public AdminBootstrapCommand(
        IAdminAccountRepository repository,
        IOptions<AdminBootstrapOptions> options,
        ILogger<AdminBootstrapCommand> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Executes the bootstrap and returns a process exit code.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="StartupCommands.SuccessExitCode"/> on success, <see cref="StartupCommands.FailureExitCode"/> otherwise.</returns>
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var email = _options.Email;
        var displayName = _options.DisplayName;
        var authIdentityId = _options.AuthIdentityId;

        // Validate nonblank configuration first: an empty or invalid run is an error, not a
        // default and not an anonymous ADMIN.
        if (string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(displayName)
            || string.IsNullOrWhiteSpace(authIdentityId))
        {
            _logger.LogError(
                "ADMIN bootstrap configuration is incomplete. Set {EmailKey}, {DisplayNameKey} and " +
                "{AuthIdentityIdKey} (environment or user-secrets). No default is assumed.",
                AdminBootstrapOptions.EmailKey,
                AdminBootstrapOptions.DisplayNameKey,
                AdminBootstrapOptions.AuthIdentityIdKey);
            return StartupCommands.FailureExitCode;
        }

        // Read the existing singleton ADMIN row (at most one by the DB invariant).
        var existing = await _repository.GetSingleAsync(cancellationToken);

        if (existing is null)
        {
            // Absent -> insert using the fixed AdminAccountId constant.
            try
            {
                await _repository.CreatedAsync(
                    new AdminAccount(AdminAccountId.Value, displayName, email, IsActive: true),
                    authIdentityId,
                    cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                // A racing concurrent bootstrap insert fails at the database layer (PK /
                // CHECK / unique constraints) and converges to a single ADMIN row.
                _logger.LogError(
                    exception,
                    "ADMIN bootstrap insert failed (a concurrent bootstrap or an existing mapping " +
                    "conflicts at the database layer). The singleton invariant was not violated.");
                return StartupCommands.FailureExitCode;
            }

            _logger.LogInformation(
                "ADMIN bootstrap created the single ADMIN row (admin_id {AdminId}, email {Email}).",
                AdminAccountId.Value,
                email);
            return StartupCommands.SuccessExitCode;
        }

        // Row present: no-op only when the same email AND the same provider subject are
        // supplied; anything else is an explicit conflict — never overwrite, never replace.
        var sameEmail = string.Equals(existing.Email, email, StringComparison.Ordinal);
        var sameSubject = await _repository.GetByAuthIdentityAsync(authIdentityId, cancellationToken) is not null;

        if (sameEmail && sameSubject)
        {
            _logger.LogInformation(
                "ADMIN bootstrap no-op: the single ADMIN row already matches the supplied email and " +
                "provider subject (email {Email}).",
                email);
            return StartupCommands.SuccessExitCode;
        }

        _logger.LogError(
            "ADMIN bootstrap conflict: an ADMIN row already exists (admin_id {AdminId}, email {ExistingEmail}) " +
            "whose email and/or provider subject differ from the supplied values. Refusing to overwrite " +
            "or replace the existing mapping.",
            existing.AccountId,
            existing.Email);
        return StartupCommands.FailureExitCode;
    }
}