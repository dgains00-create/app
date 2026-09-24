using DMO.Domain.Boquilhas;
using DMO.Domain.Tools;

namespace DMO.Application.Boquilhas;

/// <summary>
/// The exact machine-readable validation error codes of the Boquilhas area (the small closed set
/// of the OWNER CLARIFICATION model).
/// </summary>
/// <remarks>
/// <c>MACHINE_UNKNOWN</c>/<c>MACHINE_REQUIRED</c>/<c>REFERENCE_REQUIRED</c> reuse the exact P2-T04
/// tokens; <c>REPAIRER_REQUIRED</c>/<c>REPAIRER_NOT_FOUND</c>/<c>BQ_CONTEXT_NOT_FOUND</c> keep the
/// P2-T07/consumed-token shapes. <b>Superseded (Owner clarification):</b> the lifecycle tokens
/// (UTILISATION_INVALID, REOPEN_REASON_REQUIRED, INITIAL_QUANTITY_REQUIRED, MACHINES_REQUIRED,
/// MACHINE_NOT_IN_AGGREGATE, ANCHOR_REQUIRED/ANCHOR_CONFLICT, TOOL_NOT_FOUND/TOOL_TYPE_MISMATCH)
/// are removed — the register has no opening facts, no standalone anchor and no quantity opening.
/// These are validation <b>inputs</b>, not domain states, and no code outside this set is produced.</remarks>
public static class BoquilhasValidationErrors
{
    /// <summary>An append/edit quantity is not positive (whole-unit BQ count).</summary>
    public const string QuantityNotPositive = "QUANTITY_NOT_POSITIVE";

    /// <summary>The movement type is not one of <c>saida|entrada|entrada_sem_reparacao</c>.</summary>
    public const string MovementTypeInvalid = "MOVEMENT_TYPE_INVALID";

    /// <summary>A Saída requires a machine.</summary>
    public const string MachineRequired = "MACHINE_REQUIRED";

    /// <summary>A supplied machine is not one of the six settled operational machines.</summary>
    public const string MachineUnknown = "MACHINE_UNKNOWN";

    /// <summary>A Saída requires the repairer used for the movement.</summary>
    public const string RepairerRequired = "REPAIRER_REQUIRED";

    /// <summary>The supplied repairer is not a real register row.</summary>
    public const string RepairerNotFound = "REPAIRER_NOT_FOUND";

    /// <summary>The supplied <c>bq_id</c> is not a real <c>bq_contexts</c> row.</summary>
    public const string BqContextNotFound = "BQ_CONTEXT_NOT_FOUND";

    /// <summary>A supplied business date is not well-formed.</summary>
    public const string BusinessDateInvalid = "BUSINESS_DATE_INVALID";

    /// <summary>Observations are blank (or not well-formed).</summary>
    public const string ObservationsInvalid = "OBSERVATIONS_INVALID";

    /// <summary>A required reference was not supplied (reused P2-T04 token).</summary>
    public const string ReferenceRequired = "REFERENCE_REQUIRED";

    /// <summary>A list/history filter value is unknown or ill-formed (incl. page/pageSize out of bounds).</summary>
    public const string FilterInvalid = "FILTER_INVALID";
}

/// <summary>
/// Pure static Boquilhas validator: it runs before any write/query and returns the exact closed
/// codes.
/// </summary>
/// <remarks>
/// The validator owns no database access: contextual existence resolution (BQ context, repairer)
/// happens in the service pre-checks and again authoritatively inside the write transactions.
/// <b>Superseded (Owner clarification):</b> the balance-relative rules (Saída ≤ Disponível,
/// Irreparável ≤ Em reparação) and the machine-set membership rule are removed — the register
/// records historical movement facts, and the outstanding value is derived, never validated.
/// </remarks>
public static class BoquilhasValidator
{
    /// <summary>The contracted list/history page-size lower bound.</summary>
    public const int MinPageSize = 1;

