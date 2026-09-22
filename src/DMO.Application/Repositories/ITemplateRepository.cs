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

    // ---- P1-T06 additive write (no schema impact) -------------------------------------

    /// <summary>
    /// Deletes the Template with the accepted atomic delete-with-members sequence in
    /// <b>one</b> transaction: the expected version is verified <b>inside</b> the
    /// destructive transaction (stale → conflict, rollback, nothing changed); every
    /// <c>users.template_id</c> reference to the Template is nulled in the same transaction;
    /// then the Template row is deleted (composition cascade-removes
    /// <c>template_modules</c>). USER rows are never cascade-deleted and keep their active
    /// state; <c>users.template_id</c> becomes <c>null</c>.
    /// </summary>
    /// <remarks>
    /// P1-T06 addition: the P1-T03 <see cref="DeleteAsync"/> deliberately fails at the
    /// database layer when the Template is referenced (<c>users.template_id</c> is ON DELETE
    /// RESTRICT); this primitive is the atomic null-out workflow that P1-T03 explicitly
    /// deferred to P1-T06.
    /// </remarks>
    Task DeleteWithMembersAsync(Guid templateId, int expectedVersion, CancellationToken cancellationToken);
}