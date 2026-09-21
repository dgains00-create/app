using DMO.Application.Accounts;
using DMO.Application.Session;

namespace DMO.Web.Auth;

/// <summary>
/// Real runtime implementation of <see cref="ICurrentAccountContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// Narrow responsibility: read the current authenticated session identity if one exists (via
/// the runtime session element), call the real <see cref="IAccountResolver"/> with that
/// identity, and return <see cref="CurrentAccount.Admin"/>,
/// <see cref="CurrentAccount.User"/> or <see cref="CurrentAccount.None"/>.
/// </para>
/// <para>
/// It never grants access itself and adds no Template/Module logic. It returns
/// <c>None</c> when there is no session or resolution fails closed.
/// </para>
/// </remarks>
public sealed class CurrentAccountContext : ICurrentAccountContext
{
    private readonly ISessionAuthentication _session;
    private readonly IAccountResolver _resolver;

    /// <summary>Creates the context over the runtime session element and the resolver.</summary>
    public CurrentAccountContext(ISessionAuthentication session, IAccountResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(resolver);
        _session = session;
        _resolver = resolver;
    }

    /// <inheritdoc />
    public async Task<CurrentAccount> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var identity = await _session.GetIdentityAsync(cancellationToken);
        if (identity is null)
        {
            return new CurrentAccount.None();
        }

        var resolution = await _resolver.ResolveAsync(identity, cancellationToken);

        return resolution switch
        {
            AccountResolution.Admin(var account) => new CurrentAccount.Admin(account),
            AccountResolution.User(var account) => new CurrentAccount.User(account),
            _ => new CurrentAccount.None(),
        };
    }
}