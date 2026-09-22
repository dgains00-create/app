using DMO.Application.Accounts;

namespace DMO.Application.Access;

/// <summary>
/// Canonical operational access resolver: USER → Template → persisted Template Modules →
/// canonical Module Registry → effective Module set.
/// </summary>
/// <remarks>
/// <para>
/// The resolver is the fail-closed decision point of the access model. It never reads the
/// USER role label, the Template name or any provider claim; authorization comes exclusively
/// from the Modules selected in the USER's effective Template, resolved through the registry
/// (ACCESS_MODEL §3/§5/§13).
/// </para>
/// <para>
/// Resolution is atomic: one unknown or known-but-unavailable persisted Module denies the
/// <b>entire</b> resolution. No partial grants, no best-effort reinterpretation of a
/// composition; recovery of invalid/obsolete compositions belongs to future Template
/// administration, never to the runtime.
/// </para>
/// </remarks>
public interface IAccessResolver
{
    /// <summary>
    /// Resolves operational access for the given account resolution, fail-closed.
    /// </summary>
    /// <param name="resolution">The P1-T02 account resolution (active USER only, or deny).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="AccessOutcome.Granted"/> or <see cref="AccessOutcome.Denied"/>.</returns>
    Task<AccessOutcome> ResolveAccessAsync(
        AccountResolution resolution,
        CancellationToken cancellationToken);
}