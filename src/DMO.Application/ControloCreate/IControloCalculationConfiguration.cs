using DMO.Domain.Tools;

namespace DMO.Application.ControloCreate;

/// <summary>
/// The backend calculation configuration consumed by the Peso formulas (Q-CALC).
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §5.2/§5.3 and Q-CALC. The formula mechanics are contracted exactly;
/// the concrete <b>values</b> (water-temperature divisor table and processo → glass-density
/// mapping) exist in no repository authority and arrive as <b>backend calculation configuration</b>
/// — not a Definições area, not hardcoded. Until the values exist, every
/// calculate/create/update/submit that needs them is refused with the typed
/// <c>calculation-configuration-missing</c> refusal and nothing is written (MES9/AC-M9); no value
/// is ever invented and no silent zero is ever produced. The configuration-backed implementation
/// lives in DMO.Infrastructure; tests and test hosts supply their own implementations with fixed
/// values.
/// </remarks>
public interface IControloCalculationConfiguration
{
    /// <summary>
    /// Resolves the divisor value (g/cm³) for the entered water temperature, or <c>false</c> when
    /// the divisor table has no configured value for it.
    /// </summary>
    bool TryGetWaterDivisor(decimal waterTemperature, out decimal divisor);

    /// <summary>
    /// Resolves the glass density (g/cm³) for the Tool's processo, or <c>false</c> when the
    /// processo→density calibration mapping has no configured value for it.
    /// </summary>
    bool TryGetGlassDensity(Processo? processo, out decimal density);
}