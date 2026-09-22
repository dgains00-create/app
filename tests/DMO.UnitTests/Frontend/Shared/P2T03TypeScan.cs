using System.Reflection;
using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.UnitTests.Frontend.Shared;

/// <summary>
/// P2-T03 shared reflection scan used by the architecture assertions of the four component suites.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §8.1 (rows TP16, TP17, TR9, MR13, MR16, DB8, DB11) and §2.3.
/// <para>
/// This helper deliberately composes the canonical-identity token table from parts, so the
/// assertion source itself never contains a canonical identity literal. The file-scan side of the
/// contract's architecture rows (ST3) covers the production sources, the four partials, the two
/// assets and the P2-T03 presentation fixture; this helper is assertion source, not a fixture.
/// </para>
/// </remarks>
internal static class P2T03TypeScan
{
    /// <summary>The single accepted namespace of every shared P2-T03 contract type.</summary>
    public const string ContractsNamespace = "DMO.Web.Frontend.Shared.Contracts";

    /// <summary>Identity prefixes composed into the canonical-identity token table.</summary>
    private static readonly string[] IdentityPrefixes =
    [
        "tool", "jobon", "job_on", "cm", "mf", "bq", "peso", "boquilhas",
        "movement", "repairer", "pegamentos", "controlo_sheet", "resumo", "production",
    ];

    /// <summary>The canonical identity token table, composed from parts (never a literal here).</summary>
    public static IReadOnlyList<string> CanonicalIdentityTokens { get; } =
        IdentityPrefixes.Select(prefix => string.Concat(prefix, "_", "id")).ToList();

    /// <summary>Every contract type introduced by P2-T03 (contract Appendix B.1).</summary>
    public static IReadOnlyList<Type> PickerTypes { get; } =
    [
        typeof(ToolPickerFactPresentation),
        typeof(ToolPickerCandidatePresentation),
        typeof(ToolPickerEventKind),
        typeof(ToolPickerEvent),
        typeof(ToolPickerFocusTarget),
        typeof(ToolPickerOutcome),
        typeof(ToolPickerInteraction),
        typeof(ToolPickerPresentation),
    ];

    /// <summary>The compact summary row contract types.</summary>
    public static IReadOnlyList<Type> SummaryTypes { get; } =
    [
        typeof(ToolSummaryFactPresentation),
        typeof(ToolSummaryRowPresentation),
        typeof(ToolSummaryRowEventKind),
        typeof(ToolSummaryRowEvent),
    ];

    /// <summary>The repeated-row contract types.</summary>
    public static IReadOnlyList<Type> RowsTypes { get; } =
    [
        typeof(MeasurementRowFieldKind),
        typeof(MeasurementRowFieldOptionPresentation),
        typeof(MeasurementRowFieldPresentation),
        typeof(MeasurementRowPresentation),
        typeof(MeasurementRowsPresentation),
        typeof(MeasurementRowsEventKind),
        typeof(MeasurementRowsEvent),
        typeof(MeasurementRowsFocusKind),
        typeof(MeasurementRowsFocusTarget),
        typeof(MeasurementRowsOutcome),
        typeof(MeasurementRowsInteraction),
    ];

    /// <summary>The action-region contract types.</summary>
    public static IReadOnlyList<Type> DecisionTypes { get; } =
    [
        typeof(DecisionBarActionGroup),
        typeof(DecisionBarActionPresentation),
        typeof(DecisionBarEventKind),
        typeof(DecisionBarEvent),
        typeof(DecisionBarOutcome),
        typeof(DecisionBarInteraction),
        typeof(DecisionBarPresentation),
    ];

    /// <summary>Every contract type introduced by P2-T03.</summary>
    public static IReadOnlyList<Type> AllTypes { get; } =
        PickerTypes.Concat(SummaryTypes).Concat(RowsTypes).Concat(DecisionTypes).ToList();

    /// <summary>Every declared member name of the supplied types, including enum members.</summary>
    public static IEnumerable<string> MemberNames(IEnumerable<Type> types) =>
        types.SelectMany(type => type
            .GetMembers(BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(member => member.Name));

    /// <summary>Every string value held by a declared string constant or static readonly field.</summary>
    public static IEnumerable<string> ConstantStrings(IEnumerable<Type> types) =>
        types.SelectMany(type => type
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                       BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field.IsLiteral || field.IsInitOnly)
            .Where(field => field.FieldType == typeof(string))
            .Select(field => field.GetRawConstantValue() as string ?? field.GetValue(null) as string)
            .Where(value => value is not null)
            .Select(value => value!));

    /// <summary>Every declared enum member name of the supplied types.</summary>
    public static IEnumerable<string> EnumMemberNames(IEnumerable<Type> types) =>
        types.Where(type => type.IsEnum).SelectMany(type => Enum.GetNames(type));

    /// <summary>Every declared member type of the supplied types, including nested generics.</summary>
    public static IEnumerable<Type> MemberTypes(IEnumerable<Type> types) =>
        types.SelectMany(type => type
            .GetMembers(BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .SelectMany(MemberType));

    private static IEnumerable<Type> MemberType(MemberInfo member) => member switch
    {
        PropertyInfo property => Expand(property.PropertyType),
        FieldInfo field => Expand(field.FieldType),
        MethodInfo method => Expand(method.ReturnType)
            .Concat(method.GetParameters().SelectMany(parameter => Expand(parameter.ParameterType))),
        _ => [],
    };

    private static IEnumerable<Type> Expand(Type type)
    {
        yield return type;

        if (!type.IsGenericType)
        {
            yield break;
        }

        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var nested in Expand(argument))
            {
                yield return nested;
            }
        }
    }
}
