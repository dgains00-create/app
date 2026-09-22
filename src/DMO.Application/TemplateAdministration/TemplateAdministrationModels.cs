using DMO.Application.Access;

namespace DMO.Application.TemplateAdministration;

/// <summary>
/// P1-T06 command/result models for the Template administration surface.
/// </summary>
/// <remarks>
/// <para>
/// Commands carry application facts only. Template Module ids are canonical
/// <see cref="ModuleId"/> values (persisted as <c>module_id</c>); display names and
/// destination ids are never permission identities. <c>LandingDestinationId</c> is nullable
/// and validated against the final composition by the accepted rule: non-null landing must be
/// represented by at least one selected, currently available, non-contextual Module
/// destination. No active/inactive Template lifecycle exists.
/// </para>
/// <para>
/// Every operation returns a closed-set <see cref="TemplateAdministrationResult"/>; no
/// operation throws for flow control. This is the typed outcome surface P1-T09 instruments
/// later — P1-T06 implements <b>no audit</b> of any kind.
/// </para>
/// </remarks>
public static class TemplateAdministrationCommands
{
    /// <summary>Creates a Template with the ordered Module composition and optional landing.</summary>
    /// <param name="Name">Template display name (descriptive; no access meaning).</param>
    /// <param name="ModuleIds">Canonical Module ids in presentation order (1..n after normalization).</param>
    /// <param name="LandingDestinationId">Optional valid landing destination id, or <c>null</c>.</param>
    public sealed record CreateTemplateCommand(
        string Name,
        IReadOnlyList<string> ModuleIds,
        string? LandingDestinationId);

    /// <summary>Edits the Template facts, composition and landing against an expected version.</summary>
    public sealed record UpdateTemplateCommand(
        Guid TemplateId,
        string Name,
        IReadOnlyList<string> ModuleIds,
        string? LandingDestinationId,
        int ExpectedVersion);

    /// <summary>
    /// Deletes the Template (atomic: null every <c>users.template_id</c> reference, then
    /// delete the row; composition is cascade-removed). Expected version from the confirmed
    /// ficha; stale → conflict.
    /// </summary>
    public sealed record DeleteTemplateCommand(Guid TemplateId, int ExpectedVersion);

    /// <summary>
    /// Assigns, reassigns or removes the single USER ↔ Template association, executed through
    /// the exact same persisted relation as the USER ficha (<c>users.template_id</c>).
    /// </summary>
    /// <param name="TemplateId">The Template ficha being operated on (context anchor).</param>
    /// <param name="UserId">The USER being assigned/removed/reassigned.</param>
    /// <param name="TargetTemplateId">
    /// The desired association: <c>null</c> removes (<c>template_id = null</c>), the ficha id
    /// assigns, another Template id reassigns. Never <c>A + B</c>: a single column write.
    /// </param>
    /// <param name="UserExpectedVersion">Expected USER version (stale → conflict, no overwrite).</param>
    public sealed record SetTemplateUserCommand(
        Guid TemplateId,
        Guid UserId,
        Guid? TargetTemplateId,
        int UserExpectedVersion);
}

/// <summary>A Template row for the administration list.</summary>
/// <param name="AssociatedUserCount">
/// Number of USERs currently associated through <c>users.template_id</c> (shown before delete).
/// </param>
public sealed record TemplateListItem(
    Guid TemplateId,
    string Name,
    IReadOnlyList<TemplateModulePresentation> Modules,
    string? LandingDestinationId,
    bool LandingIsValid,
    int AssociatedUserCount,
    int Version);

/// <summary>
/// A Template ficha (details/edit view) for the administration surface: facts, the ordered
/// composition with per-Module resolution state, the landing validity and the associated USER
/// members.
/// </summary>
public sealed record TemplateFicha(
    Guid TemplateId,
    string Name,
    IReadOnlyList<TemplateModulePresentation> Modules,
    string? LandingDestinationId,
    bool LandingIsValid,
    IReadOnlyList<TemplateUserMember> Users,
    int Version);

/// <summary>
/// One persisted Template → Module entry with its registry resolution state, so the editor can
/// surface unavailable/unknown entries explicitly (never silently repair them).
/// </summary>
public sealed record TemplateModulePresentation(
    string ModuleId,
    string DisplayName,
    string? DestinationId,
    TemplateModuleState State);

/// <summary>Resolution state of one persisted Module id against the current build registry.</summary>
public enum TemplateModuleState
{
    /// <summary>Canonical and registered/available in this build.</summary>
    Available,

    /// <summary>Canonical but not registered/available in this build (future surface).</summary>
    KnownUnavailable,

    /// <summary>Not a valid/known canonical Module identity (corrupt or incompatible persisted state).</summary>
    Unknown,
}

/// <summary>One USER associated with the Template (through the single <c>users.template_id</c>).</summary>
public sealed record TemplateUserMember(
    Guid UserId,
    string Name,
    string CompanyNumber,
    string Role,
    bool Active,
    int Version);

/// <summary>Closed-set result of a Template administration operation.</summary>
public abstract record TemplateAdministrationResult
{
    /// <summary>The operation completed successfully; carries the affected Template ficha.</summary>
    public sealed record Success(TemplateFicha Ficha) : TemplateAdministrationResult;

    /// <summary>The Template was created.</summary>
    public sealed record Created(Guid TemplateId) : TemplateAdministrationResult;

    /// <summary>Validation rejected the command; no side effect occurred.</summary>
    public sealed record ValidationFailed(IReadOnlyList<string> Errors) : TemplateAdministrationResult;

    /// <summary>The target Template does not exist.</summary>
    public sealed record NotFound(Guid TemplateId) : TemplateAdministrationResult;

    /// <summary>The operation conflicted with current state (stale version).</summary>
    public sealed record Conflict(TemplateConflictReason Reason, string Message) : TemplateAdministrationResult;
}

/// <summary>Typed conflict reasons for <see cref="TemplateAdministrationResult.Conflict"/>.</summary>
public enum TemplateConflictReason
{
    /// <summary>The Template row was modified after it was observed; reload and retry.</summary>
    StaleTemplateVersion,

    /// <summary>The USER row was modified after it was observed; reload and retry.</summary>
    StaleUserVersion,
}