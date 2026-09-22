using DMO.Application.Accounts;

namespace DMO.Application.Repositories;

/// <summary>
/// Persistence primitives for application USER accounts.
/// </summary>
/// <remarks>
/// <para>
/// P1-T03 provides these primitives only; no service/UI workflow is implemented. P1-T05
/// builds the USER administration workflows on top. <c>GetByCompanyNumberAsync</c> is the
/// canonical management-path lookup; the USER authentication flow consumes the narrower
/// <see cref="DMO.Application.Authentication.IUserAuthenticationLookup"/> contract instead.
/// </para>
/// <para>
/// Writes are optimistic: the row <c>version</c> is compared and incremented, and a stale
/// write surfaces as <see cref="DMO.Application.Persistence.ConcurrencyConflictException"/>
/// — never a silent overwrite.
/// </para>
/// </remarks>
public interface IUserRepository
{
    /// <summary>Returns the USER account with the given application id, or <c>null</c>.</summary>
    Task<UserAccount?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Returns the USER account with the given company number (exact match), or <c>null</c>.</summary>
    Task<UserAccount?> GetByCompanyNumberAsync(string companyNumber, CancellationToken cancellationToken);

    /// <summary>Returns the USER account mapped to the given provider subject, or <c>null</c>.</summary>
    Task<UserAccount?> GetByAuthIdentityAsync(string providerSubject, CancellationToken cancellationToken);

    /// <summary>Returns the active USER accounts.</summary>
    Task<IReadOnlyList<UserAccount>> ListActiveAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Inserts the USER account (initial version 1) with the given provider subject.
    /// </summary>
    /// <remarks>
    /// <paramref name="authIdentityId"/> is the provider subject mapped to the account; it is
    /// a mandatory column (<c>users.auth_identity_id</c> NOT NULL, UNIQUE) so it is a
    /// required parameter of the create primitive. The domain <see cref="UserAccount"/>
    /// record carries no provider linkage.
    /// </remarks>
    Task CreatedAsync(UserAccount account, string authIdentityId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the USER account, comparing the row version observed at read time.
    /// </summary>
    /// <remarks>
    /// The provider mapping (<c>auth_identity_id</c>) is durable linkage and is not changed
    /// by this primitive.
    /// </remarks>
    Task UpdatedAsync(UserAccount account, CancellationToken cancellationToken);

    /// <summary>Sets the USER active flag against an expected version (stale → conflict).</summary>
    Task SetActiveAsync(Guid userId, bool active, int expectedVersion, CancellationToken cancellationToken);

    /// <summary>Sets the USER Template association against an expected version (stale → conflict).</summary>
    Task SetTemplateAsync(Guid userId, Guid? templateId, int expectedVersion, CancellationToken cancellationToken);

    /// <summary>Deletes the USER account.</summary>
    Task DeleteAsync(Guid userId, CancellationToken cancellationToken);

    // ---- P1-T05 additive reads/writes (no schema impact) -------------------------------

    /// <summary>Returns every USER account (active and inactive), ordered.</summary>
    Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Returns the USER account with the given carrier email (exact match), or <c>null</c>.</summary>
    Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the internal provider linkage (auth_identity_id) of the USER, or <c>null</c>
    /// when the USER does not exist.
    /// </summary>
    /// <remarks>
    /// P1-T05 addition required by the accepted delete/email/orphan flows: <c>UserAccount</c>
    /// deliberately carries no provider linkage (it is internal, never presented), so the
    /// administration service reads the linkage through this narrow primitive only when a
    /// privileged provider operation needs it.
    /// </remarks>
    Task<string?> GetAuthIdentityIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the USER account against an expected version (stale → conflict, no overwrite).
    /// </summary>
    /// <remarks>
    /// P1-T05 addition used as the final step of the accepted delete sequence, after the
    /// non-destructive version pre-check and the provider identity delete. A missing row
    /// (already deleted concurrently) completes as a no-op — the caller decides how to
    /// interpret that state.
    /// </remarks>
    Task DeleteAsync(Guid userId, int expectedVersion, CancellationToken cancellationToken);
}