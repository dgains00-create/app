using System.Reflection;
using DMO.Application.JobOn;
using DMO.Application.Repositories;
using DMO.Domain.JobOn;
using DMO.Domain.Tools;
using DomainJobOn = DMO.Domain.JobOn.JobOn;

namespace DMO.UnitTests.JobOn;

/// <summary>
/// P2-T04 unit proofs of the delete/dependency rule: rows DEP6 and DEP13 of the test-to-acceptance
/// matrix (<c>plans/contracts/P2-T04_DOMAIN_CORE_TOOL_JOBON_CONTRACT.md</c> §20.5).
/// </summary>
/// <remarks>
/// The delete service consults <b>every</b> registered <see cref="IJobOnDependencyProbe"/>; a
/// non-empty report refuses the delete with a typed, actionable result and deletes nothing
/// (contract §11.5, AC-77). The two test doubles and the two guarded repositories below are local to
/// this file: they are assertion source, never a second store and never a production change.
/// </remarks>
public sealed class JobOnDeletionDependencyTests
{
    private const int PersistedVersion = 3;

    /// <summary>
    /// DEP6 — the delete service inspects every registered probe: with two test doubles (one
    /// contributing probe, one silent probe, and then two contributing probes) in both registration
    /// orders, both probes record exactly one inspection, the result is
    /// <see cref="JobOnResult.Refused"/> with <see cref="JobOnRefusalReason.DependencyExists"/> naming
    /// every contributing dependency kind, and nothing was deleted. Proves AC-77.
    /// </summary>
    [Fact]
    public async Task DEP6_EveryRegisteredProbeIsInspectedRegardlessOfRegistrationOrder()
    {
        var jobOnId = Guid.NewGuid();
        var cmContextId = Guid.NewGuid();

        // One contributing probe and one silent probe, in both registration orders: the order of
        // registration never decides whether a probe is consulted. Every case uses fresh probes, so
        // each inspection count is evidence of that case alone.
        await AssertRefusalAsync(
            jobOnId,
            cmContextId,
            [Silent(), Lineage()],
            ["duplication-lineage"]);

        await AssertRefusalAsync(
            jobOnId,
            cmContextId,
            [Lineage(), Silent()],
            ["duplication-lineage"]);

        // Two contributing probes: both kinds are named, in registration order, and neither report is
        // discarded.
        await AssertRefusalAsync(
            jobOnId,
            cmContextId,
            [Lineage(), External()],
            ["duplication-lineage", "external-dependency"]);

        await AssertRefusalAsync(
            jobOnId,
            cmContextId,
            [External(), Lineage()],
            ["external-dependency", "duplication-lineage"]);
    }

