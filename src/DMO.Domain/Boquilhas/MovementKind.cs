namespace DMO.Domain.Boquilhas;

/// <summary>
/// The closed movement-type set of the Boquilhas ledger: exactly four values.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §3.1.
/// <para>
/// The four write movement types are <c>inicio | saida | entrada | irreparavel</c>
/// (Início/Saída/Entrada/Irreparável). <b><c>Editar</c> is an action on an existing movement, never
/// a movement type</b> — no token, enum value or CHECK value exists for it. No obsolete/legacy type
/// is carried forward (no <c>contagem</c>, no "Fabricar/Reparar" choice) and no fifth type exists.
/// </para>
/// <para>
/// ASCII domain tokens with canonical Portuguese labels for presentation (the accepted P2-T06 §3.2
/// convention). The mapping is explicit and ordinal: no case folding and no fuzzy matching.
/// </para>
/// </remarks>
public enum MovementKind
{
    /// <summary>Início — the opening quantity of the aggregate (created once, with the aggregate).</summary>
    Inicio,

    /// <summary>Saída — external repair dispatch (quantity ≤ Disponível; machine + repairer required).</summary>
    Saida,

    /// <summary>Entrada — repair return (recorded in full, never clamped/rejected for exceeding the expected amount).</summary>
    Entrada,

    /// <summary>Irreparável — declared irreparable (quantity ≤ Em reparação).</summary>
    Irreparavel,
}

/// <summary>
/// The stored ASCII tokens of the closed <see cref="MovementKind"/> set.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §3.1 (exact tokens <c>inicio|saida|entrada|irreparavel</c>) and §7.4
/// (<c>boquilha_movements_type_check</c>). Only these four tokens are writable; any other value is
/// refused with <c>MOVEMENT_TYPE_INVALID</c>.
/// </remarks>
public static class MovementKindTokens
{
    /// <summary>The stored token of one movement kind.</summary>
    public static string ToToken(MovementKind kind) => kind switch
    {
        MovementKind.Inicio => "inicio",
        MovementKind.Saida => "saida",
        MovementKind.Entrada => "entrada",
        MovementKind.Irreparavel => "irreparavel",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown movement kind."),
    };

    /// <summary>Parses a stored movement token, or <c>null</c> when it is not in the closed set.</summary>
    public static MovementKind? Parse(string? token) => token switch
    {
        "inicio" => MovementKind.Inicio,
        "saida" => MovementKind.Saida,
        "entrada" => MovementKind.Entrada,
        "irreparavel" => MovementKind.Irreparavel,
        _ => null,
    };

    /// <summary>The canonical Portuguese label of one movement kind (presentation only).</summary>
    public static string ToLabel(MovementKind kind) => kind switch
    {
        MovementKind.Inicio => "Início",
        MovementKind.Saida => "Saída",
        MovementKind.Entrada => "Entrada",
        MovementKind.Irreparavel => "Irreparável",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown movement kind."),
    };
}