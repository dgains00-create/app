namespace DMO.Application.Session;

/// <summary>
/// Read-only current-account lookup.
/// </summary>
/// <remarks>
/// <para>
/// This abstraction reads the current authenticated session identity (if any) and produces
/// the current application account through the same account-resolution logic. It never
/// mutates the session and never grants access itself.
/// </para>
/// <para>
/// Session mutation (establish/clear/sign-out) is a runtime concern in <c>DMO.Web</c> and is
/// deliberately not exposed here.
/// </para>
/// </remarks>
public interface ICurrentAccountContext
{
    /// <summary>
    /// Returns the current account: <see cref="CurrentAccount.Admin"/>,
    /// <see cref="CurrentAccount.User"/>, or <see cref="CurrentAccount.None"/> when there is
    /// no session or resolution fails closed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<CurrentAccount> GetCurrentAsync(CancellationToken cancellationToken);
}