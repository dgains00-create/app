namespace DMO.IntegrationTests.Host;

/// <summary>
/// xUnit collection that serializes the test classes which mutate process-scoped test
/// configuration.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DmoWebApplicationFactory"/> makes its placeholder connection string visible to
/// the real application entry point by setting the process-scoped environment variable
/// <c>Database__ConnectionString</c> for the lifetime of the factory. That variable is
/// <b>process-global</b>, so two factories alive at the same time would interleave their
/// capture/restore sequences and could leave the process in the wrong state:
/// </para>
/// <code>
/// Factory A captures original value
/// -> A writes placeholder
/// Factory B captures A's placeholder
/// -> B writes placeholder
/// A disposes
/// -> A restores original
/// B is still running
/// -> process configuration unexpectedly changes underneath B
/// B disposes
/// -> B restores placeholder instead of original
/// </code>
/// <para>
/// xUnit may run different test classes concurrently unless collection parallelization is
/// constrained, so serialization is required rather than assumed away. Every test class that
/// constructs or uses a <see cref="DmoWebApplicationFactory"/> is placed in this collection.
/// </para>
/// <para>
/// <b>Why not disable parallelization assembly-wide.</b> That would also serialize the test
/// classes that never touch process state (<c>StartupCommandsTests</c>,
/// <c>MigrationRunnerTests</c>, <c>DatabaseConnectivityTests</c>), suppressing parallelism
/// beyond the actual hazard. This collection is the narrow form: it covers exactly the classes
/// that create or use the factory, and it runs alone with respect to the rest of the assembly.
/// </para>
/// <para>
/// <b>Why not a lock.</b> A lock would have to be held across a fixture's entire lifetime,
/// which is a hand-rolled equivalent of this declaration and is easier to get wrong. The
/// collection is the supported xUnit mechanism and states the intent in one place.
/// </para>
/// <para>
/// <b>Who is deliberately not in this collection.</b> <c>MigrationRunnerTests</c> references
/// only the compile-time constant <c>DmoWebApplicationFactory.PlaceholderConnectionString</c>.
/// It does not construct the factory, does not mutate <c>Database__ConnectionString</c>, does
/// not depend on the factory lifetime and does not observe process environment state, so
/// serializing it would add no safety. This matches the Architect decision recorded in
/// <c>dmo-work dev/reviews/P1-T01_TEST_INFRASTRUCTURE_CORRECTION_PLAN_V2_REVIEW.md</c> §1.
/// </para>
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ProcessEnvironmentCollection
{
    /// <summary>
    /// Name of the serialized collection, applied to participating classes via
    /// <c>[Collection(ProcessEnvironmentCollection.Name)]</c>.
    /// </summary>
    public const string Name = "ProcessEnvironment";
}
