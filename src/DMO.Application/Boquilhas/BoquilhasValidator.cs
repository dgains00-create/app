using DMO.Domain.Boquilhas;
using DMO.Domain.Tools;

namespace DMO.Application.Boquilhas;

/// <summary>
/// The exact machine-readable validation error codes of the Boquilhas area (contract §12.2, closed
/// set).
/// </summary>
/// <remarks>
/// <c>TOOL_NOT_FOUND</c>/<c>TOOL_TYPE_MISMATCH</c>/<c>MACHINE_UNKNOWN</c>/<c>MACHINE_REQUIRED</c>/
/// <c>REFERENCE_REQUIRED</c> reuse the exact P2-T04 tokens; <c>REOPEN_REASON_REQUIRED</c> mirrors
/// the P2-T06 pattern. These are validation <b>inputs</b>, not domain states, and no code outside
/// this set is produced.</remarks>
public static class BoquilhasValidationErrors
{
    /// <summary>An append/edit quantity is not positive (whole-unit BQ count).</summary>
    public const string QuantityNotPositive = "QUANTITY_NOT_POSITIVE";

    /// <summary>The opening initial quantity (the Início) is missing/non-positive.</summary>
    public const string InitialQuantityRequired = "INITIAL_QUANTITY_REQUIRED";

    /// <summary>The movement type is not one of <c>inicio|saida|entrada|irreparavel</c>.</summary>
    public const string MovementTypeInvalid = "MOVEMENT_TYPE_INVALID";

    /// <summary>No machine of the aggregate's registered set was supplied (at least one required).</summary>
    public const string MachinesRequired = "MACHINES_REQUIRED";

    /// <summary>An external Saída requires a machine.</summary>
    public const string MachineRequired = "MACHINE_REQUIRED";

    /// <summary>A supplied machine is not one of the six settled operational machines.</summary>
    public const string MachineUnknown = "MACHINE_UNKNOWN";

    /// <summary>A supplied movement machine is not in the aggregate's registered machine set.</summary>
    public const string MachineNotInAggregate = "MACHINE_NOT_IN_AGGREGATE";

    /// <summary>An external Saída requires the final selected canonical repairer.</summary>
    public const string RepairerRequired = "REPAIRER_REQUIRED";

    /// <summary>The supplied repairer is not a real register row.</summary>
    public const string RepairerNotFound = "REPAIRER_NOT_FOUND";

    /// <summary>Neither anchor was supplied (exactly one of <c>bq_id</c>/<c>tool_id</c> is required).</summary>
    public const string AnchorRequired = "ANCHOR_REQUIRED";

    /// <summary>Both anchors were supplied (exclusive anchor).</summary>
    public const string AnchorConflict = "ANCHOR_CONFLICT";

    /// <summary>The supplied <c>tool_id</c> is not a real canonical Tool.</summary>
    public const string ToolNotFound = "TOOL_NOT_FOUND";

    /// <summary>The supplied <c>tool_id</c> is a Tool whose type is not <c>BQ</c>.</summary>
    public const string ToolTypeMismatch = "TOOL_TYPE_MISMATCH";

    /// <summary>The supplied <c>bq_id</c> is not a real <c>bq_contexts</c> row.</summary>
    public const string BqContextNotFound = "BQ_CONTEXT_NOT_FOUND";

    /// <summary>The manual utilisation still is outside 0–100 (or not a valid percentage).</summary>
    public const string UtilisationInvalid = "UTILISATION_INVALID";

    /// <summary>A supplied business/opening date is not well-formed.</summary>
    public const string BusinessDateInvalid = "BUSINESS_DATE_INVALID";

    /// <summary>Observations are blank (or not well-formed).</summary>
    public const string ObservationsInvalid = "OBSERVATIONS_INVALID";

    /// <summary>The reopen reason is blank.</summary>
    public const string ReopenReasonRequired = "REOPEN_REASON_REQUIRED";

    /// <summary>A required reference was not supplied (reused P2-T04 token).</summary>
    public const string ReferenceRequired = "REFERENCE_REQUIRED";

    /// <summary>A list/history filter value is unknown or ill-formed (incl. page/pageSize out of bounds).</summary>
    public const string FilterInvalid = "FILTER_INVALID";
}

