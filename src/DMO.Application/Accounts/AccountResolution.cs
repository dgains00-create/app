namespace DMO.Application.Accounts;

/// <summary>
/// Result of mapping an authenticated identity onto an application account.
/// </summary>
/// <remarks>
/// <c>NoAccess</c> is a resolution <b>state</b>, not a third account type. The account types
/// are exactly <see cref="AccountType.Admin"/> and <see cref="AccountType.User"/>.
/// </remarks>
public abstract record AccountResolution
{
    private AccountResolution()
    {
    }

    /// <summary>The authenticated identity maps to the active ADMIN account.</summary>
    /// <param name="Account">The ADMIN account record.</param>
    public sealed record Admin(AdminAccount Account) : AccountResolution;

    /// <summary>The authenticated identity maps to an active USER account.</summary>
    /// <param name="Account">The USER account record.</param>
    public sealed record User(UserAccount Account) : AccountResolution;

    /// <summary>The identity did not resolve to an active account; the caller fails closed.</summary>
    /// <param name="Reason">The resolution-level reason.</param>
    public sealed record NoAccess(NoAccessReason Reason) : AccountResolution;
}