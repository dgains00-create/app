namespace DMO.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence entity for the <c>boquilha_machines</c> table: one registered machine/line of the
/// aggregate's trace.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §6.2. One or more rows are required for every aggregate
/// (<c>MACHINES_REQUIRED</c>; "Máquina(s)/Linha(s) … at least one required", global §5).
/// Registered machine/line is search/filter context, <b>never identity</b>: there is no grouping
/// column, no <c>is_primary</c> and no cascade/reference from movements (movement <c>machine</c>
/// facts are independent frozen facts). The row carries no version — the aggregate's version
/// protects the set (parent-protects-child precedent).</remarks>
public sealed class BoquilhaMachineEntity
{
    /// <summary>Primary key (child-row identity).</summary>
    public Guid BoquilhaMachineId { get; set; }

    /// <summary>The owning aggregate (FK RESTRICT).</summary>
    public Guid BoquilhasId { get; set; }

    /// <summary>The settled machine code (<c>B1</c>..<c>C3</c>; CHECK-enforced, independent machines).</summary>
    public string Machine { get; set; } = string.Empty;
}