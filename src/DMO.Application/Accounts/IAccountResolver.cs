using DMO.Application.Authentication;

namespace DMO.Application.Accounts;

/// <summary>
/// Maps an authenticated identity onto the application's active ADMIN or USER account.
/// </summary>
/// <remarks>
/// Account resolution answers only: <i>does this authenticated identity map to the
/// application's active ADMIN or USER account?</i> It never decides Template/Module access.
/// </remarks>
public interface IAccountResolver
{
    /// <summary>
    /// Resolves the authenticated identity to exactly one application account, or fails closed.
    /// </summary>
    /// <param name="identity">The authenticated identity (internal linkage only).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see cref="AccountResolution.Admin"/>, <see cref="AccountResolution.User"/>, or
    /// <see cref="AccountResolution.NoAccess"/>.
    /// </returns>
    Task<AccountResolution> ResolveAsync(
        AuthenticatedIdentity identity,
        CancellationToken cancellationToken);
}