using System.Reflection;
using DMO.Application.JobOn;
using DMO.Domain.Tools;

namespace DMO.UnitTests.JobOn;

/// <summary>
/// P2-T04 unit proofs of the pure Job On validator: rows JOB1, JOB2 and DUP1 of the test-to-acceptance
/// matrix (<c>plans/contracts/P2-T04_DOMAIN_CORE_TOOL_JOBON_CONTRACT.md</c> §20.2/§20.4).
/// </summary>
/// <remarks>
/// The validator runs before any write and returns the exact contracted codes of the closed set
/// (contract §12.3/§12.4). Every assertion here is about that meaning: which refusal is produced, and
/// that the refusal is the <b>only</b> one produced for the supplied input.
/// </remarks>
public sealed class JobOnValidatorTests
{
    /// <summary>The six settled operational machine codes, in the settled order (contract §2.3, AC-17).</summary>
    private static readonly string[] SettledMachineCodes = ["B1", "B2", "B3", "C1", "C2", "C3"];

    /// <summary>A create command that carries no Tool association at all.</summary>
    private static CreateJobOnCommand Create(string reference, string productionNumber, string machine) =>
        new(
            reference,
            productionNumber,
            machine,
            ProductionDate: null,
            CmToolId: null,
            MfToolId: null,
            BqToolId: null);

    /// <summary>
    /// JOB1 — <c>JobOnValidator</c> rejects a blank reference, a blank production number and a
    /// missing or unknown machine with exactly <c>REFERENCE_REQUIRED</c>,
    /// <c>PRODUCTION_NUMBER_REQUIRED</c>, <c>MACHINE_REQUIRED</c> and <c>MACHINE_UNKNOWN</c> before any
    /// write, accepts a valid command, and refuses a blank reference → productions query instead of
    /// answering it with an empty result. Proves AC-18, AC-45 and AC-65.
    /// </summary>
    [Fact]
    public void JOB1_RejectsBlankFactsAndMissingOrUnknownMachinesWithTheExactCodes()
    {
        // Each fact failure produces its own contracted code and nothing else.
        Assert.Equal(
            new[] { JobOnValidationErrors.ReferenceRequired },
            JobOnValidator.Validate(Create("   ", "100", "B1")));

        Assert.Equal(
            new[] { JobOnValidationErrors.ProductionNumberRequired },
            JobOnValidator.Validate(Create("REF-1", "\t", "B1")));

        Assert.Equal(
            new[] { JobOnValidationErrors.MachineRequired },
            JobOnValidator.Validate(Create("REF-1", "100", "  ")));

        Assert.Equal(
            new[] { JobOnValidationErrors.MachineUnknown },
            JobOnValidator.Validate(Create("REF-1", "100", "LINHA B")));

        // A wholly empty command reports exactly the three missing facts, in the contracted order,
        // and never a fourth code.
        Assert.Equal(
            new[]
            {
                JobOnValidationErrors.ReferenceRequired,
                JobOnValidationErrors.ProductionNumberRequired,
                JobOnValidationErrors.MachineRequired,
            },
            JobOnValidator.Validate(Create("", "", "")));

        // A valid simplified-Beta command is accepted with no error at all (AC-18).
        Assert.Empty(JobOnValidator.Validate(Create("REF-1", "100", "B1")));
        Assert.Empty(JobOnValidator.Validate(
            new CreateJobOnCommand("REF-1", "100", "C3", new DateOnly(2026, 4, 1), null, null, null)));

        // The reference → productions query requires its reference: a blank one is a validation
        // failure, never an empty result (AC-45).
        Assert.Equal(
            new[] { JobOnValidationErrors.ReferenceRequired },
            JobOnValidator.Validate(new FindProductionsQuery("   ")));
        Assert.Equal(
            new[] { JobOnValidationErrors.ReferenceRequired },
            JobOnValidator.Validate(new FindProductionsQuery(string.Empty)));
        Assert.Empty(JobOnValidator.Validate(new FindProductionsQuery("REF-1")));

        // "Before any write" is structural: the validator is a pure static type that holds no field at
        // all, so it owns no repository, no database context and no service to write through.
        Assert.True(typeof(JobOnValidator).IsAbstract && typeof(JobOnValidator).IsSealed);

        var declaredFields = typeof(JobOnValidator).GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
            BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.Empty(declaredFields);

        // Exactly one validation entry point per contracted carrier, each returning the closed error
        // set: no overload writes, resolves or persists anything.
        var validateOverloads = typeof(JobOnValidator)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name == nameof(JobOnValidator.Validate))
            .ToList();
        Assert.Equal(5, validateOverloads.Count);
        Assert.All(validateOverloads, method => Assert.Equal(typeof(IReadOnlyList<string>), method.ReturnType));

