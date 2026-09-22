using DMO.Application.Accounts;

namespace DMO.Application.Access;

/// <summary>
/// Reusable surface/action access service: resolve effective USER Modules and answer
/// per-Module access checks for protected routes/actions.
/// </summary>
/// <remarks>
/// Any future endpoint/service declares <i>requires canonical Module X</i> and calls
/// <see cref="HasModuleAsync"/>; a <c>false</c> result fails closed (403). The service is a
/// thin facade over <see cref="IAccessResolver"/>/<see cref="IModuleRegistry"/> — it never
/// grants based on provider claims, role labels, Template names or navigation visibility.
/// </remarks>
public interface IModuleAccessService
{
    /// <summary>Resolves the full effective access for the given account resolution.</summary>
    Task<AccessOutcome> ResolveUserAccessAsync(
        AccountResolution resolution,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns whether the resolved effective Module set (fully valid) contains the required
    /// canonical Module. Any denied resolution returns <c>false</c>.
    /// </summary>
    Task<bool> HasModuleAsync(
        AccountResolution resolution,
        ModuleId requiredModule,
        CancellationToken cancellationToken);
}