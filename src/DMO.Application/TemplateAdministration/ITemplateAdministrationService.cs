using DMO.Application.UserAdministration;

namespace DMO.Application.TemplateAdministration;

/// <summary>
/// Template administration service contract (P1-T06).
/// </summary>
/// <remarks>
/// <para>
/// Every operation returns a closed-set <see cref="TemplateAdministrationResult"/>; no
/// operation throws for flow control. This is the typed outcome surface P1-T09 instruments
/// later — P1-T06 implements <b>no audit</b> of any kind.
/// </para>
/// <para>
/// The service orchestrates the application persistence primitives and the Module Registry
/// (availability/landing validation); it is pure Application code (no EF, no HTTP, no Razor).
/// Module ids are canonical identities only; no display-name or destination-id is ever
/// persisted as permission identity.
/// </para>
/// </remarks>
public interface ITemplateAdministrationService
{
    /// <summary>Lists all Templates with their composition presentation, landing and associated-USER count.</summary>
    Task<IReadOnlyList<TemplateListItem>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Returns the Template ficha (facts, ordered composition, landing validity, associated USERs), or <c>null</c>.</summary>
    Task<TemplateFicha?> GetAsync(Guid templateId, CancellationToken cancellationToken);

    /// <summary>Lists Templates for the assignment selectors (read-only option value object).</summary>
    Task<IReadOnlyList<TemplateOption>> ListTemplateOptionsAsync(CancellationToken cancellationToken);

    /// <summary>Creates a Template with its ordered Module composition (version starts at 1).</summary>
    Task<TemplateAdministrationResult> CreateAsync(
        TemplateAdministrationCommands.CreateTemplateCommand command,
        CancellationToken cancellationToken);

    /// <summary>Edits Template facts, composition/order and landing (stale expected version → conflict).</summary>
    Task<TemplateAdministrationResult> UpdateAsync(
        TemplateAdministrationCommands.UpdateTemplateCommand command,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the Template with the exact atomic delete-with-members flow (version verified
    /// inside the destructive transaction; every <c>users.template_id</c> reference is nulled
    /// in the same transaction; composition cascade-removed; USERs remain, active state kept).
    /// </summary>
    Task<TemplateAdministrationResult> DeleteAsync(
        TemplateAdministrationCommands.DeleteTemplateCommand command,
        CancellationToken cancellationToken);

    /// <summary>
    /// Assigns/removes/reassigns the single USER ↔ Template association through the exact same
    /// persisted relation as the USER ficha (<c>users.template_id</c>, versioned).
    /// </summary>
    Task<TemplateAdministrationResult> SetTemplateUserAsync(
        TemplateAdministrationCommands.SetTemplateUserCommand command,
        CancellationToken cancellationToken);
}