        // The blank-reference refusal is decided before the repository read, so the query can never be
        // answered with an empty list.
        var serviceSource = ReadSource("src/DMO.Application/JobOn/JobOnService.cs");
        var validationIndex = serviceSource.IndexOf("JobOnValidator.Validate(query)", StringComparison.Ordinal);
        var readIndex = serviceSource.IndexOf("_jobOns.ListByReferenceAsync", StringComparison.Ordinal);
        Assert.True(validationIndex >= 0, "The productions query must be validated by JobOnValidator.");
        Assert.True(
            readIndex > validationIndex,
            "The reference must be validated before the reference → productions read is executed.");
    }

    /// <summary>
    /// JOB2 — <c>"B4"</c>, <c>"LINHA B"</c>, <c>"Linha B"</c> and <c>"B"</c> are rejected as machines
    /// with <c>MACHINE_UNKNOWN</c>, while the six settled codes are the only accepted values.
    /// Proves AC-17.
    /// </summary>
    [Fact]
    public void JOB2_OnlyTheSixSettledMachineCodesAreAccepted()
    {
        // The settled code set itself, in the settled order: six independent machines, no line,
        // no group and no parent/child relation.
        Assert.Equal(SettledMachineCodes, MachineCode.All.Select(machine => machine.Value));

        foreach (var rejected in new[] { "B4", "LINHA B", "Linha B", "B" })
        {
            Assert.Equal(
                new[] { JobOnValidationErrors.MachineUnknown },
                JobOnValidator.Validate(Create("REF-1", "100", rejected)));

            // The same closed set governs the duplication command's machine fact.
            Assert.Equal(
                new[] { JobOnValidationErrors.MachineUnknown },
                JobOnValidator.Validate(new DuplicateJobOnCommand(Guid.NewGuid(), 1, "200", rejected, null)));
        }

        foreach (var accepted in SettledMachineCodes)
        {
            Assert.Empty(JobOnValidator.Validate(Create("REF-1", "100", accepted)));
            Assert.Empty(JobOnValidator.Validate(
                new DuplicateJobOnCommand(Guid.NewGuid(), 1, "200", accepted, null)));
        }

        Assert.Equal(6, MachineCode.All.Count);
        Assert.DoesNotContain("B4", SettledMachineCodes);
        Assert.DoesNotContain("B", SettledMachineCodes);
    }

    /// <summary>
    /// DUP1 — <c>DuplicateJobOnCommand</c> requires the source id, the production number and the
    /// machine, and the validator reports exactly <c>SOURCE_JOBON_NOT_FOUND</c>,
    /// <c>PRODUCTION_NUMBER_REQUIRED</c>, <c>MACHINE_REQUIRED</c> and <c>MACHINE_UNKNOWN</c>; a fully
    /// valid command is accepted. Proves AC-55 and AC-65.
    /// </summary>
    [Fact]
    public void DUP1_TheDuplicationCommandRequiresAnExplicitSourceAndItsOwnFacts()
    {
        Assert.Equal(
            new[] { JobOnValidationErrors.SourceJobOnNotFound },
            JobOnValidator.Validate(new DuplicateJobOnCommand(Guid.Empty, 1, "200", "B1", null)));

        Assert.Equal(
            new[] { JobOnValidationErrors.ProductionNumberRequired },
            JobOnValidator.Validate(new DuplicateJobOnCommand(Guid.NewGuid(), 1, "  ", "B1", null)));

        Assert.Equal(
            new[] { JobOnValidationErrors.MachineRequired },
            JobOnValidator.Validate(new DuplicateJobOnCommand(Guid.NewGuid(), 1, "200", string.Empty, null)));

        Assert.Equal(
            new[] { JobOnValidationErrors.MachineUnknown },
            JobOnValidator.Validate(new DuplicateJobOnCommand(Guid.NewGuid(), 1, "200", "Linha B", null)));

        Assert.Empty(JobOnValidator.Validate(
            new DuplicateJobOnCommand(Guid.NewGuid(), 3, "200", "C2", new DateOnly(2026, 5, 1))));

        // Nothing is optional and nothing defaults: a command that supplies no source reports the
        // source refusal together with its own missing facts, so no implicit source can ever be used.
        Assert.Equal(
            new[]
            {
                JobOnValidationErrors.SourceJobOnNotFound,
                JobOnValidationErrors.ProductionNumberRequired,
                JobOnValidationErrors.MachineRequired,
            },
            JobOnValidator.Validate(new DuplicateJobOnCommand(default, 0, string.Empty, string.Empty, null)));

        var commandProperties = typeof(DuplicateJobOnCommand)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .ToDictionary(property => property.Name, property => property.PropertyType, StringComparer.Ordinal);

        Assert.Equal(
            new[] { "ExpectedSourceVersion", "Machine", "ProductionDate", "ProductionNumber", "SourceJobOnId" },
            commandProperties.Keys.OrderBy(name => name, StringComparer.Ordinal));
        Assert.Equal(typeof(Guid), commandProperties["SourceJobOnId"]);
        Assert.Equal(typeof(int), commandProperties["ExpectedSourceVersion"]);
        Assert.Equal(typeof(string), commandProperties["ProductionNumber"]);
        Assert.Equal(typeof(string), commandProperties["Machine"]);

        var parameters = typeof(DuplicateJobOnCommand).GetConstructors().Single().GetParameters();
        Assert.All(parameters, parameter => Assert.False(parameter.HasDefaultValue));
    }

    /// <summary>The repository root, found by walking up to the directory holding <c>DMO.slnx</c>.</summary>
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DMO.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory!.FullName;
    }

    /// <summary>Reads one repository-relative source file.</summary>
    private static string ReadSource(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));
}
