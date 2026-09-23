namespace DMO.Domain.Controlo;

/// <summary>
/// Canonical identity of one external repairer register entry.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §10.1 and §3.3. <c>repairer_id</c> is owned by
/// Controlo_Create → Definições (the canonical repairer register). A repairer is identified by its
/// row, never by its name (two repairers may share a name — Q-REP). It is never a supplier code, a
/// person identity or a machine.
/// </remarks>
public readonly record struct RepairerId(Guid Value)
{
    /// <summary>Wraps an existing identifier value.</summary>
    public static RepairerId From(Guid value) => new(value);

    /// <summary>Allocates a new repairer identity (backend-owned allocation).</summary>
    public static RepairerId New() => new(Guid.NewGuid());

    /// <summary>Whether the identity holds an allocatable value.</summary>
    public bool IsEmpty => Value == Guid.Empty;

    /// <summary>The canonical value.</summary>
    public Guid ToGuid() => Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}