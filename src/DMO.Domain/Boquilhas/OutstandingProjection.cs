using DMO.Domain.Tools;

namespace DMO.Domain.Boquilhas;

/// <summary>
/// The derived outstanding-repair projection of one Boquilhas register.
/// </summary>
/// <remarks>
/// Authority: P2-T07 OWNER CLARIFICATION (quantity effect).
/// <para>
/// The outstanding repair quantity is <b>derived at read time from movement facts only</b> — never
/// hand-entered and never stored as a mutable balance:
/// </para>
/// <para>
/// <c>outstanding = Σ(Saída) − Σ(Entrada) − Σ(Entrada sem reparação)</c>
/// </para>
/// <para>
/// <b>Superseded (Owner clarification):</b> the four-bucket projection (Disponível / Em reparação /
/// Irreparável / Entrada excecional) and its Início-anchored invariant are removed. There is no
/// permanent Irreparável bucket; Entrada sem reparação RETURNS quantity from repair like an
/// Entrada, with the distinct historical meaning. A negative outstanding is a valid derived
/// projection, never an automatic block: no validation, no CHECK and no presentation blocks on it
/// (there is no stock semantics to violate). Movement facts remain the sole authority — no second
/// mutable balance source exists.
/// </para>
/// </remarks>
public static class OutstandingProjection
{
    /// <summary>
    /// The deterministic replay: every movement applied exactly once, in physical receipt order
    /// (<c>recorded_at ASC</c>, tie-break <c>movement_id ASC</c>).
    /// </summary>
    public static int Replay(IReadOnlyList<BoquilhaMovement> movements)
    {
        ArgumentNullException.ThrowIfNull(movements);

        var outstanding = 0;

        foreach (var movement in movements.OrderBy(movement => movement.RecordedAt)
                     .ThenBy(movement => movement.MovementId.Value))
        {
            switch (movement.Kind)
            {
                case MovementKind.Saida:
                    outstanding += movement.Quantity;
                    break;

                case MovementKind.Entrada:
                case MovementKind.EntradaSemReparacao:
                    outstanding -= movement.Quantity;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(movements), movement.Kind, "Unknown movement kind.");
            }
        }

        return outstanding;
    }
}