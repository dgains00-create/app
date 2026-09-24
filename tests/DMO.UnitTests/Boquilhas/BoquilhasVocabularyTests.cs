using DMO.Domain.Boquilhas;

namespace DMO.UnitTests.Boquilhas;

/// <summary>
/// The closed movement vocabulary of the OWNER CLARIFICATION register model: EXACTLY three
/// movement kinds with the exact stored tokens; no Início, no Irreparável, no lifecycle kind.
/// </summary>
public sealed class BoquilhasVocabularyTests
{
    [Fact]
    public void TheClosedSet_HasExactlyThreeKinds()
    {
        var values = Enum.GetValues<MovementKind>();

        Assert.Equal(3, values.Length);
        Assert.Equal(
            [MovementKind.Saida, MovementKind.Entrada, MovementKind.EntradaSemReparacao],
            values);
    }

    [Fact]
    public void Tokens_AreExactAndClosed()
    {
        Assert.Equal("saida", MovementKindTokens.ToToken(MovementKind.Saida));
        Assert.Equal("entrada", MovementKindTokens.ToToken(MovementKind.Entrada));
        Assert.Equal("entrada_sem_reparacao", MovementKindTokens.ToToken(MovementKind.EntradaSemReparacao));

        Assert.Equal(MovementKind.Saida, MovementKindTokens.Parse("saida"));
        Assert.Equal(MovementKind.Entrada, MovementKindTokens.Parse("entrada"));
        Assert.Equal(MovementKind.EntradaSemReparacao, MovementKindTokens.Parse("entrada_sem_reparacao"));

        Assert.Null(MovementKindTokens.Parse("inicio"));
        Assert.Null(MovementKindTokens.Parse("irreparavel"));
        Assert.Null(MovementKindTokens.Parse("editar"));
        Assert.Null(MovementKindTokens.Parse("SAIDA"));
        Assert.Null(MovementKindTokens.Parse(null));
        Assert.Null(MovementKindTokens.Parse(""));
    }

    [Fact]
    public void Labels_AreThePortugueseOperationalVocabulary()
    {
        Assert.Equal("Saída", MovementKindTokens.ToLabel(MovementKind.Saida));
        Assert.Equal("Entrada", MovementKindTokens.ToLabel(MovementKind.Entrada));
        Assert.Equal("Entrada sem reparação", MovementKindTokens.ToLabel(MovementKind.EntradaSemReparacao));
    }

    /// <summary>Editar is an action, never a movement kind (the four-type set and its Início/
    /// Irreparável members are superseded).</summary>
    [Fact]
    public void NoLifecycleOrSupersededKinds_Exist()
    {
        foreach (var name in Enum.GetNames<MovementKind>())
        {
            Assert.NotEqual("Inicio", name);
            Assert.NotEqual("Irreparavel", name);
        }
    }

    [Fact]
    public void TheRegister_IsAnIdentityCarrier_NotALifecycle()
    {
        // The register exposes the derived outstanding and the ledger; it carries no status.
        // Production-linked: the REAL bq_id anchor (one register per BQ context).
        var productionLinked = new BoquilhaRegister(
            BoquilhasId.From(Guid.NewGuid()),
            BqId: Guid.NewGuid(),
            ToolId: null,
            Version: 1,
            CreatedByUserId: Guid.NewGuid(),
            CreatedAt: DateTimeOffset.UtcNow,
            Movements: []);

        Assert.Equal(0, productionLinked.Outstanding);
        Assert.Empty(productionLinked.Movements);
        Assert.False(productionLinked.IsPending);
    }

    /// <summary>§34 — the TRANSITIONAL pré-JobOn anchor is exactly that: pending, versioned, and
    /// never a permanent standalone (BqId null, ToolId set; IsPending true).</summary>
    [Fact]
    public void ThePendingRegister_IsTheTransitionalPreJobOnAnchor()
    {
        var pending = new BoquilhaRegister(
            BoquilhasId.From(Guid.NewGuid()),
            BqId: null,
            ToolId: Guid.NewGuid(),
            Version: 1,
            CreatedByUserId: Guid.NewGuid(),
            CreatedAt: DateTimeOffset.UtcNow,
            Movements: []);

        Assert.True(pending.IsPending);
        Assert.Null(pending.BqId);
        Assert.NotNull(pending.ToolId);
    }
}