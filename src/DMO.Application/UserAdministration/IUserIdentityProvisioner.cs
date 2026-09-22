namespace DMO.Application.UserAdministration;

/// <summary>
/// Server-only privileged provider boundary for USER identity provisioning (P1-T05).
/// </summary>
/// <remarks>
/// <para>
/// This is the <b>only</b> surface that talks to the Supabase Auth Admin API. It is always
/// invoked server-side, carries no password in any shape (initial USER credentials are
/// established by the USER through the provider invitation/setup flow), and never sends the
/// company number to the provider — the provider identity is keyed by the carrier email.
/// </para>
/// <para>
/// The underlying adapter uses the service-role secret; implementers must never expose it to
/// the browser, Razor output, JS, HTTP responses or logs.
/// </para>
/// <para>
/// Failures are raised as <see cref="ProviderUserOperationException"/> with a typed
/// <see cref="ProviderUserOperationFailure"/>; callers map them onto the closed result set —
/// the Application layer never sees HTTP/JSON.
/// </para>
/// </remarks>
public interface IUserIdentityProvisioner
{
    /// <summary>
    /// Invites the given carrier email, returning the provider subject of the invited
    /// identity.
    /// </summary>
    /// <remarks>
    /// Creates the provider identity without a password and lets the provider send the
    /// invitation email; the USER completes setup (chooses their own password). For an
    /// <b>unconfirmed</b> existing identity the provider re-sends the invitation; for a
    /// <b>confirmed</b> identity the provider rejects and
    /// <see cref="ProviderUserOperationFailure.EmailAlreadyInUse"/> is raised.
    /// </remarks>
    /// <param name="carrierEmail">Normalized carrier email of the identity.</param>
    /// <param name="redirectUrl">Optional absolute http(s) URL for the invitation link.</param>
    Task<ProviderUserInvited> InviteUserAsync(
        string carrierEmail,
        string? redirectUrl,
        CancellationToken cancellationToken);

    /// <summary>Returns the provider identity for the subject, or <c>null</c> when absent.</summary>
    Task<ProviderUser?> GetUserAsync(string subject, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the provider identity whose carrier email matches exactly, or <c>null</c>.
    /// </summary>
    /// <remarks>
    /// Implementers page the <b>documented</b> provider list behavior
    /// (<c>GET /auth/v1/admin/users?page=&amp;per_page=</c>) and exact-match the normalized
    /// carrier email server-side. Undocumented filter behavior is never used. More than one
    /// exact match is a provider inconsistency and fails closed (raised as
    /// <see cref="ProviderUserOperationFailure.Error"/>).
    /// </remarks>
    Task<ProviderUser?> FindUserByEmailAsync(string carrierEmail, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the provider identity with the given subject.
    /// </summary>
    /// <remarks>An already-absent identity (provider 404) is treated as success (idempotent).</remarks>
    Task DeleteUserAsync(string subject, CancellationToken cancellationToken);

    /// <summary>Changes the carrier email of the provider identity (direct admin update).</summary>
    Task UpdateUserEmailAsync(string subject, string newCarrierEmail, CancellationToken cancellationToken);

    /// <summary>
    /// Initiates the provider password-recovery flow for the given carrier email.
    /// </summary>
    /// <remarks>
    /// Uses the provider's public recovery path (no service-role secret). The provider sends
    /// the recovery email; the USER chooses the new password through the provider flow. No
    /// reset token is persisted or logged by the application.
    /// </remarks>
    Task InitiatePasswordRecoveryAsync(string carrierEmail, CancellationToken cancellationToken);
}

/// <summary>Result of a successful provider invitation.</summary>
/// <param name="Subject">Provider subject (auth.users id) of the invited identity.</param>
public sealed record ProviderUserInvited(string Subject);

/// <summary>A provider identity observed through the privileged boundary.</summary>
/// <param name="Subject">Provider subject (auth.users id).</param>
/// <param name="Email">Provider-carried email (normalized).</param>
public sealed record ProviderUser(string Subject, string Email);