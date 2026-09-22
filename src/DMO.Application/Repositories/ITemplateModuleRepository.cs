using DMO.Application.Templates;

namespace DMO.Application.Repositories;

/// <summary>
/// Read primitives for the persisted Template → Module composition.
/// </summary>
/// <remarks>
/// The composition is owned by <see cref="ITemplateRepository"/> write transactions; this
/// interface exposes reads only. <see cref="TemplateModule.ModuleId"/> values are stable
/// code-defined identities — there is no Module-definition administration table.
/// </remarks>
public interface ITemplateModuleRepository
{
    /// <summary>Returns the composition entries of the given Template, in presentation order.</summary>
    Task<IReadOnlyList<TemplateModule>> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken);
}