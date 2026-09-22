namespace DMO.Application.Templates;

/// <summary>
/// One persisted Template → Module composition entry.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ModuleId"/> is a stable, code-defined Module Registry identity — it is not a
/// foreign key and there is no Module-definition administration table authority. P1-T04 owns
/// registry/access resolution; P1-T03 persists composition only.
/// </para>
/// </remarks>
/// <param name="TemplateId">The owning Template.</param>
/// <param name="ModuleId">Stable code-defined Module Registry identity.</param>
/// <param name="PresentationOrder">Deterministic presentation order within the Template (unique per Template).</param>
public sealed record TemplateModule(
    Guid TemplateId,
    string ModuleId,
    int PresentationOrder);