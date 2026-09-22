using DMO.Application.Accounts;

namespace DMO.Application.Access;

/// <summary>
/// Thin facade over <see cref="IAccessResolver"/> exposing resolve + per-Module checks.
/// </summary>
/// <remarks>
/// <see cref="HasModuleAsync"/> returns <c>false</c> for <b>any</b> denied resolution: a
/// denied (unknown or unavailable) composition denies the available sibling too — no partial
/// access survives an access-resolution denial. Gates never read role labels, Template names,
/// provider claims or navigation visibility.
/// </remarks>
public sealed class ModuleAccessService : IModuleAccessService
{
    private readonly IAccessResolver _resolver;

    /// <summary>Creates the service over the canonical resolver.</summary>
    public ModuleAccessService(IAccessResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolver = resolver;
    }

    /// <inheritdoc />
    public Task<AccessOutcome> ResolveUserAccessAsync(
        AccountResolution resolution,
        CancellationToken cancellationToken) =>
        _resolver.ResolveAccessAsync(resolution, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> HasModuleAsync(
        AccountResolution resolution,
        ModuleId requiredModule,
        CancellationToken cancellationToken)
    {
        var outcome = await _resolver.ResolveAccessAsync(resolution, cancellationToken);
        return outcome is AccessOutcome.Granted(_, var effectiveModules) &&
               effectiveModules.Any(module => module.Id == requiredModule);
    }
}