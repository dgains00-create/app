using DMO.Application.Accounts;

namespace DMO.Application.Session;

/// <summary>
/// Read-only result of the current-account lookup.
/// </summary>
/// <remarks>
/// <c>None</c> is a <b>state</b> (no session, or resolution failed closed), not an account
/// type. The account types are exactly <see cref="Accounts.AccountType.Admin"/> and
/// <see cref="Accounts.AccountType.User"/>.
/// </remarks>
public abstract record CurrentAccount
{
    private CurrentAccount()
    {
    }

    /// <summary>The current resolved ADMIN account.</summary>
    /// <param name="Account">The ADMIN account record.</param>
    public sealed record Admin(AdminAccount Account) : CurrentAccount;

    /// <summary>The current resolved USER account.</summary>
    /// <param name="Account">The USER account record.</param>
    public sealed record User(UserAccount Account) : CurrentAccount;

    /// <summary>No session, or the session identity did not resolve to an active account.</summary>
    public sealed record None() : CurrentAccount;
}