namespace DMO.Application.UserAdministration;

/// <summary>
/// USER administration service contract (P1-T05).
/// </summary>
/// <remarks>
/// <para>
/// Every operation returns a closed-set <see cref="UserAdministrationResult"/>; no operation
/// throws for flow control. This is the typed outcome surface P1-T09 instruments later —
/// P1-T05 implements <b>no audit</b> of any kind.
/// </para>
/// <para>
/// The service coordinates the application persistence primitives and the privileged provider
/// boundary; it is pure Application code (no EF, no HTTP, no Razor).
/// </para>
/// </remarks>
public interface IUserAdministrationService
{
    /// <summary>Lists every USER (active and inactive) with Template names.</summary>
    Task<IReadOnlyList<UserListItem>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Returns the USER ficha, or <c>null</c> when the USER does not exist.</summary>
    Task<UserFicha?> GetAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Lists Templates for the read-only assignment selector (P1-T05 does not edit Templates).</summary>
    Task<IReadOnlyList<TemplateOption>> ListTemplateOptionsAsync(CancellationToken cancellationToken);

    /// <summary>Creates a USER: validates, coordinates the provider invitation, persists the row.</summary>
    Task<UserAdministrationResult> CreateAsync(
        UserAdministrationCommands.CreateUserCommand command,
        CancellationToken cancellationToken);

    /// <summary>Re-sends the provider setup invitation (explicit ADMIN action; distinct from reset).</summary>
    Task<UserAdministrationResult> ResendInviteAsync(
        UserAdministrationCommands.ResendInviteCommand command,
        CancellationToken cancellationToken);

    /// <summary>Edits the application-owned USER facts; coordinates the provider when the carrier email changes.</summary>
    Task<UserAdministrationResult> UpdateAsync(
        UserAdministrationCommands.UpdateUserCommand command,
        CancellationToken cancellationToken);

    /// <summary>Activates or deactivates the USER (application state only).</summary>
    Task<UserAdministrationResult> SetActiveAsync(
        UserAdministrationCommands.SetUserActiveCommand command,
        CancellationToken cancellationToken);

    /// <summary>Assigns, reassigns or removes the single Template association.</summary>
    Task<UserAdministrationResult> SetTemplateAsync(
        UserAdministrationCommands.SetUserTemplateCommand command,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the USER: non-destructive version pre-check, provider identity first, then the
    /// version-checked row delete (recoverable partial state on the provider-success +
    /// stale-row-race).
    /// </summary>
    Task<UserAdministrationResult> DeleteAsync(
        UserAdministrationCommands.DeleteUserCommand command,
        CancellationToken cancellationToken);

    /// <summary>Initiates the provider password-recovery flow (explicit confirmation; active users only).</summary>
    Task<UserAdministrationResult> InitiatePasswordResetAsync(
        UserAdministrationCommands.RequestedPasswordResetCommand command,
        CancellationToken cancellationToken);
}