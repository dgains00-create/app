namespace DMO.Application.Access;

/// <summary>
/// Canonical surface descriptor of one assignable Module.
/// </summary>
/// <remarks>
/// <para>
/// This describes the product surface the Module projects onto. <see cref="DestinationId"/>
/// (when non-null) is a stable visible-navigation destination; <see cref="IsContextualOnly"/>
/// marks Modules (Ferramentas / Ferramentas Approve) that have no top-level destination and
/// are opened contextually from another surface — opening a Tool ficha from another Module
/// never grants Ferramentas access by itself (ACCESS_MODEL §11).
/// </para>
/// <para>
/// <see cref="ProtectedActions"/> is <b>documentation only</b>, derived from the Master
/// contract. It is never evaluated as a permission list: enforcement in P1-T04 is always by
/// canonical <see cref="ModuleId"/>; a second action-level access model is prohibited
/// (ACCESS_MODEL §7).
/// </para>
/// </remarks>
/// <param name="SurfaceName">Canonical surface name (e.g. <c>Controlo</c>, <c>Job On</c>).</param>
/// <param name="DestinationId">Stable visible destination, or <c>null</c> for contextual-only Modules.</param>
/// <param name="IsContextualOnly"><c>true</c> when the Module has no top-level destination.</param>
/// <param name="ProtectedActions">Master documentation of the surface/actions; never enforced directly.</param>
public sealed record ModuleSurfaceDescriptor(
    string SurfaceName,
    string? DestinationId,
    bool IsContextualOnly,
    IReadOnlyList<string> ProtectedActions);