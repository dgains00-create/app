using DMO.Domain.Controlo;

namespace DMO.Application.Repositories;

/// <summary>
/// The single glass-density-settings repository contract (P2-T05 post-closure correction
/// contract §5.1/§5.4, baseline §20.2 pattern).
/// </summary>
/// <remarks>
/// One row per canonical processo (<c>NNPB</c>/<c>PS</c>; PK). The row carries the CURRENT
/// operational glass density maintained in <c>Controlo → Definições</c>; a Peso calculation
/// reads it at first successful calculate/save and freezes the value on the Peso
/// (<c>pesos.glass_density_g_cm3</c>) — the setting is never re-read for an existing Peso and
/// no Peso is ever rewritten by a settings change. Every write is version-guarded (Q-CONC
/// convention): a race surfaces as <c>ConcurrencyConflictException</c> → <c>stale-version</c>.
/// </remarks>
public interface IGlassDensitySettingsRepository
{
    /// <summary>Reads the current operational density of one processo, or <c>null</c> when the
    /// row is absent (defensive only — both rows are always seeded).</summary>
    Task<GlassDensitySetting?> GetByProcessoAsync(
        string processo,
        CancellationToken cancellationToken);

    /// <summary>Lists every current operational density (exactly the two canonical rows).</summary>
    Task<IReadOnlyList<GlassDensitySetting>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Updates the density of EXACTLY the supplied processo's row (never another processo's),
    /// version-guarded: one version increment per committed write, nothing written on staleness.
    /// </summary>
    Task<GlassDensitySetting> UpdatedAsync(
        GlassDensitySetting setting,
        CancellationToken cancellationToken);
}