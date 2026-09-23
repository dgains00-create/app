namespace DMO.Domain.Tools;

/// <summary>
/// One operational machine code: <c>B1</c>, <c>B2</c>, <c>B3</c>, <c>C1</c>, <c>C2</c> or <c>C3</c>.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §2.3 and <c>reports/CONTROL_SETTINGS_REPAIRERS_EMAIL_PDF_DELTA.md</c>
/// §4 (settled machine autonomy).
/// <para>
/// The six codes are <b>independent operational machines</b>. There is deliberately no line
/// grouping, no <c>Linha B</c>/<c>Linha C</c> concept, no inheritance, no cascade, no shared B/C
/// assignment, no parent/child relation, no <c>machine_id</c> scheme and no machine registry: the
/// machine is a consumed production-context code value, never a second machine authority.
/// </para>
/// </remarks>
public readonly record struct MachineCode(string Value)
{
    /// <summary>The settled machine codes, in the settled order.</summary>
    public static IReadOnlyList<MachineCode> All { get; } =
    [
        new MachineCode("B1"),
        new MachineCode("B2"),
        new MachineCode("B3"),
        new MachineCode("C1"),
        new MachineCode("C2"),
        new MachineCode("C3"),
    ];

    /// <summary>Wraps a machine code value; the value is not validated here.</summary>
    public static MachineCode From(string value) => new(value);

    /// <summary>Whether the supplied value is one of the six settled operational machines.</summary>
    public static bool IsKnown(string? value) =>
        value is not null && All.Any(machine => string.Equals(machine.Value, value, StringComparison.Ordinal));

    /// <summary>Parses a settled machine code, or <c>null</c> when the value is not settled.</summary>
    public static MachineCode? Parse(string? value) =>
        IsKnown(value) ? new MachineCode(value!) : null;

    /// <summary>The settled ordering position (0-based) of this code, or -1 when unsettled.</summary>
    public int Order
    {
        get
        {
            for (var index = 0; index < All.Count; index++)
            {
                if (string.Equals(All[index].Value, Value, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