    /// <summary>The contracted list/history page-size upper bound.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Validates the register-creation command (route 4).</summary>
    public static IReadOnlyList<string> Validate(CreateBoquilhaRegisterCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command.BqId == Guid.Empty
            ? [BoquilhasValidationErrors.BqContextNotFound]
            : [];
    }

    /// <summary>Validates a movement append command (route 5).</summary>
    public static IReadOnlyList<string> Validate(AppendMovementCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();

        ValidateMovementCore(command.MovementType, command.Quantity, command.BusinessDate, errors);

        if (command.Machine is not null && !MachineCode.IsKnown(command.Machine))
        {
            errors.Add(BoquilhasValidationErrors.MachineUnknown);
        }

        if (command.RepairerId is { } repairerId && repairerId == Guid.Empty)
        {
            errors.Add(BoquilhasValidationErrors.RepairerNotFound);
        }

        // A Saída records WHO repairs and ON WHICH line: machine + repairer required (the same
        // external-Saída discipline; the CHECK backstop maps the same tokens).
        if (string.Equals(command.MovementType, "saida", StringComparison.Ordinal))
        {
            if (command.Machine is null)
            {
                errors.Add(BoquilhasValidationErrors.MachineRequired);
            }

            if (command.RepairerId is null)
            {
                errors.Add(BoquilhasValidationErrors.RepairerRequired);
            }
        }

        ValidateObservations(command.Observations, errors);

        return errors;
    }

    /// <summary>
    /// Validates a movement edit command (route 6). Movement type and <c>recorded_at</c> are
    /// immutable and not carried; the type-specific Saída required-facts rule is re-asserted by
    /// the service/repository from the STORED type (the edit form preserves the type).
    /// </summary>
    public static IReadOnlyList<string> Validate(EditMovementCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = ValidateMovementCoreShape(command.Quantity, command.BusinessDate);

        if (command.Machine is not null && !MachineCode.IsKnown(command.Machine))
        {
            errors.Add(BoquilhasValidationErrors.MachineUnknown);
        }

        if (command.RepairerId is { } repairerId && repairerId == Guid.Empty)
        {
            errors.Add(BoquilhasValidationErrors.RepairerNotFound);
        }

        ValidateObservations(command.Observations, errors);

        return errors;
    }

    /// <summary>Validates the BQ-association command shape (route 9).</summary>
    public static IReadOnlyList<string> Validate(AssociateBqCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command.ToolId == Guid.Empty ? [BoquilhasValidationErrors.BqContextNotFound] : [];
    }

    /// <summary>Validates the register list query (route 1): paging bounds.</summary>
    public static IReadOnlyList<string> Validate(BoquilhasListQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var errors = new List<string>();
        ValidatePaging(query.Page, query.PageSize, errors);
        return errors;
    }

    /// <summary>
    /// Validates the local Histórico query (route 12): every filter is a closed-value or
    /// well-formed predicate; unknown/ill-formed values → <c>FILTER_INVALID</c> (never a silent
    /// full list).
    /// </summary>
    public static IReadOnlyList<string> Validate(BoquilhasHistoryQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var errors = new List<string>();

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

    // ------------------------------------------------------------------ helpers

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

        errors.AddRange(ValidateMovementCoreShape(quantity, businessDate));
    }

    private static List<string> ValidateMovementCoreShape(int quantity, DateOnly businessDate)
    {
        var errors = new List<string>();

        if (quantity <= 0)
        {
            errors.Add(BoquilhasValidationErrors.QuantityNotPositive);
        }

        if (businessDate == default)
        {
            errors.Add(BoquilhasValidationErrors.BusinessDateInvalid);
        }

        return errors;
    }

    private static void ValidateObservations(string? observations, List<string> errors)
    {
        if (observations is not null && string.IsNullOrWhiteSpace(observations))
        {
            errors.Add(BoquilhasValidationErrors.ObservationsInvalid);
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