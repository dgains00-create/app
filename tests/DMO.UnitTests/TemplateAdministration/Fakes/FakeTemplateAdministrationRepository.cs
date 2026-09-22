using DMO.Application.Persistence;
using DMO.Application.Repositories;
using DMO.Application.Templates;
using DMO.UnitTests.UserAdministration.Fakes;

namespace DMO.UnitTests.TemplateAdministration.Fakes;

/// <summary>
/// In-memory <see cref="ITemplateRepository"/> + <see cref="ITemplateModuleRepository"/>
/// implementation for the Template administration tests: enforces the dense 1..n
/// presentation order, bumps the Template version on every write, compares versions on every
/// versioned write (mirroring the real repository contract) and implements the atomic
/// delete-with-members null-out against the <b>shared</b> user store — the same store the
/// <see cref="FakeUserRepository"/> writes, so USER ↔ Template transversality is provable in
/// memory.
/// </summary>
public sealed class FakeTemplateAdministrationRepository : ITemplateRepository, ITemplateModuleRepository
{
    private sealed class Row
    {
        public Template Template { get; set; } = null!;

        /// <summary>Composition already in dense 1..n order.</summary>
        public List<TemplateModule> Modules { get; } = [];
    }

    private readonly List<Row> _rows = [];
    private readonly FakeUserRepository _users;

    /// <summary>
    /// When set, invoked at the start of <see cref="DeleteWithMembersAsync"/>: tests use it to
    /// simulate the concurrent writer between the delete pre-check and the destructive
    /// transaction (the accepted delete-race posture).
    /// </summary>
    public Action<Guid>? BeforeDeleteWithMembers { get; set; }

    /// <summary>All persisted Templates, for assertions.</summary>
    public IReadOnlyList<Template> Templates => _rows.Select(row => row.Template).ToArray();

    /// <summary>Returns the persisted composition of one Template (dense 1..n order).</summary>
    public IReadOnlyList<TemplateModule> CompositionsOf(Guid templateId)
    {
        var row = _rows.FirstOrDefault(candidate => candidate.Template.TemplateId == templateId);
        return row?.Modules.ToArray() ?? [];
    }

    /// <summary>
    /// Test probe: bumps the Template version, simulating a concurrent writer (used by the
    /// delete-race tests between the pre-check and the destructive transaction).
    /// </summary>
    public void HookBumpVersion(Guid templateId, int expectedVersion)
    {
        var index = FindIndex(templateId);
        var row = _rows[index];
        EnsureVersion(row, expectedVersion);
        row.Template = row.Template with { Version = expectedVersion + 1 };
    }

    /// <summary>
    /// Test probe: sets the Template landing to an arbitrary value, simulating persisted state
    /// written by a future build whose landing is not represented by the current composition.
    /// </summary>
    public void NullLandingValidityProbe(Guid templateId, string landingDestinationId)
    {
        var index = FindIndex(templateId);
        _rows[index].Template = _rows[index].Template with { LandingDestinationId = landingDestinationId };
    }

    /// <summary>Creates the store over the shared user store (delete nulls users.template_id).</summary>
    public FakeTemplateAdministrationRepository(FakeUserRepository users)
    {
        ArgumentNullException.ThrowIfNull(users);
        _users = users;
    }

    // ------------------------------------------------------------- ITemplateRepository

