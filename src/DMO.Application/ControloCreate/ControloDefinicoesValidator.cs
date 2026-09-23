using DMO.Domain.Tools;

namespace DMO.Application.ControloCreate;

/// <summary>
/// The exact machine-readable validation error codes of <c>Controlo_Create → Definições</c>
/// (closed set, §26.2).
/// </summary>
public static class ControloDefinicoesValidationErrors
{
    /// <summary>The repairer name was not supplied (blank/whitespace).</summary>
    public const string NameRequired = "NAME_REQUIRED";

    /// <summary>The supplied repairer for an assignment does not exist in the register.</summary>
    public const string RepairerNotFound = "REPAIRER_NOT_FOUND";

    /// <summary>The base directory was not supplied (blank/whitespace).</summary>
    public const string DirectoryRequired = "DIRECTORY_REQUIRED";

    /// <summary>The base directory is not an absolute path.</summary>
    public const string DirectoryInvalid = "DIRECTORY_INVALID";

    /// <summary>The machine code is not one of B1/B2/B3/C1/C2/C3.</summary>
    public const string MachineUnknown = "MACHINE_UNKNOWN";

    /// <summary>The email-list name was not supplied (blank/whitespace).</summary>
    public const string ListNameRequired = "LIST_NAME_REQUIRED";

    /// <summary>A recipient address was not supplied (blank/whitespace).</summary>
    public const string AddressRequired = "ADDRESS_REQUIRED";

    /// <summary>A recipient address does not have the minimal unbroken shape.</summary>
    public const string AddressInvalid = "ADDRESS_INVALID";

    /// <summary>The template name was not supplied (blank/whitespace).</summary>
    public const string TemplateNameRequired = "TEMPLATE_NAME_REQUIRED";

    /// <summary>The template subject was not supplied (blank/whitespace).</summary>
    public const string SubjectRequired = "SUBJECT_REQUIRED";

    /// <summary>The template body was not supplied (blank/whitespace).</summary>
    public const string BodyRequired = "BODY_REQUIRED";

    /// <summary>The template document type is not one of peso/pegamentos/resumo.</summary>
    public const string DocumentTypeUnknown = "DOCUMENT_TYPE_UNKNOWN";

    /// <summary>The delete was not explicitly confirmed.</summary>
    public const string DeleteNotConfirmed = "DELETE_NOT_CONFIRMED";
}

/// <summary>
/// Pure static Definições validator: it runs before any write and returns the exact contracted
/// codes (§10.4, §12.2, §13.2, §14.2).
/// </summary>
public static class ControloDefinicoesValidator
{
    /// <summary>Validates the add-repairer command (name is the only required data).</summary>
    public static IReadOnlyList<string> Validate(CreateRepairerCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return string.IsNullOrWhiteSpace(command.Name)
            ? [ControloDefinicoesValidationErrors.NameRequired]
            : [];
    }

    /// <summary>Validates the rename-repairer command.</summary>
    public static IReadOnlyList<string> Validate(RenameRepairerCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();

        if (command.RepairerId == Guid.Empty)
        {
            errors.Add(ControloDefinicoesValidationErrors.NameRequired);
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add(ControloDefinicoesValidationErrors.NameRequired);
        }

        return errors;
    }

