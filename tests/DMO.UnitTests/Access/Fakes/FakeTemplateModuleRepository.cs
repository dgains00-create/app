using DMO.Application.Repositories;
using DMO.Application.Templates;

namespace DMO.UnitTests.Access.Fakes;

/// <summary>
/// In-memory <see cref="ITemplateModuleRepository"/> for access tests. Seeded entries are
/// returned in seeded order (mimicking the persisted <c>presentation_order</c>).
/// </summary>
public sealed class FakeTemplateModuleRepository : ITemplateModuleRepository
{
    private readonly Dictionary<Guid, IReadOnlyList<TemplateModule>> _byTemplate = [];

    /// <summary>When set, every read throws this failure (infrastructure-failure tests).</summary>
    public Exception? Failure { get; set; }

    /// <summary>
    /// Seeds a composition for a Template in the given order (presentation order
    /// 1..n in sequence).
    /// </summary>
    public void Seed(Guid templateId, params string[] moduleIds)
    {
        _byTemplate[templateId] = moduleIds
            .Select((moduleId, index) => new TemplateModule(templateId, moduleId, index + 1))
            .ToArray();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TemplateModule>> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken)
    {
        ThrowIfFailed();
        return Task.FromResult(_byTemplate.GetValueOrDefault(templateId) ?? []);
    }

    private void ThrowIfFailed()
    {
        if (Failure is not null)
        {
            throw Failure;
        }
    }
}