    /// <summary>
    /// DEP13 — the refusal is a typed, actionable result and not a generic exception:
    /// <see cref="JobOnResult.Refused"/> carries the reason, a non-blank message and the contributing
    /// dependency kinds, and it is one member of the closed <see cref="JobOnResult"/> set (an abstract
    /// root whose nested records are the only results). Proves AC-77.
    /// </summary>
    [Fact]
    public void DEP13_TheRefusalIsATypedActionResultNamingEveryContributingDependencyKind()
    {
        var dependencies = new[]
        {
            new JobOnDependency("duplication-lineage", "Job On 'REF-1/200' was duplicated from this occurrence"),
            new JobOnDependency("external-dependency", "A dependent operational record references this occurrence"),
        };

        var refusal = new JobOnResult.Refused(
            JobOnRefusalReason.DependencyExists,
            "Dependent operational facts exist for this Job On, so it cannot be deleted: " +
            string.Join("; ", dependencies.Select(dependency => dependency.Description)),
            ExistingJobOnId: null,
            Dependencies: dependencies);

        // Typed and actionable: a reason the caller can switch on, a message an operator can read and
        // the exact contributing kinds — never a bare failure.
        Assert.True(Enum.IsDefined(refusal.Reason));
        Assert.Equal(JobOnRefusalReason.DependencyExists, refusal.Reason);
        Assert.False(string.IsNullOrWhiteSpace(refusal.Message));
        Assert.Null(refusal.ExistingJobOnId);
        Assert.NotNull(refusal.Dependencies);
        Assert.Equal(
            new[] { "duplication-lineage", "external-dependency" },
            refusal.Dependencies!.Select(dependency => dependency.Kind));
        Assert.All(refusal.Dependencies!, dependency => Assert.False(string.IsNullOrWhiteSpace(dependency.Description)));
        Assert.All(
            refusal.Dependencies!,
            dependency => Assert.Contains(dependency.Description, refusal.Message, StringComparison.Ordinal));

        // The refusal is a result of the closed set, never an exception and never a thrown failure.
        Assert.False(typeof(Exception).IsAssignableFrom(typeof(JobOnResult)));
        Assert.False(typeof(Exception).IsAssignableFrom(typeof(JobOnResult.Refused)));

        // The set is closed: an abstract record root whose constructors are all inaccessible outside
        // the set, with sealed nested records as its only members.
        Assert.True(typeof(JobOnResult).IsAbstract);
        Assert.Empty(typeof(JobOnResult).GetConstructors(BindingFlags.Public | BindingFlags.Instance));

        var rootConstructors = typeof(JobOnResult).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.All(rootConstructors, constructor => Assert.False(constructor.IsPublic));
        Assert.Contains(rootConstructors, constructor => constructor.IsPrivate && constructor.GetParameters().Length == 0);

        var results = typeof(JobOnResult).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
        // 11 since P2-T05 (disclosed): the accepted Q-CAND additive read adds exactly one result
        // case — JobOnResult.AssociationCandidates (P2-T05 contract §20.4.2); the closed-set
        // discipline asserted below is unchanged.
        Assert.Equal(11, results.Length);
        Assert.Contains(typeof(JobOnResult.Refused), results);
        Assert.All(
            results,
            result =>
            {
                Assert.True(result.IsSealed, $"{result.Name} must be a sealed result of the closed set.");
                Assert.True(typeof(JobOnResult).IsAssignableFrom(result), $"{result.Name} must be a JobOnResult.");
                Assert.NotNull(
                    result.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
            });

        // The refusal carries its four actionable members, so the reason, the message and the
        // dependency kinds are part of the result shape itself.
        var refusalProperties = typeof(JobOnResult.Refused)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .ToDictionary(property => property.Name, property => property.PropertyType, StringComparer.Ordinal);
        Assert.Equal(
            new[] { "Dependencies", "ExistingJobOnId", "Message", "Reason" },
            refusalProperties.Keys.OrderBy(name => name, StringComparer.Ordinal));
        Assert.Equal(typeof(JobOnRefusalReason), refusalProperties["Reason"]);
        Assert.Equal(typeof(string), refusalProperties["Message"]);
        Assert.Equal(typeof(IReadOnlyList<JobOnDependency>), refusalProperties["Dependencies"]);
    }

    /// <summary>A registered probe that reports nothing: it must still be consulted.</summary>
    private static Probe Silent() => new("controlo-module");

    /// <summary>A registered probe contributing the recorded duplication lineage (AC-78).</summary>
    private static Probe Lineage() => new(
        "jobon-lineage",
        "duplication-lineage",
        "Job On 'REF-1/200' was duplicated from this occurrence");

    /// <summary>A registered probe contributing a dependency owned by another module (AC-82).</summary>
    private static Probe External() => new(
        "boquilhas-module",
        "external-dependency",
        "A dependent operational record references this occurrence");

    /// <summary>
    /// Runs one delete over a fresh service and asserts the order-independent refusal contract of
    /// DEP6: both probes inspected, the refusal naming every contributing kind, nothing deleted.
    /// </summary>
    private static async Task AssertRefusalAsync(
        Guid jobOnId,
        Guid cmContextId,
        Probe[] probes,
        string[] expectedKinds)
    {
        var repository = new GuardedJobOnRepository(Persisted(jobOnId, cmContextId));
        var tools = new GuardedToolRepository();

        var service = new JobOnService(repository, tools, probes);

        var result = await service.DeleteAsync(
            new DeleteJobOnCommand(
                jobOnId,
                PersistedVersion,
                DeleteConfirmed: true,
                DateThresholdWarningAcknowledged: false),
            CancellationToken.None);

        var refused = Assert.IsType<JobOnResult.Refused>(result);
        Assert.Equal(JobOnRefusalReason.DependencyExists, refused.Reason);
        Assert.False(string.IsNullOrWhiteSpace(refused.Message));

        // EVERY registered probe was inspected exactly once, in both registration orders.
        Assert.All(probes, probe => Assert.Equal(1, probe.InspectionCount));

        Assert.NotNull(refused.Dependencies);
        Assert.Equal(expectedKinds, refused.Dependencies!.Select(dependency => dependency.Kind));

        // Each probe received the occurrence and its own context identities.
        Assert.All(
            probes,
            probe =>
            {
                Assert.NotNull(probe.LastTarget);
                Assert.Equal(jobOnId, probe.LastTarget!.JobOnId);
                Assert.Equal(cmContextId, probe.LastTarget!.CmContextId);
                Assert.Null(probe.LastTarget!.MfContextId);
                Assert.Null(probe.LastTarget!.BqContextId);
            });

        // Nothing was deleted, and the refusal never reached the delete primitive.
        Assert.Equal(0, repository.DeleteCallCount);
        Assert.Equal(PersistedVersion, repository.Stored!.Version);
    }

    /// <summary>The persisted occurrence the delete path reads: version 3 with one CM context.</summary>
    private static DomainJobOn Persisted(Guid jobOnId, Guid cmContextId)
    {
        var id = JobOnId.From(jobOnId);

        return new DomainJobOn(
            id,
            "REF-1",
            "100",
            MachineCode.From("B1"),
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7),
            CopiedFromJobOnId: null,
            Version: PersistedVersion,
            Contexts:
            [
                new ToolContext(
                    ToolContextType.Cm,
                    cmContextId,
                    id,
                    ToolId.From(Guid.NewGuid()),
                    new ToolContextSnapshot(ToolType.Cm, "5447T173", "12")),
            ]);
    }

