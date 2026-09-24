namespace DMO.Domain.Boquilhas;

/// <summary>
/// The derived balance projection of one Boquilhas aggregate: the four buckets.
/// </summary>
/// <remarks>
/// Authority: P2-T07 contract §3.3 and §18.
/// <para>
/// Buckets are <b>derived at read time from movement facts only</b> — never hand-entered and never
/// stored as a mutable balance. <see cref="Replay"/> is the exact §18.2 determinisitc replay over
/// the ledger ordered by <c>recorded_at ASC, movement_id ASC</c> (physical receipt order); editing
/// <c>business_date</c> never reorders the ledger and never changes the balance.
/// </para>
/// <para>
/// Invariant: <c>Disponivel + EmReparacao + Irreparavel = Σ(início)</c>. Negative <c>EmReparacao</c>
/// (and any negative saldo projection) is a <b>valid visible projection</b>, never an automatic
/// block: no validation, no CHECK and no presentation blocks on it.
/// </para>
/// </remarks>
public sealed record BalanceProjection(int Disponivel, int EmReparacao, int Irreparavel, int EntradaExcecional)
{
    /// <summary>
    /// The exact §18.2 replay: every movement applied once, in physical receipt order
    /// (<c>recorded_at ASC</c>, tie-break <c>movement_id ASC</c>).
    /// </summary>
    public static BalanceProjection Replay(IReadOnlyList<BoquilhaMovement> movements)
    {
        ArgumentNullException.ThrowIfNull(movements);

        var disponivel = 0;
        var emReparacao = 0;
        var irreparavel = 0;
        var entradaExcecional = 0;

        foreach (var movement in movements.OrderBy(movement => movement.RecordedAt)
                     .ThenBy(movement => movement.MovementId.Value))
        {
            switch (movement.Kind)
            {
                case MovementKind.Inicio:
                    disponivel += movement.Quantity;
                    break;

                case MovementKind.Saida:
                    disponivel -= movement.Quantity;
                    emReparacao += movement.Quantity;
                    break;

                case MovementKind.Entrada:
                    emReparacao -= movement.Quantity;
                    disponivel += movement.Quantity;
                    entradaExcecional += movement.ExcessReceivedQuantity ?? 0;
                    break;

                case MovementKind.Irreparavel:
                    emReparacao -= movement.Quantity;
                    irreparavel += movement.Quantity;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(movements), movement.Kind, "Unknown movement kind.");
            }
        }

        return new BalanceProjection(disponivel, emReparacao, irreparavel, entradaExcecional);
    }

    /// <summary>
    /// The expected return of an Entrada before its own effect: <c>GREATEST(0, Em reparação)</c> of
    /// a replay over the supplied before-state ledger (contract §17.2 step 4, Q-EXCESS).
    /// </summary>
    public static int ExpectedReturn(IReadOnlyList<BoquilhaMovement> beforeState) =>
        Math.Max(0, Replay(beforeState).EmReparacao);
}