    /// <summary>Validates a one-machine assignment set/change/clear command.</summary>
    public static IReadOnlyList<string> Validate(SetMachineAssignmentCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.Machine) || !MachineCode.IsKnown(command.Machine.Trim()))
        {
            errors.Add(ControloDefinicoesValidationErrors.MachineUnknown);
        }

        // A null repairer id is the explicit clear; an EMPTY id is a resolution failure.
        if (command.RepairerId == Guid.Empty)
        {
            errors.Add(ControloDefinicoesValidationErrors.RepairerNotFound);
        }

        return errors;
    }

    /// <summary>Validates a one-machine assignment clear command.</summary>
    public static IReadOnlyList<string> Validate(ClearMachineAssignmentCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.Machine) || !MachineCode.IsKnown(command.Machine.Trim()))
        {
            errors.Add(ControloDefinicoesValidationErrors.MachineUnknown);
        }

        return errors;
    }

    /// <summary>Validates the PDF-directory configure/change command.</summary>
    public static IReadOnlyList<string> Validate(SetPdfDirectoryCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.BaseDirectory))
        {
            errors.Add(ControloDefinicoesValidationErrors.DirectoryRequired);
        }
        else if (!Path.IsPathRooted(command.BaseDirectory.Trim()))
        {
            // The configured path is a server-host absolute filesystem path (Q-PDF); a non-absolute
            // value is refused here so only absolute paths reach the accessibility check.
            errors.Add(ControloDefinicoesValidationErrors.DirectoryInvalid);
        }

        return errors;
    }

    /// <summary>Validates the email-list create command.</summary>
    public static IReadOnlyList<string> Validate(CreateEmailListCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return ValidateList(command.Name, command.Recipients);
    }

    /// <summary>Validates the email-list update command.</summary>
    public static IReadOnlyList<string> Validate(UpdateEmailListCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = ValidateList(command.Name, command.Recipients).ToList();

        if (command.EmailListId == Guid.Empty)
        {
            errors.Add(ControloDefinicoesValidationErrors.ListNameRequired);
        }

        return errors;
    }

    /// <summary>Validates the email-list delete command (explicit confirmation).</summary>
    public static IReadOnlyList<string> Validate(DeleteEmailListCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command.DeleteConfirmed
            ? []
            : [ControloDefinicoesValidationErrors.DeleteNotConfirmed];
    }

    /// <summary>Validates the email-template create command.</summary>
    public static IReadOnlyList<string> Validate(CreateEmailTemplateCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return ValidateTemplate(command.Name, command.Subject, command.Body, command.DocumentType);
    }

    /// <summary>Validates the email-template update command.</summary>
    public static IReadOnlyList<string> Validate(UpdateEmailTemplateCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = ValidateTemplate(
            command.Name,
            command.Subject,
            command.Body,
            command.DocumentType).ToList();

        if (command.EmailTemplateId == Guid.Empty)
        {
            errors.Add(ControloDefinicoesValidationErrors.TemplateNameRequired);
        }

        return errors;
    }

    /// <summary>Validates the email-template delete command (explicit confirmation).</summary>
    public static IReadOnlyList<string> Validate(DeleteEmailTemplateCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command.DeleteConfirmed
            ? []
            : [ControloDefinicoesValidationErrors.DeleteNotConfirmed];
    }

    private static IReadOnlyList<string> ValidateList(string name, IReadOnlyList<string> recipients)
    {
        ArgumentNullException.ThrowIfNull(recipients);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(ControloDefinicoesValidationErrors.ListNameRequired);
        }

        foreach (var address in recipients)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                errors.Add(ControloDefinicoesValidationErrors.AddressRequired);
            }
            else if (!IsMinimalAddressShape(address.Trim()))
            {
                errors.Add(ControloDefinicoesValidationErrors.AddressInvalid);
            }
        }

        return errors;
    }

    private static IReadOnlyList<string> ValidateTemplate(
        string name,
        string subject,
        string body,
        string? documentType)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(ControloDefinicoesValidationErrors.TemplateNameRequired);
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            errors.Add(ControloDefinicoesValidationErrors.SubjectRequired);
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            errors.Add(ControloDefinicoesValidationErrors.BodyRequired);
        }

        if (documentType is not null)
        {
            var token = documentType.Trim();
            var known = token switch
            {
                "peso" or "pegamentos" or "resumo" => true,
                _ => false,
            };

            if (!known)
            {
                errors.Add(ControloDefinicoesValidationErrors.DocumentTypeUnknown);
            }
        }

        return errors;
    }

    /// <summary>
    /// The minimal unbroken address shape (Q-ADDR): exactly one <c>@</c>, non-blank local part and
    /// domain, no whitespace. No full RFC validation is invented.
    /// </summary>
    public static bool IsMinimalAddressShape(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        var trimmed = address.Trim();

        if (trimmed != address || trimmed.Any(char.IsWhiteSpace))
        {
            return false;
        }

        var separator = trimmed.IndexOf('@');
        if (separator <= 0 || separator != trimmed.LastIndexOf('@'))
        {
            return false;
        }

        return separator < trimmed.Length - 1;
    }
}