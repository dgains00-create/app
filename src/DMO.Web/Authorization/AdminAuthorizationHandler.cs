using DMO.Application.Session;
using Microsoft.AspNetCore.Authorization;

namespace DMO.Web.Authorization;

/// <summary>
/// Server-side gate handler for <see cref="AdminAuthorizationRequirement"/> (P1-T05).
/// </summary>
/// <remarks>
/// <para>
/// The handler decides from the accepted current-account boundary
/// (<see cref="ICurrentAccountContext"/>, which re-resolves the account from persistence on
/// every request): exactly one way to allow — the current account is the dedicated active
/// ADMIN. Every other state fails closed.
/// </para>
/// <para>
/// ADMIN access is never derived from <c>role</c> labels, Templates, operational Modules,
/// provider claims or navigation visibility. A USER is denied regardless of its role label
/// (including a role label of "Admin") and regardless of any Template/Modules it holds; there
/// is no super-user bypass in either direction.
/// </para>
/// <para>
/// The handler is scoped (it consumes the scoped <see cref="ICurrentAccountContext"/>) — the
/// same lifetime lesson applied to <see cref="ModuleAuthorizationHandler"/>.
/// </para>
/// </remarks>
public sealed class AdminAuthorizationHandler : AuthorizationHandler<AdminAuthorizationRequirement>
{
    private readonly ICurrentAccountContext _currentAccount;

    /// <summary>Creates the handler over the current-account context.</summary>
    public AdminAuthorizationHandler(ICurrentAccountContext currentAccount)
    {
        ArgumentNullException.ThrowIfNull(currentAccount);
        _currentAccount = currentAccount;
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminAuthorizationRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        var current = await _currentAccount.GetCurrentAsync(CancellationToken.None);

        if (current is CurrentAccount.Admin)
        {
            context.Succeed(requirement);
            return;
        }

        // USER, no session, inactive/unresolved account: deny. (Resolution already fails
        // closed for inactive/unresolved accounts, producing CurrentAccount.None.)
        context.Fail();
    }
}