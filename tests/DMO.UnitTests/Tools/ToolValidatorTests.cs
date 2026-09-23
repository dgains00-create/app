using System.Reflection;
using DMO.Application.Repositories;
using DMO.Application.Tools;
using DMO.Domain.Tools;

namespace DMO.UnitTests.Tools;

/// <summary>
/// P2-T04 unit — the pure Tool validator: exact contracted code set, closed machine/type/processo
/// value sets, the bounded search criteria and the "validate before any write" rule.
/// <para>
/// Authority: <c>plans/contracts/P2-T04_DOMAIN_CORE_TOOL_JOBON_CONTRACT.md</c> §20.1 rows TOL2 and
/// TOL4, §12.3/§12.4 (closed code set) and §22 AC-5, AC-6, AC-42, AC-45, AC-65.
/// </para>
/// <para>
/// Preconditions: none; the validator is a pure static type and every test constructs its own input.
/// Required non-effects: an invalid command writes nothing and reads nothing — proved differentially
/// through a recording <see cref="IToolRepository"/>, so the assertion is not a tautology about a
/// method that simply returns a list.
/// </para>
/// </summary>
public sealed class ToolValidatorTests
{
    /// <summary>The closed validation-code set the contract allows (contract §12.3/§12.4).</summary>
    private static readonly string[] ClosedCodeSet =
    [
        ToolValidationErrors.ReferenceRequired,
        ToolValidationErrors.ToolTypeRequired,
        ToolValidationErrors.ToolTypeUnknown,
        ToolValidationErrors.ProcessoUnknown,
        ToolValidationErrors.QuantityNegative,
        ToolValidationErrors.MachineCompatibilityRequired,
        ToolValidationErrors.MachineUnknown,
        ToolValidationErrors.SearchCriteriaRequired,
        ToolValidationErrors.LimitOutOfRange,
    ];

    /// <summary>A fully valid Tool create command (all six contracted facts present).</summary>
    private static CreateToolCommand ValidCommand() =>
        new("CM", "5447T173", "12", "NNPB", 10, ["B1", "C2"]);

