namespace DMO.Domain.Boquilhas;

/// <summary>
/// The closed movement-type set of the Boquilhas production movement register: exactly three
/// values.
/// </summary>
/// <remarks>
/// Authority: P2-T07 OWNER CLARIFICATION (the production movement register). The final operational
/// movement types are <c>saida | entrada | entrada_sem_reparacao</c> (Saída / Entrada / Entrada sem
/// reparação).
/// <para>
/// <b>Superseded (Owner clarification):</b> <c>inicio</c> (the register never manufactures a
/// quantity movement to establish existence) and <c>irreparavel</c> (Entrada sem reparação replaced
/// it: X boquilhas returned from the repairer but NOT repaired — a normal historical movement
/// record that returns quantity from repair with the distinct meaning; it never marks the Tool
/// irreparable, never creates a permanent Tool state, never separates/destroys the Tool identity
/// and never creates an irreparable bucket). <c>Editar</c> is an action, never a movement type.
/// </para>
/// <para>
/// ASCII domain tokens with canonical Portuguese labels for presentation. The mapping is explicit
/// and ordinal: no case folding and no fuzzy matching.
/// </para>
/// </remarks>
public enum MovementKind
{
    /// <summary>Saída — boquilhas dispatched to the external repairer (machine + repairer recorded).</summary>
    Saida,

    /// <summary>Entrada — repaired boquilhas returned (quantity leaves the outstanding repair).</summary>
    Entrada,

    /// <summary>Entrada sem reparação — boquilhas returned from the repairer but NOT repaired
    /// (quantity leaves the outstanding repair; the history records they are not chargeable).</summary>
    EntradaSemReparacao,
}

/// <summary>
/// The stored ASCII tokens of the closed <see cref="MovementKind"/> set.
/// </summary>
/// <remarks>
/// Authority: P2-T07 OWNER CLARIFICATION (exact tokens <c>saida|entrada|entrada_sem_reparacao</c>;
/// the <c>boquilha_movements_type_check</c> backstop). Only these three tokens are writable; any
/// other value is refused with <c>MOVEMENT_TYPE_INVALID</c>.
/// </remarks>
public static class MovementKindTokens
{
    /// <summary>The stored token of one movement kind.</summary>
    public static string ToToken(MovementKind kind) => kind switch
    {
        MovementKind.Saida => "saida",
        MovementKind.Entrada => "entrada",
        MovementKind.EntradaSemReparacao => "entrada_sem_reparacao",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown movement kind."),
    };

    /// <summary>Parses a stored movement token, or <c>null</c> when it is not in the closed set.</summary>
    public static MovementKind? Parse(string? token) => token switch
    {
        "saida" => MovementKind.Saida,
        "entrada" => MovementKind.Entrada,
        "entrada_sem_reparacao" => MovementKind.EntradaSemReparacao,
        _ => null,
    };

    /// <summary>The canonical Portuguese label of one movement kind (presentation only).</summary>
    public static string ToLabel(MovementKind kind) => kind switch
    {
        MovementKind.Saida => "Saída",
        MovementKind.Entrada => "Entrada",
        MovementKind.EntradaSemReparacao => "Entrada sem reparação",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown movement kind."),
    };
}