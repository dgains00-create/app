using DMO.Application.Templates;

namespace DMO.Application.Repositories;

/// <summary>
/// Persistence primitives for Templates and their Module composition.
/// </summary>
/// <remarks>
/// <para>
/// P1-T03 provides these primitives only; Template administration workflows (P1-T06) and
/// Module Registry/Access Resolver behaviour (P1-T04) are later phases. Template create and
/// update persist the <c>template_modules</c> composition in the same transaction as the
/// <c>templates</c> row; the Template row version is the concurrency boundary for the whole
/// composition.
/// </para>
/// </remarks>
public interface ITemplateRepository
{
    /// <summary>Returns the Template with the given id, or <c>null</c>.</summary>
    Task<Template?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken);

    /// <summary>Returns all Templates.</summary>
    Task<IReadOnlyList<Template>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Inserts the Template and its composition atomically, returning the new Template id.</summary>
    Task<Guid> CreatedAsync(Template template, IReadOnlyList<TemplateModule> modules, CancellationToken cancellationToken);

    /// <summary>Updates the Template and replaces its composition atomically (stale version → conflict).</summary>
    Task UpdatedAsync(Template template, IReadOnlyList<TemplateModule> modules, CancellationToken cancellationToken);

    /// <summary>Deletes the Template against an expected version (stale version → conflict).</summary>
    Task DeleteAsync(Guid templateId, int expectedVersion, CancellationToken cancellationToken);
}