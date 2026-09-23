using DMO.Domain.Tools;

namespace DMO.Application.JobOn;

/// <summary>
/// The exact machine-readable validation error codes of the Job On area.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §12.3/§12.4 (closed set). These are validation <b>inputs</b>, not
/// domain states, and no code outside this set is produced.
/// </remarks>
public static class JobOnValidationErrors
{
    /// <summary>The required production reference was not supplied.</summary>
    public const string ReferenceRequired = "REFERENCE_REQUIRED";

    /// <summary>The required production number was not supplied.</summary>
    public const string ProductionNumberRequired = "PRODUCTION_NUMBER_REQUIRED";

    /// <summary>No machine was supplied.</summary>
    public const string MachineRequired = "MACHINE_REQUIRED";

    /// <summary>The supplied machine is not one of the six settled operational machines.</summary>
    public const string MachineUnknown = "MACHINE_UNKNOWN";

    /// <summary>A supplied context Tool identity does not exist in the canonical registry.</summary>
    public const string ToolNotFound = "TOOL_NOT_FOUND";

    /// <summary>A supplied context Tool's type does not match the slot's context type.</summary>
    public const string ToolTypeMismatch = "TOOL_TYPE_MISMATCH";

    /// <summary>The delete was not explicitly confirmed.</summary>
    public const string DeleteNotConfirmed = "DELETE_NOT_CONFIRMED";

    /// <summary>A supplied association action is not one of Keep/Set/Remove, or is inconsistent.</summary>
    public const string AssociationActionInvalid = "ASSOCIATION_ACTION_INVALID";

    /// <summary>The same context type appears more than once in one association change list.</summary>
    public const string DuplicateAssociationType = "DUPLICATE_ASSOCIATION_TYPE";

    /// <summary>The duplication source was not supplied.</summary>
    public const string SourceJobOnNotFound = "SOURCE_JOBON_NOT_FOUND";
}

/// <summary>
/// Pure static Job On validator: it runs before any write and returns the exact contracted codes.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §11.1/§11.2/§11.5 and §12.3 rule 1. The validator performs no database
/// access: Tool existence and type-match resolution happens inside the write transaction, so a race
/// cannot bypass it.
/// </remarks>
public static class JobOnValidator
{
    /// <summary>Validates the reference → productions query.</summary>
    public static IReadOnlyList<string> Validate(FindProductionsQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        return string.IsNullOrWhiteSpace(query.Reference)
            ? [JobOnValidationErrors.ReferenceRequired]
            : [];
    }

    /// <summary>Validates a Job On create command before any write.</summary>
    public static IReadOnlyList<string> Validate(CreateJobOnCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();

        AddFactErrors(errors, command.Reference, command.ProductionNumber, command.Machine, requireReference: true);

        // A bare canonical identity is the only way the create command expresses an association, so
        // an empty value is a resolution failure, never "no association" (the create command has no
        // change list; absence is expressed by null).
        foreach (var toolId in new[] { command.CmToolId, command.MfToolId, command.BqToolId })
        {
            if (toolId == Guid.Empty)
            {
                errors.Add(JobOnValidationErrors.ToolNotFound);
            }
        }

        return errors;
    }

    /// <summary>Validates a Job On edit command before any write.</summary>
    public static IReadOnlyList<string> Validate(UpdateJobOnCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();

        AddFactErrors(errors, command.Reference, command.ProductionNumber, command.Machine, requireReference: true);

        var seen = new HashSet<ToolContextType>();
        foreach (var association in command.Associations ?? [])
        {
            if (!Enum.IsDefined(association.ContextType) || !Enum.IsDefined(association.Action))
            {
                errors.Add(JobOnValidationErrors.AssociationActionInvalid);
                continue;
            }

            if (!seen.Add(association.ContextType))
            {
                errors.Add(JobOnValidationErrors.DuplicateAssociationType);
            }

            switch (association.Action)
            {
                case ToolAssociationAction.Set
                    when association.ToolId is null || association.ToolId == Guid.Empty:
                    errors.Add(JobOnValidationErrors.ToolNotFound);
                    break;

                case ToolAssociationAction.Keep or ToolAssociationAction.Remove
                    when association.ToolId is not null:
                    // Keep writes nothing and Remove deletes the row: neither carries a Tool value.
                    errors.Add(JobOnValidationErrors.AssociationActionInvalid);
                    break;
            }
        }

        return errors;
    }

    /// <summary>Validates a Job On duplication command before any write.</summary>
    public static IReadOnlyList<string> Validate(DuplicateJobOnCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();

        if (command.SourceJobOnId == Guid.Empty)
        {
            errors.Add(JobOnValidationErrors.SourceJobOnNotFound);
        }

        // The duplicated occurrence keeps the source reference, so only the new production number
        // and the new machine are supplied facts.
        AddFactErrors(
            errors,
            reference: null,
            command.ProductionNumber,
            command.Machine,
            requireReference: false);

        return errors;
    }

    /// <summary>Validates a Job On delete command before any write.</summary>
    public static IReadOnlyList<string> Validate(DeleteJobOnCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();

        if (!command.DeleteConfirmed)
        {
            errors.Add(JobOnValidationErrors.DeleteNotConfirmed);
        }

        return errors;
    }

    private static void AddFactErrors(
        List<string> errors,
        string? reference,
        string? productionNumber,
        string? machine,
        bool requireReference)
    {
        if (requireReference && string.IsNullOrWhiteSpace(reference))
        {
            errors.Add(JobOnValidationErrors.ReferenceRequired);
        }

        if (string.IsNullOrWhiteSpace(productionNumber))
        {
            errors.Add(JobOnValidationErrors.ProductionNumberRequired);
        }

        if (string.IsNullOrWhiteSpace(machine))
        {
            errors.Add(JobOnValidationErrors.MachineRequired);
        }
        else if (!MachineCode.IsKnown(machine.Trim()))
        {
            errors.Add(JobOnValidationErrors.MachineUnknown);
        }
    }
}
