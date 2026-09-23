using DMO.Application.ControloCreate;
using DMO.Domain.Tools;
using Microsoft.Extensions.Configuration;

namespace DMO.Infrastructure.Configuration;

/// <summary>
/// The configuration-backed <see cref="IControloCalculationConfiguration"/>: the
/// <c>Controlo:Calculation</c> sections.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §5.2/§5.3 and Q-CALC. The formula mechanics are contracted exactly;
/// the concrete <b>values</b> (water-temperature divisor table and processo → glass-density
/// mapping) exist in no repository authority and arrive as <b>backend calculation configuration</b>
/// — not a Definições area, not hardcoded. The default shipped configuration intentionally carries
/// <b>no</b> table values; a deployment supplies them through configuration. Until they exist,
/// every calculate/create/update/submit that needs them returns the typed
/// <c>calculation-configuration-missing</c> refusal (MES9/AC-M9); no value is ever invented and no
/// silent zero is ever produced. Tests and test hosts supply their own implementations with fixed
/// values.
/// </remarks>
public sealed class ConfigurationCalculationConfiguration : IControloCalculationConfiguration
{
    private readonly IReadOnlyDictionary<decimal, decimal> _waterDivisors;
    private readonly IReadOnlyDictionary<string, decimal> _glassDensities;

    /// <summary>Section root of the whole calculation configuration.</summary>
    public const string SectionName = "Controlo:Calculation";

    /// <summary>Binds the configuration sections (missing sections = an empty configuration).</summary>
    public ConfigurationCalculationConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var divisors = configuration.GetSection($"{SectionName}:TemperatureDivisors").GetChildren()
            .Select(entry => new
            {
                Temperature = TryParseDecimal(entry["Temperature"]),
                Divisor = TryParseDecimal(entry["Divisor"]),
            })
            .Where(entry => entry.Temperature is not null && entry.Divisor is not null)
            .ToArray();

        _waterDivisors = divisors
            .GroupBy(entry => entry.Temperature!.Value)
            .ToDictionary(group => group.Key, group => group.First().Divisor!.Value);

        _glassDensities = configuration.GetSection($"{SectionName}:GlassDensities").GetChildren()
            .Select(entry => new
            {
                Processo = entry["Processo"],
                Density = TryParseDecimal(entry["Density"]),
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Processo) && entry.Density is not null)
            .ToDictionary(entry => entry.Processo!.Trim(), entry => entry.Density!.Value);
    }

    /// <inheritdoc />
    public bool TryGetWaterDivisor(decimal waterTemperature, out decimal divisor) =>
        _waterDivisors.TryGetValue(waterTemperature, out divisor);

    /// <inheritdoc />
    public bool TryGetGlassDensity(Processo? processo, out decimal density)
    {
        if (processo is not { } known)
        {
            density = default;
            return false;
        }

        var token = known switch
        {
            Processo.Nnpb => "NNPB",
            Processo.Ps => "PS",
            _ => throw new ArgumentOutOfRangeException(nameof(processo), processo, "Unknown processo."),
        };

        return _glassDensities.TryGetValue(token, out density);
    }

    private static decimal? TryParseDecimal(string? value) =>
        decimal.TryParse(
            value,
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : null;
}