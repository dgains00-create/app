using DMO.Domain.Tools;

namespace DMO.Application.ControloCreate;

/// <summary>
/// The backend calculation configuration consumed by the Peso formulas (Q-CALC).
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §5.2/§5.3 and Q-CALC, as clarified by the Owner
/// (WATER_TEMPERATURE_TO_WATER_DENSITY_LOOKUP). The two lookups are <b>separate facts</b>:
/// <list type="bullet">
/// <item><b>Water density</b> — resolved automatically from the <b>entered water temperature</b>
/// through the application's authoritative water-temperature table (per whole degree 5–35 °C,
/// the closest whole degree by <see cref="MidpointRounding.AwayFromZero"/>; <b>no
/// interpolation</b>). The operator never enters a water density and never selects a divisor:
/// the "valor da tabela de temperatura" of the contracted formulas <i>is</i> the water density
/// corresponding to the entered temperature. It is used to calculate capacity
/// (<c>capacity = water weight ÷ water density</c>); it is never persisted as a Peso fact itself
/// (the frozen per-row results are the historical truth).</item>
/// <item><b>Glass density</b> — resolved from the anchor's <c>processo</c>
/// (<c>cm_id → tool_id → processo</c>; pending: <c>tool_id → processo</c>) through the
/// processo→density calibration mapping; used later for the glass weight and frozen onto the
/// Peso. Never confused with water density, never entered by the operator.</item>
/// </list>
/// The water-temperature table is <b>application calculation configuration</b>: not Peso-owned
/// editable user data, not an operator-typed field, not part of <c>Controlo → Definições</c>, not
/// a business identity and not a user-CRUD table. It ships with the application as the
/// authoritative built-in table and may be overridden only by backend calculation configuration.
/// <para>
/// Until a value is resolvable, every calculate/create/update/submit that needs it is refused
/// with the typed <c>calculation-configuration-missing</c> refusal and nothing is written
/// (MES9/AC-M9); no value is ever invented and no silent zero is ever produced (the fail-closed
/// rule covers an authoritative lookup point missing from an override table exactly like a
/// missing mapping — an entered valid-range temperature for which no density can be resolved
/// must keep failing closed). The configuration-backed implementation lives in
/// DMO.Infrastructure; tests and test hosts supply their own implementations with fixed values.
/// </para>
/// </remarks>
public interface IControloCalculationConfiguration
{
    /// <summary>
    /// Resolves the <b>water density</b> (g/cm³) for the entered water temperature, or
    /// <c>false</c> when no authoritative table entry exists for it.
    /// </summary>
    /// <remarks>
    /// The authoritative lookup rule: the entered temperature is rounded to the nearest whole
    /// degree with <see cref="MidpointRounding.AwayFromZero"/> and the exact 5–35 °C entry of
    /// the water-temperature table is returned. The rounded degree outside 5–35, or a whole
    /// degree absent from a supplied override table, resolves to <c>false</c> — <b>no
    /// interpolation</b> between entries, no fallback formula, no invented value. Implementations
    /// MUST apply this exact rule so the resolved value is always the table density of the
    /// entered temperature's closest whole degree.
    /// </remarks>
    bool TryGetWaterDensity(decimal waterTemperature, out decimal waterDensity);

    /// <summary>
    /// Resolves the glass density (g/cm³) for the Tool's processo, or <c>false</c> when the
    /// processo→density calibration mapping has no configured value for it.
    /// </summary>
    /// <remarks>
    /// Independent from <see cref="TryGetWaterDensity"/>: this lookup is fed by the anchor's
    /// <c>processo</c>, never by the water temperature.
    /// </remarks>
    bool TryGetGlassDensity(Processo? processo, out decimal density);
}