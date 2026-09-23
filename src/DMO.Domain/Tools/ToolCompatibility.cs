namespace DMO.Domain.Tools;

/// <summary>
/// The create-time machine-compatibility set of a Tool: one or more settled machine codes.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §2.4, §3.2 (<c>tool_machines</c>: one row per compatible machine)
/// and §5.2 ("machine compatibility: one or more of B1 B2 B3 C1 C2 C3, required, &gt;= 1").
/// <para>
/// The set is normalised into the settled machine order <c>B1,B2,B3,C1,C2,C3</c> and cannot hold a
/// duplicate code. An empty set is not a Tool compatibility and is rejected on construction, so
/// the "one or more machines" rule has no in-memory escape hatch even though the database has no
/// trigger for it (contract §3.2).
/// </para>
/// </remarks>
public readonly record struct ToolCompatibility
{
    /// <summary>Creates a compatibility set, failing closed on an empty or unsettled input.</summary>
    /// <exception cref="ArgumentException">
    /// The set is empty or contains an unsettled machine code.
    /// </exception>
    public ToolCompatibility(IReadOnlyList<MachineCode> machines)
    {
        ArgumentNullException.ThrowIfNull(machines);

        if (machines.Count == 0)
        {
            throw new ArgumentException(
                "A Tool is compatible with one or more machines; an empty compatibility set is not a Tool fact.",
                nameof(machines));
        }

        foreach (var machine in machines)
        {
            if (!MachineCode.IsKnown(machine.Value))
            {
                throw new ArgumentException(
                    $"'{machine.Value}' is not one of the six settled operational machines.",
                    nameof(machines));
            }
        }

        Machines = machines
            .Distinct()
            .OrderBy(machine => machine.Order)
            .ToList();
    }

    /// <summary>The compatible machines, in the settled machine order.</summary>
    public IReadOnlyList<MachineCode> Machines { get; }

    /// <summary>Whether the Tool is registered as compatible with the supplied machine.</summary>
    public bool Includes(MachineCode machine) =>
        Machines.Any(candidate => string.Equals(candidate.Value, machine.Value, StringComparison.Ordinal));

    /// <summary>The comma-separated display form of the compatibility set.</summary>
    public override string ToString() => string.Join(", ", Machines.Select(machine => machine.Value));
}
