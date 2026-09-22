namespace DMO.IntegrationTests.Persistence;

/// <summary>
/// xUnit collection that serializes the tests which mutate the shared disposable PostgreSQL
/// database.
/// </summary>
/// <remarks>
/// The env-gated persistence tests (migrations, constraints, repositories, single-ADMIN
/// invariant, bootstrap) run against the same disposable database supplied through
/// <c>DMO_TEST_POSTGRES_CONNECTION</c>. They are serialized so the singleton/bootstrapping
/// tests never interleave with other row-mutating tests.
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PersistenceDatabaseCollection
{
    /// <summary>Name of the serialized collection.</summary>
    public const string Name = "PersistenceDatabase";
}