namespace DMO.Domain.Controlo;

/// <summary>
/// One current operational glass density (g/cm³) of a processo, maintained in
/// <c>Controlo_Create → Definições</c>.
/// </summary>
/// <remarks>
/// Authority: P2-T05 post-closure correction contract §5.1/§5.2 (Owner rule
/// GLASS_DENSITY_CONFIGURATION): <c>glass_density_settings</c> holds exactly two rows, one per
/// canonical processo (<c>NNPB</c>/<c>PS</c>), seeded with the provenance-backed authoritative
/// values. The <see cref="Processo"/> token is the closed two-value set owned by the domain
/// (text + CHECK, mirroring the <c>machine_repairer_assignments.machine</c> precedent — no
/// second process catalog is invented).
/// <para>
/// This row is the <b>current operational value</b> — the source for NEW Peso calculations only.
/// The value effectively used by a Peso is frozen on the Peso itself
/// (<c>pesos.glass_density_g_cm3</c>) and is never re-resolved, rewritten or silently
/// recalculated afterwards. No per-<c>tool_id</c> density exists anywhere (R1).</para>
/// </remarks>
public sealed record GlassDensitySetting(
    string Processo,
    decimal DensityGCm3,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);