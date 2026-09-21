namespace DMO.Application.Accounts;

/// <summary>
/// The application's account types. Exactly two.
/// </summary>
/// <remarks>
/// <c>NoAccess</c> and <c>None</c> are result states, never account types.
/// </remarks>
public enum AccountType
{
    /// <summary>Dedicated Administration account. Exactly one exists.</summary>
    Admin,

    /// <summary>Operational application user with exactly one effective Template later (P1-T03+).</summary>
    User,
}