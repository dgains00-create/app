using DMO.Application.Accounts;

namespace DMO.Application.Repositories;

/// <summary>
/// Persistence support for the single ADMIN account and its bootstrap.
/// </summary>
/// <remarks>
/// <para>
/// There is deliberately <b>no</b> general ADMIN CRUD: no update/delete/list beyond the
/// single-account read paths. The bootstrap creates the row; ordinary operations can only
/// read <c>admin_accounts</c>.
/// </para>
/// <para>
/// The single-ADMIN invariant is enforced by the database (CHECK + PK on the fixed
/// <see cref="AdminAccountId"/>); these repository methods exist for the bootstrap flow and
/// its diagnostics.
/// </para>
/// </remarks>
public interface IAdminAccountRepository
{
    /// <summary>Returns the ADMIN account mapped to the given provider subject, or <c>null</c>.</summary>
    Task<AdminAccount?> GetByAuthIdentityAsync(string providerSubject, CancellationToken cancellationToken);

    /// <summary>Returns the ADMIN account with the given email, or <c>null</c>.</summary>
    Task<AdminAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>Returns the single ADMIN account, or <c>null</c> when none exists.</summary>
    Task<AdminAccount?> GetSingleAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Inserts the ADMIN row with the fixed <see cref="AdminAccountId"/> and the given
    /// provider subject.
    /// </summary>
    /// <remarks>
    /// Insert only (not ordinary CRUD). <paramref name="authIdentityId"/> is the provider
    /// subject of the existing Supabase Auth ADMIN identity, supplied by the bootstrap; it
    /// is a mandatory column (<c>auth_identity_id</c> NOT NULL) so it is a required
    /// parameter of the create primitive.
    /// </remarks>
    Task CreatedAsync(AdminAccount account, string authIdentityId, CancellationToken cancellationToken);
}