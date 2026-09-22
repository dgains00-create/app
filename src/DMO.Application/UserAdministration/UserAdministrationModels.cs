using DMO.Application.Accounts;

namespace DMO.Application.UserAdministration;

/// <summary>
/// P1-T05 command/result models for the USER administration surface.
/// </summary>
/// <remarks>
/// <para>
/// Commands carry application facts only. <b>No password exists in any command</b>: initial
/// credentials come from the provider invitation/setup flow and password replacement from the
/// provider recovery flow — the ADMIN never supplies or sees a USER password.
/// </para>
/// <para>
/// <c>company_number</c> is the canonical USER login identifier; <c>email</c> is the provider
/// carrier. <c>role</c> is free text, presentation only.
/// </para>
/// </remarks>
public static class UserAdministrationCommands
{
    /// <summary>Creates a USER through the provider invitation flow.</summary>
    /// <param name="Name">Display name (application-owned).</param>
    /// <param name="CompanyNumber">Canonical USER login identifier (application-owned).</param>
    /// <param name="Email">Provider carrier email (application + provider owned).</param>
    /// <param name="Role">Presentation-only free-text role label.</param>
    /// <param name="Active">Explicit initial active state (no silent default).</param>
    /// <param name="TemplateId">Optional Template association (single nullable relationship).</param>
    /// <param name="InviteRedirectUrl">Optional absolute http(s) URL for the provider invitation link.</param>
    public sealed record CreateUserCommand(
        string Name,
        string CompanyNumber,
        string Email,
        string Role,
        bool Active,
        Guid? TemplateId,
        string? InviteRedirectUrl);

    /// <summary>Edits the application-owned facts of an existing USER.</summary>
    public sealed record UpdateUserCommand(
        Guid UserId,
        string Name,
        string CompanyNumber,
        string Email,
        string Role,
        int ExpectedVersion);

    /// <summary>Activates or deactivates a USER (application state only).</summary>
    public sealed record SetUserActiveCommand(Guid UserId, bool Active, int ExpectedVersion);

    /// <summary>Assigns, reassigns or removes the single Template association.</summary>
    public sealed record SetUserTemplateCommand(Guid UserId, Guid? TemplateId, int ExpectedVersion);

    /// <summary>Deletes a USER (provider identity first, then the version-checked row).</summary>
    public sealed record DeleteUserCommand(Guid UserId, int ExpectedVersion);

    /// <summary>Initiates the provider password-recovery flow for a USER.</summary>
    public sealed record RequestedPasswordResetCommand(Guid UserId, int ExpectedVersion);

    /// <summary>Re-sends the provider setup invitation for a USER.</summary>
    public sealed record ResendInviteCommand(Guid UserId, int ExpectedVersion);
}

/// <summary>A USER row for the administration list.</summary>
public sealed record UserListItem(
    Guid UserId,
    string Name,
    string CompanyNumber,
    string Email,
    string Role,
    bool Active,
    Guid? TemplateId,
    string? TemplateName,
    int Version);

/// <summary>A USER ficha (details view) for the administration surface.</summary>
/// <remarks>
/// <c>auth_identity_id</c> is internal provider linkage and is deliberately absent from this
/// model: it is never presented in forms or responses.
/// </remarks>
public sealed record UserFicha(
    Guid UserId,
    string Name,
    string CompanyNumber,
    string Email,
    string Role,
    bool Active,
    Guid? TemplateId,
    string? TemplateName,
    int Version);

/// <summary>Read-only Template option for the assignment selector (P1-T05 does not edit Templates).</summary>
public sealed record TemplateOption(Guid TemplateId, string Name);

/// <summary>Closed-set result of a USER administration operation.</summary>
public abstract record UserAdministrationResult
{
    /// <summary>The operation completed successfully (edit/activate/…).</summary>
    public sealed record Success(UserAccount Account) : UserAdministrationResult;

    /// <summary>The USER was created through a fresh provider invitation.</summary>
    public sealed record Created(UserAccount Account) : UserAdministrationResult;

    /// <summary>
    /// The create was retried and the existing row provably equals the same completed logical
    /// create result (strict replay, never assumed).
    /// </summary>
    public sealed record IdempotentReplay(UserAccount Account) : UserAdministrationResult;

    /// <summary>
    /// The create adopted a provably orphaned provider identity (exact carrier-email match,
    /// no application row referencing its subject).
    /// </summary>
    public sealed record RecoveredProvisioning(UserAccount Account) : UserAdministrationResult;

    /// <summary>Validation rejected the command; no side effect occurred.</summary>
    public sealed record ValidationFailed(IReadOnlyList<string> Errors) : UserAdministrationResult;

    /// <summary>The target USER does not exist.</summary>
    public sealed record NotFound(Guid UserId) : UserAdministrationResult;

    /// <summary>The operation conflicted with current state (stale version / duplicates).</summary>
    public sealed record Conflict(UserConflictReason Reason, string Message) : UserAdministrationResult;

    /// <summary>The privileged provider operation failed (explicit, never reported as success).</summary>
    public sealed record ProviderFailed(ProviderFailureKind Kind) : UserAdministrationResult;
}

/// <summary>Typed conflict reasons for <see cref="UserAdministrationResult.Conflict"/>.</summary>
public enum UserConflictReason
{
    /// <summary>The row was modified after it was observed; reload and retry.</summary>
    StaleVersion,

    /// <summary>The company number is already in use by another USER.</summary>
    DuplicateCompanyNumber,

    /// <summary>The carrier email is already in use.</summary>
    EmailInUse,
}

/// <summary>Typed provider failure kinds for <see cref="UserAdministrationResult.ProviderFailed"/>.</summary>
public enum ProviderFailureKind
{
    /// <summary>The provider was unreachable or rate-limited (retryable).</summary>
    Unavailable,

    /// <summary>The provider rejected or failed the operation.</summary>
    Error,
}