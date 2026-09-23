namespace DMO.Domain.Tools;

/// <summary>
/// Canonical identity of one concrete operational Tool.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §2.1, §2.2 (identity semantics) and §5.1 (one canonical registry).
/// <para>
/// <c>tool_id</c> means exactly one concrete Tool: one type, one reference, one lot. It is
/// allocated by the backend application layer and is never supplied, guessed or minted by a
/// client, and never a lot layer, a per-piece unit, a display-key surrogate or a machine.
/// </para>
/// <para>
/// The conversion from <see cref="Guid"/> is deliberately <b>not</b> implicit: a bare Guid is
/// never silently promoted into a canonical Tool identity at a call site.
/// </para>
/// </remarks>
public readonly record struct ToolId(Guid Value)
{
    /// <summary>Wraps an existing identifier value.</summary>
    public static ToolId From(Guid value) => new(value);

    /// <summary>Allocates a new canonical Tool identity (backend-owned allocation).</summary>
    public static ToolId New() => new(Guid.NewGuid());

    /// <summary>Whether the identity holds an allocatable value.</summary>
    public bool IsEmpty => Value == Guid.Empty;

    /// <summary>The canonical value.</summary>
    public Guid ToGuid() => Value;

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
