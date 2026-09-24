using DMO.Domain.Boquilhas;

namespace DMO.UnitTests.Boquilhas;

/// <summary>
/// The derived outstanding-repair projection (OWNER CLARIFICATION): replay of the movement facts
/// in physical receipt order with <c>outstanding = Σ Saída − Σ Entrada − Σ Entrada sem reparação</c>.
/// </summary>
public sealed class OutstandingProjectionTests
{
    private static BoquilhaMovement Movement(
        MovementKind kind,
        int quantity,
        DateTimeOffset recordedAt,
        int movementId = 1) => new(
        MovementId.From(Guid.Parse($"00000000-0000-0000-0000-{movementId:D12}")),
        BoquilhasId: Guid.NewGuid(),
        kind,
        quantity,
        BusinessDate: DateOnly.FromDateTime(recordedAt.ToUniversalTime().Date),
        recordedAt,
        RecordedByUserId: Guid.NewGuid(),
        Machine: null,
        RepairerId: null,
        Observations: null,
        Version: 1,
        CreatedAt: recordedAt,
        UpdatedAt: recordedAt);

    /// <summary>The Owner example: Saída 10, Entrada 4, Entrada sem reparação 6 → outstanding 0.</summary>
    [Fact]
    public void OwnerExample_ReplaysToZero()
    {
        var ledger = new List<BoquilhaMovement>
        {
            Movement(MovementKind.Saida, 10, DateTimeOffset.Parse("2026-09-25T10:00:00Z"), 1),
            Movement(MovementKind.Entrada, 4, DateTimeOffset.Parse("2026-09-27T10:00:00Z"), 2),
            Movement(MovementKind.EntradaSemReparacao, 6, DateTimeOffset.Parse("2026-09-29T10:00:00Z"), 3),
        };

        Assert.Equal(0, OutstandingProjection.Replay(ledger));
    }

    [Fact]
    public void SaidaOnly_IsOutstanding()
    {
        var ledger = new List<BoquilhaMovement>
        {
            Movement(MovementKind.Saida, 10, DateTimeOffset.Parse("2026-09-25T10:00:00Z"), 1),
        };

        Assert.Equal(10, OutstandingProjection.Replay(ledger));
    }

    /// <summary>Entrada sem reparação RETURNS quantity from repair exactly like an Entrada.</summary>
    [Fact]
    public void EntradaSemReparacao_ReturnsQuantity()
    {
        var ledger = new List<BoquilhaMovement>
        {
            Movement(MovementKind.Saida, 10, DateTimeOffset.Parse("2026-09-25T10:00:00Z"), 1),
            Movement(MovementKind.EntradaSemReparacao, 10, DateTimeOffset.Parse("2026-09-29T10:00:00Z"), 2),
        };

        Assert.Equal(0, OutstandingProjection.Replay(ledger));
    }

    [Fact]
    public void AllTypes_MoveInTheSignedDirection()
    {
        var ledger = new List<BoquilhaMovement>
        {
            Movement(MovementKind.Saida, 8, DateTimeOffset.Parse("2026-09-25T10:00:00Z"), 1),
            Movement(MovementKind.Entrada, 3, DateTimeOffset.Parse("2026-09-27T10:00:00Z"), 2),
            Movement(MovementKind.EntradaSemReparacao, 2, DateTimeOffset.Parse("2026-09-29T10:00:00Z"), 3),
            Movement(MovementKind.Saida, 5, DateTimeOffset.Parse("2026-10-01T10:00:00Z"), 4),
        };

        Assert.Equal(8, OutstandingProjection.Replay(ledger));
    }

    /// <summary>A negative outstanding is a valid DERIVED projection, never an automatic block.</summary>
    [Fact]
    public void NegativeOutstanding_IsAValidProjection()
    {
        var ledger = new List<BoquilhaMovement>
        {
            Movement(MovementKind.Saida, 2, DateTimeOffset.Parse("2026-09-25T10:00:00Z"), 1),
            Movement(MovementKind.Entrada, 5, DateTimeOffset.Parse("2026-09-27T10:00:00Z"), 2),
        };

        Assert.Equal(-3, OutstandingProjection.Replay(ledger));
    }

    [Fact]
    public void EmptyLedger_IsZero()
    {
        Assert.Equal(0, OutstandingProjection.Replay([]));
    }

    /// <summary>The replay order is physical receipt (recorded_at ASC, movement_id ASC): the sums
    /// commute, but the deterministic order is the contract of the per-movement cumulative saldo.</summary>
    [Fact]
    public void Replay_IsInvariantUnderReceiptOrder()
    {
        var first = Movement(MovementKind.Saida, 10, DateTimeOffset.Parse("2026-09-25T10:00:00Z"), 1);
        var second = Movement(MovementKind.Entrada, 10, DateTimeOffset.Parse("2026-09-27T10:00:00Z"), 2);

        var forward = OutstandingProjection.Replay([first, second]);
        var reordered = OutstandingProjection.Replay([second, first]);

        // The SUM is commutative; the value is the same either way.
        Assert.Equal(0, forward);
        Assert.Equal(0, reordered);
    }

    /// <summary>Each movement is applied exactly once; a replayed ledger never misses a row.</summary>
    [Fact]
    public void Replay_AppliesEveryMovementExactlyOnce()
    {
        var ledger = new List<BoquilhaMovement>
        {
            Movement(MovementKind.Saida, 1, DateTimeOffset.Parse("2026-09-25T10:00:00Z"), 1),
            Movement(MovementKind.Saida, 1, DateTimeOffset.Parse("2026-09-26T10:00:00Z"), 2),
            Movement(MovementKind.Saida, 1, DateTimeOffset.Parse("2026-09-27T10:00:00Z"), 3),
            Movement(MovementKind.Entrada, 1, DateTimeOffset.Parse("2026-09-28T10:00:00Z"), 4),
            Movement(MovementKind.EntradaSemReparacao, 1, DateTimeOffset.Parse("2026-09-29T10:00:00Z"), 5),
        };

        // Σ Saída (3) − Σ Entrada (1) − Σ Entrada sem reparação (1) = 1.
        Assert.Equal(1, OutstandingProjection.Replay(ledger));
    }
}