using DMO.Application.Authentication;

namespace DMO.Application.Accounts;

/// <summary>
/// Provider-neutral data source of application accounts associated with an authenticated
/// identity.
/// </summary>
/// <remarks>
/// <para>
/// The lookup is the data boundary only. It never classifies and never decides access:
/// <see cref="AccountResolver"/> owns the real mapping semantics.
/// </para>
/// <para>
/// In P1-T02 production the lookup is <c>UnavailableAccountLookup</c> (no persisted mapping
/// until P1-T03), so the real resolver always fails closed. P1-T03 replaces the lookup
/// adapter without changing resolver semantics.
/// </para>
/// </remarks>
public interface IAccountLookup
{
    /// <summary>
    /// Returns the application accounts associated with the authenticated identity.
    /// </summary>
    /// <param name="identity">The authenticated identity (internal linkage only).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A possibly empty list of candidate matches.</returns>
    Task<IReadOnlyList<AccountMatch>> LookupAsync(
        AuthenticatedIdentity identity,
        CancellationToken cancellationToken);
}