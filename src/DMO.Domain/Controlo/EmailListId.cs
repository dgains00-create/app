namespace DMO.Domain.Controlo;

/// <summary>
/// Canonical identity of one named email recipient list.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §13.2 and §3.3. <c>email_list_id</c> is a configuration identity
/// owned by Controlo_Create → Definições. It is never a document identity, a send identity or a
/// routing rule; the context → list mapping belongs to P2-T08's contract (Q-ROUTE).
/// </remarks>
public readonly record struct EmailListId(Guid Value)
{
    /// <summary>Wraps an existing identifier value.</summary>
    public static EmailListId From(Guid value) => new(value);

    /// <summary>Allocates a new list identity (backend-owned allocation).</summary>
    public static EmailListId New() => new(Guid.NewGuid());

    /// <summary>Whether the identity holds an allocatable value.</summary>
    public bool IsEmpty => Value == Guid.Empty;

    /// <summary>The canonical value.</summary>
    public Guid ToGuid() => Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}