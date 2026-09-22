namespace DMO.Application.Templates;

/// <summary>
/// Application Template record used by the persistence primitives.
/// </summary>
/// <remarks>
/// <para>
/// P1-T03 persists Templates and their Module composition only. There is no Template
/// administration behaviour, no access/availability logic and no active/inactive lifecycle:
/// Templates use Create/Edit/Delete (Master). Module Registry and Access Resolver behaviour
/// is P1-T04.
/// </para>
/// <para>
/// <see cref="Version"/> is the optimistic-concurrency token observed at read time; a write
/// carrying a stale version is rejected with a typed conflict.
/// </para>
/// </remarks>
/// <param name="TemplateId">Stable Template identity.</param>
/// <param name="Name">Template display name.</param>
/// <param name="LandingDestinationId">Nullable landing destination identifier (validity is later access/module logic).</param>
/// <param name="Version">Optimistic-concurrency version observed at read time.</param>
public sealed record Template(
    Guid TemplateId,
    string Name,
    string? LandingDestinationId,
    int Version);