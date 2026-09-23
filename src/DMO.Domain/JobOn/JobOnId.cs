namespace DMO.Domain.JobOn;

/// <summary>
/// Canonical identity of one Job On production occurrence.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §2.1, §2.2 and §6.1.
/// <para>
/// <c>jobon_id</c> identifies one concrete production occurrence. It is <b>not</b> a snapshot, a
/// revision, a lifecycle container or a "current production" pointer. There is no
/// <c>production_id</c> and no <c>job_on_revision_id</c> anywhere in this workstream.
/// </para>
/// <para>
/// The conversion from <see cref="Guid"/> is deliberately <b>not</b> implicit, and allocation
/// belongs to the backend application layer.
/// </para>
/// </remarks>
public readonly record struct JobOnId(Guid Value)
{
    /// <summary>Wraps an existing identifier value.</summary>
    public static JobOnId From(Guid value) => new(value);

    /// <summary>Allocates a new production-occurrence identity (backend-owned allocation).</summary>
    public static JobOnId New() => new(Guid.NewGuid());

    /// <summary>Whether the identity holds an allocatable value.</summary>
    public bool IsEmpty => Value == Guid.Empty;

    /// <summary>The canonical value.</summary>
    public Guid ToGuid() => Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