    /// <summary>One recording dependency probe: it reports a fact and counts its own inspections.</summary>
    private sealed class Probe(string source, string? dependencyKind = null, string? dependencyDescription = null)
        : IJobOnDependencyProbe
    {
        /// <summary>How many times the delete service consulted this probe.</summary>
        public int InspectionCount { get; private set; }

        /// <summary>The last target the service offered.</summary>
        public JobOnDependencyTarget? LastTarget { get; private set; }

        /// <inheritdoc />
        public Task<JobOnDependencyReport> InspectAsync(
            JobOnDependencyTarget target,
            CancellationToken cancellationToken)
        {
            InspectionCount++;
            LastTarget = target;

            return Task.FromResult(dependencyKind is null || dependencyDescription is null
                ? JobOnDependencyReport.None(source)
                : new JobOnDependencyReport(source, [new JobOnDependency(dependencyKind, dependencyDescription)]));
        }
    }

    /// <summary>
    /// A Job On repository guarded against every operation the delete-with-dependency path must not
    /// perform: any unexpected call fails the test loudly instead of passing silently.
    /// </summary>
    private sealed class GuardedJobOnRepository(DomainJobOn stored) : IJobOnRepository
    {
        /// <summary>The occurrence the delete path reads.</summary>
        public DomainJobOn? Stored { get; } = stored;

        /// <summary>How many times the delete primitive was reached.</summary>
        public int DeleteCallCount { get; private set; }

        /// <inheritdoc />
        public Task<DomainJobOn?> GetByIdAsync(Guid jobOnId, CancellationToken cancellationToken) =>
            Task.FromResult(Stored is not null && Stored.JobOnId.Value == jobOnId ? Stored : null);

        /// <inheritdoc />
        public Task<DomainJobOn?> FindByProductionAsync(
            string reference,
            string productionNumber,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("A delete must not look up a production pair.");

        /// <inheritdoc />
        public Task<IReadOnlyList<JobOnProductionListItem>> ListByReferenceAsync(
            string reference,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("A delete must not list productions.");

        /// <inheritdoc />
        public Task<DomainJobOn> CreatedAsync(
            DomainJobOn jobOn,
            IReadOnlyList<ToolContext> contexts,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("A delete must not create an occurrence.");

        /// <inheritdoc />
        public Task<DomainJobOn> UpdatedAsync(
            DomainJobOn jobOn,
            IReadOnlyList<ToolContextChange> changes,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("A delete must not update an occurrence.");

        /// <inheritdoc />
        public Task<DomainJobOn> DuplicatedAsync(
            DomainJobOn duplicate,
            IReadOnlyList<ToolContext> duplicatedContexts,
            int expectedSourceVersion,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("A delete must not duplicate an occurrence.");

        /// <inheritdoc />
        public Task DeletedAsync(Guid jobOnId, int expectedVersion, CancellationToken cancellationToken)
        {
            DeleteCallCount++;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<JobOnDependency>> ListLineageDependentsAsync(
            Guid jobOnId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("The delete path consults probes, not a hardcoded lineage read.");
    }

    /// <summary>
    /// A Tool repository guarded against every operation the delete-with-dependency path must not
    /// perform: the Job On delete owns no Tool write.
    /// </summary>
    private sealed class GuardedToolRepository : IToolRepository
    {
        /// <inheritdoc />
        public Task<Tool?> GetByIdAsync(Guid toolId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("A delete must not read a canonical Tool.");

        /// <inheritdoc />
        public Task<IReadOnlyList<Tool>> SearchAsync(
            ToolSearchCriteria criteria,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("A delete must not search the Tool registry.");

        /// <inheritdoc />
        public Task<Tool?> FindByIdentityAsync(
            ToolType type,
            string reference,
            string lot,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("A delete must not resolve a Tool identity.");

        /// <inheritdoc />
        public Task<Tool> CreatedAsync(
            Tool tool,
            IReadOnlyList<MachineCode> compatibleMachines,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("A delete must never write the Tool registry.");

        /// <inheritdoc />
        public Task<IReadOnlyList<ToolUsageOccurrence>> ListUsageOccurrencesAsync(
            Guid toolId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("A delete must not read the reverse Tool usage.");
    }
}
