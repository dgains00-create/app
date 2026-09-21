using DMO.Application.Accounts;
using DMO.Application.Authentication;

namespace DMO.Web.Resolution;

/// <summary>
/// Production <see cref="IAccountLookup"/> for P1-T02.
/// </summary>
/// <remarks>
/// <para>
/// No persisted account mapping exists until P1-T03, so this lookup always returns an empty
/// match list and the real <see cref="AccountResolver"/> therefore always produces
/// <c>NoAccess(UnknownAccount)</c> in P1-T02 production.
/// </para>
/// <para>
/// This is the honest fail-closed data source — not a fake provider, not a temporary
/// mapping, not an in-memory account store, and not a hard-coded ADMIN/USER correspondence.
/// P1-T03 replaces this lookup with the persistence-backed lookup without changing resolver
/// semantics.
/// </para>
/// </remarks>
public sealed class UnavailableAccountLookup : IAccountLookup
{
    /// <inheritdoc />
    public Task<IReadOnlyList<AccountMatch>> LookupAsync(
        AuthenticatedIdentity identity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identity);
        return Task.FromResult<IReadOnlyList<AccountMatch>>([]);
    }
}