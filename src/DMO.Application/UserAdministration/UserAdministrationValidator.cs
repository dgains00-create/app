using System.Text.RegularExpressions;

namespace DMO.Application.UserAdministration;

/// <summary>
/// Server-side validation of the USER administration commands (P1-T05).
/// </summary>
/// <remarks>
/// <para>
/// Validation is deliberately a pure static surface: no EF, no HTTP, no provider call. The DB
/// constraints and the provider remain backstops (unique company number, unique provider
/// subject, one identity per carrier email, FK on template_id).
/// </para>
/// <para>
/// There is <b>no password validation</b>: no password exists in any create flow — the USER
/// establishes their own credential through the provider invitation/setup flow.
/// </para>
/// </remarks>
public static class UserAdministrationValidator
{
    /// <summary>Reasonably bounded free-text role length (role remains free text; no enum).</summary>
    public const int MaxRoleLength = 64;

    /// <summary>Bounds for free-text display facts (no industrial format is invented).</summary>
    public const int MaxNameLength = 200;

    /// <summary>Bounds for the canonical company number text.</summary>
    public const int MaxCompanyNumberLength = 200;

    /// <summary>Bounds for the carrier email (RFC maximum).</summary>
    public const int MaxEmailLength = 320;

    private static readonly Regex ProviderCompatibleEmailPattern = new(
        @"^[^@\s]+@[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Validates the create command (application facts only).</summary>
    public static IReadOnlyList<string> Validate(UserAdministrationCommands.CreateUserCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();

        AddFactErrors(errors, command.Name, command.CompanyNumber, command.Email, command.Role);

        if (command.InviteRedirectUrl is not null
            && !IsValidAbsoluteHttpUrl(command.InviteRedirectUrl))
        {
            errors.Add(
                "Invite redirect URL must be an absolute http(s) URL when supplied; otherwise leave it empty.");
        }

        return errors;
    }

    /// <summary>Validates the edit command (application facts only).</summary>
    public static IReadOnlyList<string> Validate(UserAdministrationCommands.UpdateUserCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();
        AddFactErrors(errors, command.Name, command.CompanyNumber, command.Email, command.Role);
        return errors;
    }

    /// <summary>Validates an activate/deactivate command (boolean facts have no further rules).</summary>
    public static IReadOnlyList<string> Validate(UserAdministrationCommands.SetUserActiveCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return [];
    }

    /// <summary>Validates a Template assignment command (existence is checked against the repository).</summary>
    public static IReadOnlyList<string> Validate(UserAdministrationCommands.SetUserTemplateCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return [];
    }

    /// <summary>Validates a delete command.</summary>
    public static IReadOnlyList<string> Validate(UserAdministrationCommands.DeleteUserCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return [];
    }

    /// <summary>Validates a password-reset-confirmation command.</summary>
    public static IReadOnlyList<string> Validate(UserAdministrationCommands.RequestedPasswordResetCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return [];
    }

    /// <summary>Validates a resend-invite-confirmation command.</summary>
    public static IReadOnlyList<string> Validate(UserAdministrationCommands.ResendInviteCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return [];
    }

    /// <summary>True when the email shape is provider-compatible (basic RFC shape, no invention).</summary>
    public static bool IsValidProviderCompatibleEmail(string email) =>
        email.Length <= MaxEmailLength && ProviderCompatibleEmailPattern.IsMatch(email);

    /// <summary>True when the value is an absolute http(s) URL.</summary>
    public static bool IsValidAbsoluteHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var parsed)
        && parsed.Scheme is "https" or "http";

    private static void AddFactErrors(
        List<string> errors,
        string name,
        string companyNumber,
        string email,
        string role)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLength)
        {
            errors.Add($"Name must be non-blank and at most {MaxNameLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(companyNumber) || companyNumber.Length > MaxCompanyNumberLength)
        {
            errors.Add($"Company number must be non-blank and at most {MaxCompanyNumberLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add("Email is required.");
        }
        else if (!IsValidProviderCompatibleEmail(email))
        {
            errors.Add("Email must be a valid, provider-compatible email address.");
        }

        if (role.Length > MaxRoleLength)
        {
            errors.Add($"Role label must be at most {MaxRoleLength} characters.");
        }
    }
}