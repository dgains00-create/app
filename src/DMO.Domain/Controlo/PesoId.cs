namespace DMO.Domain.Controlo;

/// <summary>
/// Canonical identity of one Peso control/result fact.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §3.1 and AC-P3. <c>peso_id</c> is exactly one specific Peso/Controlo
/// record. It is <b>never</b> a production identity, a Tool identity, a Job On identity, a CM
/// identity, an approval copy or a revision. There is no <c>production_id</c> and no
/// <c>job_on_revision_id</c> anywhere in this workstream.
/// <para>
/// The conversion from <see cref="Guid"/> is deliberately <b>not</c> implicit, and allocation
/// belongs to the backend application layer: no client ever supplies or guesses a Peso identity
/// (PID4, AC-P4). The database default <c>gen_random_uuid()</c> exists only as the foundation
/// column convention and is never the source of a canonical identity used by the application.
/// </para>
/// </remarks>
public readonly record struct PesoId(Guid Value)
{
    /// <summary>Wraps an existing identifier value.</summary>
    public static PesoId From(Guid value) => new(value);

    /// <summary>Allocates a new Peso identity (backend-owned allocation, inside the create transaction).</summary>
    public static PesoId New() => new(Guid.NewGuid());

    /// <summary>Whether the identity holds an allocatable value.</summary>
    public bool IsEmpty => Value == Guid.Empty;

    /// <summary>The canonical value.</summary>
    public Guid ToGuid() => Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}