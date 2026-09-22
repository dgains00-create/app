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
}