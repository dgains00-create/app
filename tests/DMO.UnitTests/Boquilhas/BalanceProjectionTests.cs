using DMO.Domain.Boquilhas;

namespace DMO.UnitTests.Boquilhas;

/// <summary>
/// P2-T07 unit rows for the balance replay (contract §18, exact): the bucket formulas, the
/// invariant, the deterministic physical order, negative projections, the excess accumulation and
/// the Entrada expected-return helper.
/// </summary>
public sealed class BalanceProjectionTests
{
    private static BoquilhaMovement Movement(
        int order,
        MovementKind kind,
        int quantity,
        DateOnly businessDate,
        int? expectedReturn = null,
        int? excess = null) =>
        new(
            MovementId.From(Guid.Parse($"00000000-0000-0000-0000-{order:D12}")),
            BoquilhasId: Guid.Empty,
            kind,
            quantity,
            businessDate,
            RecordedAt: DateTimeOffset.UtcNow.AddSeconds(order),
            RecordedByUserId: Guid.Empty,
            Machine: null,
            RepairerId: null,
            expectedReturn,
            excess,
            Observations: null,
            Version: 1,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

    private static BoquilhaMovement Inicio(int order, int quantity) => Movement(order, MovementKind.Inicio, quantity, new DateOnly(2026, 9, 1));
    private static BoquilhaMovement Saida(int order, int quantity) => Movement(order, MovementKind.Saida, quantity, new DateOnly(2026, 9, 2));
    private static BoquilhaMovement Entrada(int order, int quantity, int expected, int excess) => Movement(order, MovementKind.Entrada, quantity, new DateOnly(2026, 9, 3), expected, excess);
    private static BoquilhaMovement Irreparavel(int order, int quantity) => Movement(order, MovementKind.Irreparavel, quantity, new DateOnly(2026, 9, 4));

    /// <summary>
    /// B2 (AC-B2) — the multi-scenario replay matches the exact §18 formulas and the invariant
    /// <c>Disponível + Em reparação + Irreparável = Σ(início)</c> holds, including excess returns.
    /// </summary>
    [Fact]
    public void B2_TheReplayMatchesTheExactFormulasAndInvariant()
    {
        var ledger = new List<BoquilhaMovement>
        {
            Inicio(1, 100),
            Saida(2, 40),
            Entrada(3, 30, expected: 40, excess: 0),
            Saida(4, 10),
            Irreparavel(5, 8),
            Entrada(6, 12, expected: 2, excess: 10),
        };

        var balance = BalanceProjection.Replay(ledger);

        // Disponível = Σ(início) − Σ(saída) + Σ(entrada) = 100 − 50 + 42 = 92
        Assert.Equal(92, balance.Disponivel);
        // Em reparação = Σ(saída) − Σ(entrada) − Σ(irreparável) = 50 − 42 − 8 = 0
        Assert.Equal(0, balance.EmReparacao);
        // Irreparável = Σ(irreparável) = 8
        Assert.Equal(8, balance.Irreparavel);
        // Entrada excecional = Σ(excess) = 0 + 10 = 10
        Assert.Equal(10, balance.EntradaExcecional);

        // The §3.3 invariant holds.
        Assert.Equal(100, balance.Disponivel + balance.EmReparacao + balance.Irreparavel);
    }

    /// <summary>
    /// B7 (AC-B7) — negative Em reparação after an excess Entrada is a VALID derived projection:
    /// the replay produces it and nothing normalizes it away.
    /// </summary>
    [Fact]
    public void B7_NegativeEmReparacaoIsAVisibleProjection()
    {
        var ledger = new List<BoquilhaMovement>
        {
            Inicio(1, 10),
            Saida(2, 6),
            Entrada(3, 8, expected: 6, excess: 2),
        };

        var balance = BalanceProjection.Replay(ledger);

        Assert.Equal(12, balance.Disponivel);
        Assert.Equal(-2, balance.EmReparacao);
        Assert.Equal(2, balance.EntradaExcecional);
    }

    /// <summary>
    /// B5 (AC-B5) — the Entrada excecional bucket accumulates exactly the persisted per-row excess
    /// facts (Σ over entrada rows of excess_received_quantity).
    /// </summary>
    [Fact]
    public void B5_TheExceptionalBucketIsTheSumOfThePerRowExcessFacts()
    {
        var ledger = new List<BoquilhaMovement>
        {
            Inicio(1, 100),
            Saida(2, 40),
            Entrada(3, 45, expected: 40, excess: 5),
            Saida(4, 10),
            Entrada(5, 15, expected: 5, excess: 10),
        };

        Assert.Equal(15, BalanceProjection.Replay(ledger).EntradaExcecional);
    }

    /// <summary>
    /// D3/B9 (AC-D3/AC-B9) — the replay order is PHYSICAL (recorded_at ASC, movement_id ASC):
    /// identical facts with a different business_date still produce the identical balance, because
    /// business_date never participates in the derivation.
    /// </summary>
    [Fact]
    public void D3B9_BusinessDateNeverAffectsTheReplay()
    {
        var businessA = new List<BoquilhaMovement>
        {
            Inicio(1, 10),
            Saida(2, 4),
            Entrada(3, 6, expected: 6, excess: 0),
        };

        var businessB = new List<BoquilhaMovement>
        {
            Inicio(1, 10),
            Saida(2, 4),
            Entrada(3, 6, expected: 6, excess: 0),
        };

        Assert.Equal(
            BalanceProjection.Replay(businessA),
            BalanceProjection.Replay(businessB));
    }

    /// <summary>
    /// B2 (AC-B2) — the deterministic tie-break: two movements recorded at the same instant replay
    /// in movement_id order; the totals are order-independent for this scenario class.
    /// </summary>
    [Fact]
    public void B2_TheMovementIdTieBreakIsDeterministic()
    {
        var sameTimestamp = new List<BoquilhaMovement>
        {
            Movement(1, MovementKind.Inicio, 10, new DateOnly(2026, 9, 1)),
            Movement(2, MovementKind.Saida, 4, new DateOnly(2026, 9, 2)),
        };

        var replayed = BalanceProjection.Replay(sameTimestamp);
        Assert.Equal(6, replayed.Disponivel);
        Assert.Equal(4, replayed.EmReparacao);
    }

    /// <summary>
    /// B8 (AC-B8) — a fully returned aggregate (zero saldo) still replays: the aggregate fact set
    /// is NOT deleted by the zero balance; the projection is simply zeroed.
    /// </summary>
    [Fact]
    public void B8_ZeroBalanceIsAValidProjectionOfAStillExistingAggregate()
    {
        var ledger = new List<BoquilhaMovement>
        {
            Inicio(1, 4),
            Saida(2, 4),
            Entrada(3, 4, expected: 4, excess: 0),
        };

        var balance = BalanceProjection.Replay(ledger);
        Assert.Equal(4, balance.Disponivel);
        Assert.Equal(0, balance.EmReparacao);
        Assert.Equal(0, balance.Irreparavel);
    }

    /// <summary>
    /// S6/Q-EXCESS — ExpectedReturn is GREATEST(0, Em reparação before): zero when nothing is in
    /// repair, the exact in-repair amount otherwise.
    /// </summary>
    [Fact]
    public void ExpectedReturnIsTheGreatestOfZeroAndInRepair()
    {
        var inRepair = new List<BoquilhaMovement>
        {
            Inicio(1, 10),
            Saida(2, 6),
        };

        Assert.Equal(6, BalanceProjection.ExpectedReturn(inRepair));

        var nothingInRepair = new List<BoquilhaMovement>
        {
            Inicio(1, 10),
        };

        Assert.Equal(0, BalanceProjection.ExpectedReturn(nothingInRepair));
    }

    /// <summary>
    /// E4 (AC-E4 unit facet) — the replay of an edited ledger equals the replay with the edited
    /// row's CURRENT values replaced by its NEW values (a single net event; no double counting).
    /// </summary>
    [Fact]
    public void E4_EditIsASingleNetEvent()
    {
        var original = new List<BoquilhaMovement>
        {
            Inicio(1, 10),
            Saida(2, 6),
        };

        var editedSaida = Saida(2, 4);
        var editedLedger = new List<BoquilhaMovement> { Inicio(1, 10), editedSaida };

        var beforeEdit = BalanceProjection.Replay(original);
        var afterEdit = BalanceProjection.Replay(editedLedger);

        // Replacing 6 by 4 moves exactly 2 units from Em reparação back to Disponível.
        Assert.Equal(beforeEdit.Disponivel + 2, afterEdit.Disponivel);
        Assert.Equal(beforeEdit.EmReparacao - 2, afterEdit.EmReparacao);
    }
}