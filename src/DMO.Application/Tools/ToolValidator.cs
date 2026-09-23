using DMO.Domain.Tools;

namespace DMO.Application.Tools;

/// <summary>
/// The exact machine-readable validation error codes of the Tool area.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §12.3/§12.4 (closed set). These are validation <b>inputs</b>, not
/// domain states, and no code outside this set is produced.
/// </remarks>
public static class ToolValidationErrors
{
    /// <summary>The required reference/lot identity text was not supplied.</summary>
    public const string ReferenceRequired = "REFERENCE_REQUIRED";

    /// <summary>No Tool type was supplied.</summary>
    public const string ToolTypeRequired = "TOOL_TYPE_REQUIRED";

    /// <summary>The supplied Tool type is not one of <c>CM</c>/<c>MF</c>/<c>BQ</c>.</summary>
    public const string ToolTypeUnknown = "TOOL_TYPE_UNKNOWN";

    /// <summary>The supplied <c>processo</c> is neither <c>NNPB</c>, <c>PS</c> nor absent.</summary>
    public const string ProcessoUnknown = "PROCESSO_UNKNOWN";

    /// <summary>The supplied canonical quantity is negative.</summary>
    public const string QuantityNegative = "QUANTITY_NEGATIVE";

    /// <summary>No machine-compatibility entry was supplied (at least one is required).</summary>
    public const string MachineCompatibilityRequired = "MACHINE_COMPATIBILITY_REQUIRED";

    /// <summary>A supplied machine is not one of the six settled operational machines.</summary>
    public const string MachineUnknown = "MACHINE_UNKNOWN";

    /// <summary>No search criterion was supplied (no unbounded registry dump is contracted).</summary>
    public const string SearchCriteriaRequired = "SEARCH_CRITERIA_REQUIRED";

    /// <summary>The search limit is outside <c>1..100</c>.</summary>
    public const string LimitOutOfRange = "LIMIT_OUT_OF_RANGE";
}

/// <summary>
/// Pure static Tool validator: it runs before any write and returns the exact contracted codes.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §5.3.2 and §12.3. The validator owns no database access: contextual
/// existence and type-match resolution happens inside the write transaction in the repository.
/// </remarks>
public static class ToolValidator
{
    /// <summary>The contracted maximum Tool search limit.</summary>
    public const int MaxLimit = 100;

    /// <summary>The contracted minimum Tool search limit.</summary>
    public const int MinLimit = 1;

    /// <summary>Validates a Tool create command before any write.</summary>
    /// <remarks>
    /// A blank <c>lot</c> is a missing required identity text fact. The contracted code set has no
    /// separate lot token, so the required reference/lot identity text is reported with
    /// <see cref="ToolValidationErrors.ReferenceRequired"/>; no code outside the closed set is
    /// produced (contract §12.4).
    /// </remarks>
    public static IReadOnlyList<string> Validate(CreateToolCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.Type))
        {
            errors.Add(ToolValidationErrors.ToolTypeRequired);
        }
        else if (ToolTokens.ParseType(command.Type.Trim()) is null)
        {
            errors.Add(ToolValidationErrors.ToolTypeUnknown);
        }

        if (string.IsNullOrWhiteSpace(command.Reference) || string.IsNullOrWhiteSpace(command.Lot))
        {
            errors.Add(ToolValidationErrors.ReferenceRequired);
        }

        if (!string.IsNullOrWhiteSpace(command.Processo) &&
            ToolTokens.ParseProcesso(command.Processo.Trim()) is null)
        {
            errors.Add(ToolValidationErrors.ProcessoUnknown);
        }

        if (command.Quantity is < 0)
        {
            errors.Add(ToolValidationErrors.QuantityNegative);
        }

        var machines = command.CompatibleMachines?
            .Where(machine => !string.IsNullOrWhiteSpace(machine))
            .Select(machine => machine.Trim())
            .ToList() ?? [];

        if (machines.Count == 0)
        {
            errors.Add(ToolValidationErrors.MachineCompatibilityRequired);
        }
        else if (machines.Any(machine => !MachineCode.IsKnown(machine)))
        {
            errors.Add(ToolValidationErrors.MachineUnknown);
        }

        return errors;
    }

    /// <summary>Validates a canonical Tool search query before any read is executed.</summary>
    public static IReadOnlyList<string> Validate(ToolSearchQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var errors = new List<string>();

        if (query.Limit is < MinLimit or > MaxLimit)
        {
            errors.Add(ToolValidationErrors.LimitOutOfRange);
        }

        var hasQuery = !string.IsNullOrWhiteSpace(query.Query);
        var hasReference = !string.IsNullOrWhiteSpace(query.Reference);
        var hasLot = !string.IsNullOrWhiteSpace(query.Lot);

        if (!hasQuery && !hasReference && !hasLot && query.Type is null && query.Machine is null)
        {
            errors.Add(ToolValidationErrors.SearchCriteriaRequired);
        }

        return errors;
    }
}
