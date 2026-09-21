using DMO.Application.Authentication;

namespace DMO.Application.Accounts;

/// <summary>
/// Real, provider-neutral application mapping semantics. This is the subject under test for
/// active/inactive/unknown/ambiguous resolution behaviour.
/// </summary>
/// <remarks>
/// <para>
/// Resolution is pure logic over the <see cref="IAccountLookup"/> data source:
/// </para>
/// <code>
/// zero matches         -> NoAccess(UnknownAccount)
/// more than one match  -> NoAccess(AmbiguousMapping)
/// exactly one match:
///   Admin match, active   -> Admin(AdminAccount)
///   Admin match, inactive -> NoAccess(InactiveAdmin)
///   User match,  active   -> User(UserAccount)
///   User match,  inactive -> NoAccess(InactiveUser)
/// </code>
/// <para>
/// Provider identity is internal linkage only. No role string, Template name, email pattern
/// or provider claim ever produces a classification.
/// </para>
/// </remarks>
public sealed class AccountResolver : IAccountResolver
{
    private readonly IAccountLookup _lookup;

    /// <summary>Creates a resolver over the supplied lookup.</summary>
    /// <param name="lookup">The application-account data source.</param>
    public AccountResolver(IAccountLookup lookup)
    {
        ArgumentNullException.ThrowIfNull(lookup);
        _lookup = lookup;
    }

    /// <inheritdoc />
    public async Task<AccountResolution> ResolveAsync(
        AuthenticatedIdentity identity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(identity);

        var matches = await _lookup.LookupAsync(identity, cancellationToken);

        if (matches.Count == 0)
        {
            return new AccountResolution.NoAccess(NoAccessReason.UnknownAccount);
        }

        if (matches.Count > 1)
        {
            return new AccountResolution.NoAccess(NoAccessReason.AmbiguousMapping);
        }

        return matches[0] switch
        {
            AccountMatch.Admin(var admin) when admin.IsActive => new AccountResolution.Admin(admin),
            AccountMatch.Admin(_) => new AccountResolution.NoAccess(NoAccessReason.InactiveAdmin),
            AccountMatch.User(var user) when user.IsActive => new AccountResolution.User(user),
            AccountMatch.User(_) => new AccountResolution.NoAccess(NoAccessReason.InactiveUser),
            _ => new AccountResolution.NoAccess(NoAccessReason.UnknownAccount),
        };
    }
}