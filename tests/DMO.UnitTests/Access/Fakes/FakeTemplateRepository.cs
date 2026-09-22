using DMO.Application.Repositories;
using DMO.Application.Templates;

namespace DMO.UnitTests.Access.Fakes;

/// <summary>
/// In-memory <see cref="ITemplateRepository"/> for access tests.
/// </summary>
public sealed class FakeTemplateRepository : ITemplateRepository
{
    private readonly Dictionary<Guid, Template> _templates = [];

    /// <summary>When set, every read throws this failure (infrastructure-failure tests).</summary>
    public Exception? Failure { get; set; }

    /// <summary>Seeds one Template row.</summary>
    public void Seed(Template template) => _templates[template.TemplateId] = template;

    /// <inheritdoc />
    public Task<Template?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken)
    {
        ThrowIfFailed();
        return Task.FromResult(_templates.GetValueOrDefault(templateId));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Template>> ListAsync(CancellationToken cancellationToken)
    {
        ThrowIfFailed();
        return Task.FromResult<IReadOnlyList<Template>>(_templates.Values.ToArray());
    }

    /// <inheritdoc />
    public Task<Guid> CreatedAsync(Template template, IReadOnlyList<TemplateModule> modules, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Not used by access unit tests.");

    /// <inheritdoc />
    public Task UpdatedAsync(Template template, IReadOnlyList<TemplateModule> modules, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Not used by access unit tests.");

    /// <inheritdoc />
    public Task DeleteAsync(Guid templateId, int expectedVersion, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Not used by access unit tests.");

    private void ThrowIfFailed()
    {
        if (Failure is not null)
        {
            throw Failure;
        }
    }
}