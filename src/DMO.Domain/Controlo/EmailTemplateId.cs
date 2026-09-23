namespace DMO.Domain.Controlo;

/// <summary>
/// Canonical identity of one email template.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §14.2 and §3.3. <c>email_template_id</c> is a configuration identity
/// owned by Controlo_Create → Definições. It is never a document identity or a send identity, and
/// templates and lists are independent configuration entities (§15).
/// </remarks>
public readonly record struct EmailTemplateId(Guid Value)
{
    /// <summary>Wraps an existing identifier value.</summary>
    public static EmailTemplateId From(Guid value) => new(value);

    /// <summary>Allocates a new template identity (backend-owned allocation).</summary>
    public static EmailTemplateId New() => new(Guid.NewGuid());

    /// <summary>Whether the identity holds an allocatable value.</summary>
    public bool IsEmpty => Value == Guid.Empty;

    /// <summary>The canonical value.</summary>
    public Guid ToGuid() => Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}