    /// <summary>
    /// TOL2 (AC-5, AC-6, AC-65) — the validator rejects a missing Tool type, missing reference/lot
    /// identity text, a missing machine-compatibility set, an unsettled machine, an unsettled
    /// <c>processo</c> and a negative quantity with exactly <c>TOOL_TYPE_REQUIRED</c>,
    /// <c>REFERENCE_REQUIRED</c>, <c>MACHINE_COMPATIBILITY_REQUIRED</c>, <c>MACHINE_UNKNOWN</c>,
    /// <c>PROCESSO_UNKNOWN</c> and <c>QUANTITY_NEGATIVE</c> — all of them before any write, because
    /// the create service refuses the command without touching the repository at all.
    /// </summary>
    [Fact]
    public async Task TOL2_ValidatorRejectsMissingFactsBeforeAnyWrite()
    {
        // The validator is a pure static type: no instance state and no persistence dependency.
        Assert.True(typeof(ToolValidator).IsAbstract && typeof(ToolValidator).IsSealed);
        Assert.Empty(typeof(ToolValidator).GetConstructors(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
        Assert.DoesNotContain(
            DeclaredMemberTypes(typeof(ToolValidator)),
            type => (type.Namespace ?? string.Empty).StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
                    (type.Namespace ?? string.Empty).StartsWith("DMO.Infrastructure", StringComparison.Ordinal) ||
                    type == typeof(IToolRepository));

        // Nothing supplied at all: the exact ordered code list, and nothing outside the closed set.
        var missingEverything = ToolValidator.Validate(new CreateToolCommand(null, null, null, null, null, null));

        Assert.Equal(
            new[]
            {
                ToolValidationErrors.ToolTypeRequired,
                ToolValidationErrors.ReferenceRequired,
                ToolValidationErrors.MachineCompatibilityRequired,
            },
            missingEverything);
        Assert.All(missingEverything, code => Assert.Contains(code, ClosedCodeSet));

        // Type: absent vs unsettled are different contracted codes.
        Assert.Equal(
            new[] { ToolValidationErrors.ToolTypeRequired },
            ToolValidator.Validate(new CreateToolCommand("   ", "5447T173", "12", null, null, ["B1"])));
        Assert.Equal(
            new[] { ToolValidationErrors.ToolTypeUnknown },
            ToolValidator.Validate(new CreateToolCommand("XX", "5447T173", "12", null, null, ["B1"])));

        // Reference and lot are both the required identity text: the lot has no separate token, so a
        // blank lot is the same contracted failure (contract §12.4: no code outside the closed set).
        Assert.Equal(
            new[] { ToolValidationErrors.ReferenceRequired },
            ToolValidator.Validate(new CreateToolCommand("CM", "  ", "12", null, null, ["B1"])));
        Assert.Equal(
            new[] { ToolValidationErrors.ReferenceRequired },
            ToolValidator.Validate(new CreateToolCommand("CM", "5447T173", null, null, null, ["B1"])));

        // Machine compatibility: at least one row is required, and every supplied code must be settled.
        Assert.Equal(
            new[] { ToolValidationErrors.MachineCompatibilityRequired },
            ToolValidator.Validate(new CreateToolCommand("CM", "5447T173", "12", null, null, null)));
        Assert.Equal(
            new[] { ToolValidationErrors.MachineCompatibilityRequired },
            ToolValidator.Validate(new CreateToolCommand("CM", "5447T173", "12", null, null, [])));
        Assert.Equal(
            new[] { ToolValidationErrors.MachineCompatibilityRequired },
            ToolValidator.Validate(new CreateToolCommand("CM", "5447T173", "12", null, null, [" ", ""])));
        Assert.Equal(
            new[] { ToolValidationErrors.MachineUnknown },
            ToolValidator.Validate(new CreateToolCommand("CM", "5447T173", "12", null, null, ["B4"])));

        // Processo and quantity.
        Assert.Equal(
            new[] { ToolValidationErrors.ProcessoUnknown },
            ToolValidator.Validate(new CreateToolCommand("CM", "5447T173", "12", "XX", null, ["B1"])));
        Assert.Equal(
            new[] { ToolValidationErrors.QuantityNegative },
            ToolValidator.Validate(new CreateToolCommand("CM", "5447T173", "12", null, -1, ["B1"])));

        // Every produced code, over every rejected shape above, stays inside the closed set.
        var allRejectedCodes = new[]
        {
            ToolValidator.Validate(new CreateToolCommand(null, null, null, null, null, null)),
            ToolValidator.Validate(new CreateToolCommand("XX", "5447T173", "12", "XX", -3, ["B4"])),
        }.SelectMany(codes => codes).ToList();

        Assert.NotEmpty(allRejectedCodes);
        Assert.All(allRejectedCodes, code => Assert.Contains(code, ClosedCodeSet));

        // A fully valid command produces no code at all.
        Assert.Empty(ToolValidator.Validate(ValidCommand()));

        // AC-65 — "before any write": the rejected command reaches no repository member, while the
        // same service call with a valid command does reach it. The refusal is therefore a real
        // pre-write gate, not a post-write rollback.
        var rejectingRepository = new RecordingToolRepository();
        var rejectingResult = await new ToolService(rejectingRepository)
            .CreateAsync(new CreateToolCommand(null, null, null, null, null, null), CancellationToken.None);

        var rejected = Assert.IsType<ToolResult.ValidationFailed>(rejectingResult);
        Assert.Equal(missingEverything, rejected.Errors);
        Assert.Equal(0, rejectingRepository.Calls);

        var writingRepository = new RecordingToolRepository();
        var acceptedResult = await new ToolService(writingRepository)
            .CreateAsync(ValidCommand(), CancellationToken.None);

        Assert.IsType<ToolResult.Created>(acceptedResult);
        Assert.True(writingRepository.Calls > 0);
    }

    /// <summary>
    /// TOL4 (AC-42, AC-45) — <c>ToolValidator.MaxLimit == 100</c>; <c>Limit</c> 0 and 101 fail with
    /// <c>LIMIT_OUT_OF_RANGE</c>; a search with no criterion fails with
    /// <c>SEARCH_CRITERIA_REQUIRED</c>; a whitespace-only reference with no other criterion counts as
    /// not supplied and therefore fails the same way, never as an empty result.
    /// </summary>
    [Fact]
    public void TOL4_MaxLimitIsHundredAndLimitAndCriteriaFailuresUseTheExactCodes()
    {
        Assert.Equal(100, ToolValidator.MaxLimit);
        Assert.Equal(1, ToolValidator.MinLimit);

        // The bound is inclusive and honoured on both edges.
        Assert.Empty(ToolValidator.Validate(new ToolSearchQuery("5447", null, null, null, null, ToolValidator.MaxLimit)));
        Assert.Empty(ToolValidator.Validate(new ToolSearchQuery("5447", null, null, null, null, ToolValidator.MinLimit)));
        Assert.Empty(ToolValidator.Validate(new ToolSearchQuery("5447", null, null, null, null, 50)));

        // 0, 101 and any other out-of-range value is the contracted limit failure.
        Assert.Equal(
            new[] { ToolValidationErrors.LimitOutOfRange },
            ToolValidator.Validate(new ToolSearchQuery("5447", null, null, null, null, 0)));
        Assert.Equal(
            new[] { ToolValidationErrors.LimitOutOfRange },
            ToolValidator.Validate(new ToolSearchQuery("5447", null, null, null, null, 101)));
        Assert.Equal(
            new[] { ToolValidationErrors.LimitOutOfRange },
            ToolValidator.Validate(new ToolSearchQuery("5447", null, null, null, null, -5)));

        // No criterion at all: never an unbounded registry dump and never an empty result.
        Assert.Equal(
            new[] { ToolValidationErrors.SearchCriteriaRequired },
            ToolValidator.Validate(new ToolSearchQuery(null, null, null, null, null, 50)));
        Assert.Equal(
            new[] { ToolValidationErrors.SearchCriteriaRequired },
            ToolValidator.Validate(new ToolSearchQuery("   ", null, "  ", "\t", null, 50)));

        // A blank-after-trim reference counts as not supplied: on its own it is a criteria failure,
        // and together with a real criterion it is simply ignored as a predicate.
        Assert.Equal(
            new[] { ToolValidationErrors.SearchCriteriaRequired },
            ToolValidator.Validate(new ToolSearchQuery(null, null, "   ", null, null, 50)));
        Assert.Empty(ToolValidator.Validate(new ToolSearchQuery(null, null, " ", null, MachineCode.Parse("B1"), 50)));

        // Both failures are reported together, limit first, and both stay inside the closed set.
        Assert.Equal(
            new[] { ToolValidationErrors.LimitOutOfRange, ToolValidationErrors.SearchCriteriaRequired },
            ToolValidator.Validate(new ToolSearchQuery("  ", null, null, null, null, 0)));

        // Each contracted predicate is a sufficient criterion on its own.
        Assert.Empty(ToolValidator.Validate(new ToolSearchQuery("5447", null, null, null, null, 10)));
        Assert.Empty(ToolValidator.Validate(new ToolSearchQuery(null, ToolType.Cm, null, null, null, 10)));
        Assert.Empty(ToolValidator.Validate(new ToolSearchQuery(null, null, "5447T173", null, null, 10)));
        Assert.Empty(ToolValidator.Validate(new ToolSearchQuery(null, null, null, "12", null, 10)));
        Assert.Empty(ToolValidator.Validate(new ToolSearchQuery(null, null, null, null, MachineCode.Parse("C3"), 10)));

        // The validator holds no state: the same query always yields the same code list.
        var first = ToolValidator.Validate(new ToolSearchQuery(null, null, null, null, null, 0));
        var second = ToolValidator.Validate(new ToolSearchQuery(null, null, null, null, null, 0));
        Assert.Equal(first, second);
    }

    /// <summary>Every declared member type of a type, including parameter and field types.</summary>
    private static IReadOnlyList<Type> DeclaredMemberTypes(Type type)
    {
        var types = new List<Type>();

        foreach (var member in type.GetMembers(
                     BindingFlags.Public | BindingFlags.NonPublic |
                     BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            switch (member)
            {
                case FieldInfo field:
                    types.Add(field.FieldType);
                    break;

                case PropertyInfo property:
                    types.Add(property.PropertyType);
                    break;

                case MethodBase method:
                    types.AddRange(method.GetParameters().Select(parameter => parameter.ParameterType));
                    if (method is MethodInfo info)
                    {
                        types.Add(info.ReturnType);
                    }

                    break;
            }
        }

        return types;
    }

    /// <summary>
    /// A repository that records every member invocation, so "no write happened" is observable.
    /// </summary>
    private sealed class RecordingToolRepository : IToolRepository
    {
        public int Calls { get; private set; }

        public Task<Tool?> GetByIdAsync(Guid toolId, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<Tool?>(null);
        }

        public Task<IReadOnlyList<Tool>> SearchAsync(
            ToolSearchCriteria criteria,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<Tool>>([]);
        }

        public Task<Tool?> FindByIdentityAsync(
            ToolType type,
            string reference,
            string lot,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<Tool?>(null);
        }

        public Task<Tool> CreatedAsync(
            Tool tool,
            IReadOnlyList<MachineCode> compatibleMachines,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(tool);
        }

        public Task<IReadOnlyList<ToolUsageOccurrence>> ListUsageOccurrencesAsync(
            Guid toolId,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<IReadOnlyList<ToolUsageOccurrence>>([]);
        }
    }
}
