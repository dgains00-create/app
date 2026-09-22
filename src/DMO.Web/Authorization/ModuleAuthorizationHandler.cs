using DMO.Application.Accounts;
using DMO.Application.Access;
using DMO.Application.Session;
using Microsoft.AspNetCore.Authorization;

namespace DMO.Web.Authorization;

/// <summary>
/// Server-side gate handler for <see cref="ModuleAuthorizationRequirement"/>.
/// </summary>
/// <remarks>
/// <para>
/// The handler always resolves the current effective access through the Module access service
/// (registry + persisted Template composition). It grants nothing from provider claims, role
/// labels, Template names or navigation visibility.
/// </para>
/// <para>
/// ADMIN (and no-session) fail: ADMIN is a separate account type with no operational Template
/// and no implicit super-user bypass for operational Module gates
/// (ACCESS_MODEL §12 / ADMIN contract §10 / request §12).
/// </para>
/// </remarks>
public sealed class ModuleAuthorizationHandler : AuthorizationHandler<ModuleAuthorizationRequirement>
{
    private readonly ICurrentAccountContext _currentAccount;
    private readonly IModuleAccessService _accessService;

    /// <summary>Creates the handler over the current-account context and the access service.</summary>
    public ModuleAuthorizationHandler(
        ICurrentAccountContext currentAccount,
        IModuleAccessService accessService)
    {
        ArgumentNullException.ThrowIfNull(currentAccount);
        ArgumentNullException.ThrowIfNull(accessService);
        _currentAccount = currentAccount;
        _accessService = accessService;
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ModuleAuthorizationRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        var current = await _currentAccount.GetCurrentAsync(CancellationToken.None);

        if (current is not CurrentAccount.User(var user))
        {
            // No session / ADMIN: fail closed. No super-user bypass.
            context.Fail();
            return;
        }

        var hasModule = await _accessService.HasModuleAsync(
            new AccountResolution.User(user),
            requirement.RequiredModule,
            CancellationToken.None);

        if (hasModule)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}