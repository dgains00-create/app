namespace DMO.Application.Access;

/// <summary>
/// One registered/available assignable Module definition of the current build.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Id"/> is the authority and must belong to the canonical
/// <see cref="ModuleCatalog"/> — Admin/other parties cannot invent identities. The
/// presentation and navigation metadata (<see cref="DisplayName"/>,
/// <see cref="DestinationId"/>, <see cref="Surface"/>) never grant anything by themselves.
/// </para>
/// <para>
/// Structural invariants (id ∈ canonical catalog; destination/contextual consistency) are
/// enforced by <see cref="ModuleRegistry"/> at construction time, so an invalid definition
/// can never become valid runtime access; the registry is the product's validation boundary.
/// </para>
/// </remarks>
/// <param name="Id">Canonical Module identity.</param>
/// <param name="DisplayName">Presentation name (identity is <see cref="Id"/>, never this text).</param>
/// <param name="DestinationId">Stable visible destination, or <c>null</c> for contextual-only Modules.</param>
/// <param name="IsDefaultLandingEligible">
/// Whether the Module may be the Template default/landing destination (P1-T07 consumes this;
/// P1-T04 only exposes it).
/// </param>
/// <param name="Surface">Canonical surface descriptor of the Module.</param>
public sealed record ModuleDefinition(
    ModuleId Id,
    string DisplayName,
    string? DestinationId,
    bool IsDefaultLandingEligible,
    ModuleSurfaceDescriptor Surface);