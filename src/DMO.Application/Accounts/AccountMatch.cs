using DMO.Application.Authentication;

namespace DMO.Application.Accounts;

/// <summary>
/// One application account associated with the authenticated identity.
/// </summary>
/// <remarks>
/// The lookup only supplies candidate matches. All mapping semantics live in
/// <see cref="AccountResolver"/>; a match never decides access by itself. Provider identity
/// is internal linkage only — a match must be an explicit, persisted application association
/// (P1-T03), never a provider role/claim.
/// </remarks>
public abstract record AccountMatch
{
    private AccountMatch()
    {
    }

    /// <summary>A match on the single ADMIN account.</summary>
    /// <param name="Account">The ADMIN account record.</param>
    public sealed record Admin(AdminAccount Account) : AccountMatch;

    /// <summary>A match on a USER account.</summary>
    /// <param name="Account">The USER account record.</param>
    public sealed record User(UserAccount Account) : AccountMatch;
}