/// <summary>
/// Pure static Boquilhas validator: it runs before any write/query and returns the exact contracted
/// §12.2 codes.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §9.2 (the closed code set) and §17.2/§19.2/§23.3/§24.2 (the shaped
/// validation steps). The validator owns no database access: contextual existence/type resolution
/// (Tool, BQ context, repairer, machine membership) happens in the service pre-checks and again
/// authoritatively inside the write transactions.
/// </remarks>
public static class BoquilhasValidator
{
    /// <summary>The contracted list/history page-size lower bound.</summary>
    public const int MinPageSize = 1;

    /// <summary>The contracted list/history page-size upper bound.</summary>
    public const int MaxPageSize = 100;

    /// <summary>The exact allowed <c>state</c> filter tokens of the list/history queries.</summary>
    public static readonly IReadOnlyList<string> StateFilterTokens =
    [
        BoquilhaStatusTokens.ToToken(BoquilhaStatus.Active),
        BoquilhaStatusTokens.ToToken(BoquilhaStatus.Closed),
    ];

    // ------------------------------------------------------------------ commands (§9.2)

    /// <summary>Validates an aggregate opening command (route 7, §22.2).</summary>
    public static IReadOnlyList<string> Validate(CreateBoquilhasCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();

        AnchorValidation(command.BqId, command.ToolId, errors);

        if (command.Machines is null || command.Machines.Count == 0)
        {
            errors.Add(BoquilhasValidationErrors.MachinesRequired);
        }
        else
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var machine in command.Machines)
            {
                if (!MachineCode.IsKnown(machine))
                {
                    errors.Add(BoquilhasValidationErrors.MachineUnknown);
                }

                // A duplicated machine is not a valid machine SET (the same closed token; the DB
                // unique key <c>boquilha_machines_boquilhas_machine_key</c> is the backstop with
                // the identical mapping — §7.2 "no other 23505 source maps to ActiveAggregateExists").
                if (!seen.Add(machine))
                {
                    errors.Add(BoquilhasValidationErrors.MachineUnknown);
                }
            }
        }

        if (command.InitialQuantity <= 0)
        {
            errors.Add(command.InitialQuantity == 0
                ? BoquilhasValidationErrors.InitialQuantityRequired
                : BoquilhasValidationErrors.QuantityNotPositive);
        }

        if (command.OpeningDate == default)
        {
            errors.Add(BoquilhasValidationErrors.BusinessDateInvalid);
        }

        ValidateUtilisation(command.UtilisationPercent, errors);
        ValidateObservations(command.Observations, errors);

        return errors;
    }

    /// <summary>Validates a movement append command (route 8, §17.2 steps 1/3).</summary>
    public static IReadOnlyList<string> Validate(AppendMovementCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();

        ValidateMovementCore(command.MovementType, command.Quantity, command.BusinessDate, errors);
        ValidateMachineShape(command.Machine, errors);
        ValidateRepairerShape(command.RepairerId, errors);
        ValidateObservations(command.Observations, errors);

        return errors;
    }

    /// <summary>Validates a movement edit command (route 9, §19.1/§19.2).</summary>
    public static IReadOnlyList<string> Validate(EditMovementCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();

        ValidateMovementCore("saida", command.Quantity, command.BusinessDate, errors);

        // Movement type and recorded_at are immutable by carrier shape: neither is carried here.
        ValidateMachineShape(command.Machine, errors);
        ValidateRepairerShape(command.RepairerId, errors);
        ValidateObservations(command.Observations, errors);

        return errors;
    }

    /// <summary>Validates the reopen reason shape (route 11, §23.3 step 4).</summary>
    public static IReadOnlyList<string> Validate(ReopenBoquilhasCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return string.IsNullOrWhiteSpace(command.Reason)
            ? [BoquilhasValidationErrors.ReopenReasonRequired]
            : [];
    }

    /// <summary>Validates an opening-facts update command (route 12, §23.4 step 3).</summary>
    public static IReadOnlyList<string> Validate(UpdateOpeningFactsCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();

        if (command.OpeningDate == default)
        {
            errors.Add(BoquilhasValidationErrors.BusinessDateInvalid);
        }

        ValidateUtilisation(command.UtilisationPercent, errors);
        ValidateObservations(command.Observations, errors);

        if (command.Machines is null || command.Machines.Count == 0)
        {
            errors.Add(BoquilhasValidationErrors.MachinesRequired);
        }
        else
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var machine in command.Machines)
            {
                if (!MachineCode.IsKnown(machine))
                {
                    errors.Add(BoquilhasValidationErrors.MachineUnknown);
                }

                // Same duplicate-machine rule as the opening command (the identical closed token;
                // the DB unique key is the backstop with the identical mapping).
                if (!seen.Add(machine))
                {
                    errors.Add(BoquilhasValidationErrors.MachineUnknown);
                }
            }
        }

        return errors;
    }

    /// <summary>Validates the Registo lot-grid query (route 4): state/machine filters + paging.</summary>
    public static IReadOnlyList<string> Validate(BoquilhasListQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var errors = new List<string>();

        ValidateStateFilter(query.State, errors);

        if (query.Machine is not null && !MachineCode.IsKnown(query.Machine))
        {
            errors.Add(BoquilhasValidationErrors.FilterInvalid);
        }

        ValidatePaging(query.Page, query.PageSize, errors);

        return errors;
    }

    /// <summary>
    /// Validates the local Histórico query (route 18, §24.2): every filter is a closed-value or
    /// well-formed predicate; unknown/ill-formed values → <c>FILTER_INVALID</c> (never a silent full
    /// list).
    /// </summary>
    public static IReadOnlyList<string> Validate(BoquilhasHistoryQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var errors = new List<string>();

        ValidateStateFilter(query.State, errors);

        if (query.Machine is not null && !MachineCode.IsKnown(query.Machine))
        {
            errors.Add(BoquilhasValidationErrors.FilterInvalid);
        }

        if (query.MovementType is not null && MovementKindTokens.Parse(query.MovementType) is null)
        {
            errors.Add(BoquilhasValidationErrors.FilterInvalid);
        }

        if (query.BusinessDateFrom is { } from && query.BusinessDateTo is { } to && from > to)
        {
            errors.Add(BoquilhasValidationErrors.FilterInvalid);
        }

        ValidatePaging(query.Page, query.PageSize, errors);

        return errors;
    }

    /// <summary>Validates the BQ-association command shape (route 15, §22.3).</summary>
    public static IReadOnlyList<string> Validate(AssociateBqCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command.ToolId == Guid.Empty ? [BoquilhasValidationErrors.ToolNotFound] : [];
    }

    // ------------------------------------------------------------------ helpers

    private static void AnchorValidation(Guid? bqId, Guid? toolId, List<string> errors)
    {
        if (bqId is null && toolId is null)
        {
            errors.Add(BoquilhasValidationErrors.AnchorRequired);
        }
        else if (bqId is not null && toolId is not null)
        {
            errors.Add(BoquilhasValidationErrors.AnchorConflict);
        }
    }

    private static void ValidateMovementCore(
        string movementType,
        int quantity,
        DateOnly businessDate,
        List<string> errors)
    {
        if (MovementKindTokens.Parse(movementType) is null)
        {
            errors.Add(BoquilhasValidationErrors.MovementTypeInvalid);
        }

        if (quantity <= 0)
        {
            errors.Add(BoquilhasValidationErrors.QuantityNotPositive);
        }

        if (businessDate == default)
        {
            errors.Add(BoquilhasValidationErrors.BusinessDateInvalid);
        }
    }

    private static void ValidateMachineShape(string? machine, List<string> errors)
    {
        if (machine is null)
        {
            return;
        }

        if (!MachineCode.IsKnown(machine))
        {
            errors.Add(BoquilhasValidationErrors.MachineUnknown);
        }
    }

    private static void ValidateRepairerShape(Guid? repairerId, List<string> errors)
    {
        if (repairerId is null)
        {
            return;
        }

        if (repairerId == Guid.Empty)
        {
            errors.Add(BoquilhasValidationErrors.RepairerNotFound);
        }
    }

    private static void ValidateUtilisation(decimal? utilisationPercent, List<string> errors)
    {
        if (utilisationPercent is { } value && (value < 0 || value > 100))
        {
            errors.Add(BoquilhasValidationErrors.UtilisationInvalid);
        }
    }

    private static void ValidateObservations(string? observations, List<string> errors)
    {
        if (observations is not null && string.IsNullOrWhiteSpace(observations))
        {
            errors.Add(BoquilhasValidationErrors.ObservationsInvalid);
        }
    }

    private static void ValidateStateFilter(string? state, List<string> errors)
    {
        if (state is not null && !StateFilterTokens.Contains(state, StringComparer.Ordinal))
        {
            errors.Add(BoquilhasValidationErrors.FilterInvalid);
        }
    }

    private static void ValidatePaging(int page, int pageSize, List<string> errors)
    {
        if (page < 1)
        {
            errors.Add(BoquilhasValidationErrors.FilterInvalid);
        }

        if (pageSize < MinPageSize || pageSize > MaxPageSize)
        {
            errors.Add(BoquilhasValidationErrors.FilterInvalid);
        }
    }
}