    /// <inheritdoc />
    public Task<Template?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken) =>
        Task.FromResult(_rows.FirstOrDefault(row => row.Template.TemplateId == templateId)?.Template);

    /// <inheritdoc />
    public Task<IReadOnlyList<Template>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Template>>(_rows.Select(row => row.Template).ToArray());

    /// <inheritdoc />
    public Task<Guid> CreatedAsync(
        Template template,
        IReadOnlyList<TemplateModule> modules,
        CancellationToken cancellationToken)
    {
        var templateId = Guid.NewGuid();
        _rows.Add(new Row
        {
            Template = template with { TemplateId = templateId, Version = 1 },
        });
        ReplaceComposition(templateId, modules);
        return Task.FromResult(templateId);
    }

    /// <inheritdoc />
    public Task UpdatedAsync(
        Template template,
        IReadOnlyList<TemplateModule> modules,
        CancellationToken cancellationToken)
    {
        var index = FindIndex(template.TemplateId);
        var row = _rows[index];

        if (row.Template.Version != template.Version)
        {
            throw new ConcurrencyConflictException(
                $"Template '{template.TemplateId}' was modified concurrently; reload and retry (fake).");
        }

        row.Template = template with { Version = template.Version + 1 };
        ReplaceComposition(template.TemplateId, modules);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid templateId, int expectedVersion, CancellationToken cancellationToken)
    {
        // P1-T03 primitive: a referenced Template fails to delete (RESTRICT) — mirrored here
        // so the P1-T06 tests demonstrate the atomic delete-with-members is the working path.
        if (_users.Accounts.Any(account => account.TemplateId == templateId))
        {
            throw new UserPersistenceException(
                UserPersistenceFailureReason.InvalidTemplateReference,
                "users.template_id references the Template; delete-with-members is required (fake).");
        }

        var index = FindIndex(templateId);
        EnsureVersion(_rows[index], expectedVersion);
        _rows.RemoveAt(index);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteWithMembersAsync(Guid templateId, int expectedVersion, CancellationToken cancellationToken)
    {
        BeforeDeleteWithMembers?.Invoke(templateId);

        var index = FindIndex(templateId);
        var row = _rows[index];

        // Version verified inside the destructive sequence (the fake mirrors the real
        // repository: stale → conflict, nothing changed).
        EnsureVersion(row, expectedVersion);

        // Atomic null-out of every USER reference, then the row delete (composition removed
        // with the row; USER rows stay untouched, active state preserved).
        NullUserReferences(templateId);

        _rows.RemoveAt(index);
        return Task.CompletedTask;
    }

    // ------------------------------------------------------ ITemplateModuleRepository

    /// <inheritdoc />
    public Task<IReadOnlyList<TemplateModule>> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken)
    {
        var row = _rows.FirstOrDefault(candidate => candidate.Template.TemplateId == templateId);
        return Task.FromResult<IReadOnlyList<TemplateModule>>(row?.Modules.ToArray() ?? []);
    }

    // ---------------------------------------------------------------------- internals

    private void ReplaceComposition(Guid templateId, IReadOnlyList<TemplateModule> modules)
    {
        var row = _rows.Single(candidate => candidate.Template.TemplateId == templateId);
        row.Modules.Clear();

        // Dense 1..n normalization, exactly like the real AddComposition: the persisted order
        // is the submitted order renumbered from 1.
        for (var index = 0; index < modules.Count; index++)
        {
            row.Modules.Add(new TemplateModule(
                templateId,
                modules[index].ModuleId,
                PresentationOrder: index + 1));
        }
    }

    private void NullUserReferences(Guid templateId) => _users.NullTemplateReferences(templateId);

    /// <summary>
    /// Test probe: appends one persisted Module entry (used to simulate persisted
    /// unavailable/invalid composition state that the service itself would never write).
    /// </summary>
    public void AppendToCompositionProbe(Guid templateId, string moduleId)
    {
        var index = FindIndex(templateId);
        var row = _rows[index];
        row.Modules.Add(new TemplateModule(templateId, moduleId, PresentationOrder: row.Modules.Count + 1));
    }

    private int FindIndex(Guid templateId)
    {
        var index = _rows.FindIndex(row => row.Template.TemplateId == templateId);
        if (index < 0)
        {
            throw new InvalidOperationException(
                $"Template '{templateId}' was not found (deleted or invalid identifier).");
        }

        return index;
    }

    private static void EnsureVersion(Row row, int expectedVersion)
    {
        if (row.Template.Version != expectedVersion)
        {
            throw new ConcurrencyConflictException(
                $"Template '{row.Template.TemplateId}' was modified concurrently " +
                $"(expected version {expectedVersion}, current version {row.Template.Version}); " +
                "reload and retry.");
        }
    }
}