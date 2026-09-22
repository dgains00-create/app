namespace DMO.Application.Access;

/// <summary>
/// Reason an operational access resolution was denied.
/// </summary>
/// <remarks>
/// Every denial reason denies the <b>entire</b> resolution: no partial effective Module set
/// is ever produced and no sibling Module survives a denied resolution.
/// </remarks>
public enum AccessDenialReason
{
    /// <summary>The presented resolution is not an operational USER (ADMIN / NoAccess).</summary>
    NotOperationalUser,

    /// <summary>The active USER has no Template association (<c>users.template_id</c> is null).</summary>
    NoTemplate,

    /// <summary>The USER's <c>template_id</c> points to a Template row that does not exist.</summary>
    TemplateMissing,

    /// <summary>The persisted Template composition contains an unknown/invalid Module identity.</summary>
    UnknownModule,

    /// <summary>
    /// The persisted Template composition contains a canonical Module identity that is known
    /// but not registered/available in the current build.
    /// </summary>
    UnavailableModule,

    /// <summary>Repository/infrastructure failure while resolving access — never surfaces as a grant.</summary>
    ResolutionFailure,
}

/// <summary>
/// Result of an operational access resolution.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Granted"/> contains <b>only</b> a fully valid effective Module set: every
/// persisted <c>module_id</c> was valid and available, in deterministic persisted
/// presentation order. There is no <c>ExcludedUnavailable</c> and no partial result: any
/// <see cref="Denied"/> state denies the whole resolution.
/// </para>
/// <para>
/// <see cref="Granted"/> with an empty effective set is a valid outcome (a fully interpreted
/// composition without Modules); per-Module enforcement then denies everything in practice.
/// Presentation order never changes authorization semantics.
/// </para>
/// </remarks>
public abstract record AccessOutcome
{
    private AccessOutcome()
    {
    }

    /// <summary>The resolution succeeded with a fully valid effective Module set.</summary>
    /// <param name="LandingDestinationId">
    /// The nullable landing destination id persisted on the Template (P1-T06 fact /
    /// ACCESS_MODEL §3). It is a routing fact carried from the existing Template read; it
    /// never grants anything and is not permission authority (P1-T07 accepted contract).
    /// </param>
    /// <param name="EffectiveModules">
    /// The effective Module definitions, in deterministic persisted presentation order.
    /// </param>
    public sealed record Granted(
        string? LandingDestinationId,
        IReadOnlyList<ModuleDefinition> EffectiveModules) : AccessOutcome;

    /// <summary>The resolution was denied for <paramref name="Reason"/>; nothing is granted.</summary>
    /// <param name="Reason">The denial reason.</param>
    public sealed record Denied(AccessDenialReason Reason) : AccessOutcome;
}