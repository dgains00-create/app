using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.UnitTests.Frontend.Shared;

/// <summary>
/// P2-T03 (A5) unit — the compact summary row carrier and its rendering rules.
/// Authority: <c>plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md</c> §3.2, §3.2.3 and
/// the matrix rows TR1–TR9 (AC-17 … AC-24, AC-43, AC-45, AC-46).
/// Purpose: prove that the row renders exactly the supplied facts in the fixed slot order, that a
/// missing optional fact is never rendered as zero, false or any placeholder, that the accepted
/// status and action carriers are reused rather than re-implemented, and that the row owns no
/// lookup, inference or selection arbitration.
/// Preconditions: none; the carriers are deterministic presentation-only types.
/// Required non-effects: no derived fact, no invented value, no status vocabulary of its own.
/// </summary>
public sealed class ToolSummaryRowContractTests
{
    [Fact]
    public void TR1_VisibleFacts_EqualTheSuppliedFacts_InTheFixedSlotOrder()
    {
        var row = ToolSummaryRowPresentation.Create(
            CommonState.Ready,
            "item-1",
            accessibleContext: "contexto item-1",
            type: Fact("Tipo", "MF"),
            reference: Fact("Referência", "REF-1"),
            lot: Fact("Lote", "L-9"),
            machines: Fact("Máquinas", "Linha 1, Linha 2"),
            quantity: Fact("Quantidade", "12"),
            process: Fact("Processo", "A"),
            context: Fact("Contexto", "nota"),
            additionalFacts: [Fact("Extra A", "1"), Fact("Extra B", "2")]);

        Assert.Equal(
            ["Tipo", "Referência", "Lote", "Máquinas", "Quantidade", "Processo", "Contexto", "Extra A", "Extra B"],
            row.VisibleFacts.Select(fact => fact.Label));

        Assert.Equal(
            ["MF", "REF-1", "L-9", "Linha 1, Linha 2", "12", "A", "nota", "1", "2"],
            row.VisibleFacts.Select(fact => fact.Value));

        // Exactly the supplied facts: no synthesized or duplicated entry.
        Assert.Equal(9, row.VisibleFacts.Count);
        Assert.True(row.HasFacts);
    }

    [Fact]
    public void TR2_MissingOptionalQuantity_IsNotZero_AndASuppliedZeroRendersAsZero()
    {
        var withoutQuantity = ToolSummaryRowPresentation.Create(
            CommonState.Ready, "item-1", reference: Fact("Referência", "REF-1"));

        Assert.DoesNotContain(withoutQuantity.VisibleFacts, fact => fact.Label == "Quantidade");
        Assert.DoesNotContain(withoutQuantity.VisibleFacts, fact => fact.Value == "0");
        Assert.Single(withoutQuantity.VisibleFacts);

        var withZero = ToolSummaryRowPresentation.Create(
            CommonState.Ready, "item-1", quantity: Fact("Quantidade", "0"));

        var quantity = Assert.Single(withZero.VisibleFacts);

        Assert.Equal("Quantidade", quantity.Label);
        Assert.Equal("0", quantity.Value);
    }

    [Fact]
    public void TR3_MissingOptionalFact_IsNotFalse_AndNoPlaceholderIsInvented()
    {
        string[] invented = ["false", "False", "Não", "—", "-", "0", string.Empty];

        var withoutSlot = ToolSummaryRowPresentation.Create(
            CommonState.Ready, "item-1", reference: Fact("Referência", "REF-1"));

        Assert.All(
            withoutSlot.VisibleFacts,
            fact => Assert.DoesNotContain(fact.Value, invented));

        // A consumer expressing a boolean-ish fact supplies its own text, rendered verbatim.
        var withExplicitValue = ToolSummaryRowPresentation.Create(
            CommonState.Ready, "item-1", context: Fact("Ativo", "Não"));

        var stated = Assert.Single(withExplicitValue.VisibleFacts);

        Assert.Equal("Ativo", stated.Label);
        Assert.Equal("Não", stated.Value);
    }

    [Fact]
    public void TR4_NoLookupInferenceOrRequiredFact()
    {
        // No required slot: additional facts alone, and even no facts at all, are valid.
        var additionalOnly = ToolSummaryRowPresentation.Create(
            CommonState.Ready, "item-1", additionalFacts: [Fact("Extra", "1")]);

        Assert.Single(additionalOnly.VisibleFacts);

        var empty = ToolSummaryRowPresentation.Create(CommonState.Ready, "item-1");

        Assert.Empty(empty.VisibleFacts);
        Assert.False(empty.HasFacts);

        string[] derived =
        [
            "Lookup", "Resolve", "Derive", "Infer", "Compute", "Aggregate", "Format", "Parse",
            "Default", "Sum", "Total", "Combine", "Merge", "Join",
        ];

        var names = P2T03TypeScan.MemberNames(P2T03TypeScan.SummaryTypes)
            .Concat(P2T03TypeScan.ConstantStrings(P2T03TypeScan.SummaryTypes));

        foreach (var name in names)
        {
            foreach (var marker in derived)
            {
                Assert.DoesNotContain(marker, name, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void TR5_StatusReusesTheAcceptedRecordStatusComponent()
    {
        Assert.Equal(
            typeof(RecordStatusPresentation),
            typeof(ToolSummaryRowPresentation).GetProperty("Status")!.PropertyType);

        // No P2-T03-owned status vocabulary, catalogue or tone map exists.
        Assert.DoesNotContain(
            P2T03TypeScan.SummaryTypes,
            type => type.IsEnum && type.Name.Contains("Status", StringComparison.Ordinal));

        var withoutStatus = ToolSummaryRowPresentation.Create(CommonState.Ready, "item-1");

        Assert.False(withoutStatus.HasStatus);
        Assert.Null(withoutStatus.Status);

        var withStatus = ToolSummaryRowPresentation.Create(
            CommonState.Ready,
            "item-1",
            status: RecordStatusPresentation.Create("Neutro fornecido"));

        Assert.True(withStatus.HasStatus);
        Assert.Equal(StatusTone.Neutral, withStatus.Status!.Tone);
    }

    [Fact]
    public void TR6_ActionsReuseTheAcceptedActionCarrier_AndPreserveSuppliedOrder()
    {
        Assert.Equal(
            typeof(IReadOnlyList<SharedActionPresentation>),
            typeof(ToolSummaryRowPresentation).GetProperty("Actions")!.PropertyType);

        var row = ToolSummaryRowPresentation.Create(
            CommonState.Ready,
            "item-1",
            actions:
            [
                SharedActionPresentation.CreateEnabled("first", "Primeira"),
                SharedActionPresentation.CreateDisabled("second", "Segunda", "Motivo fornecido."),
                SharedActionPresentation.Create("hidden", "Oculta", true, null, null, visible: false),
            ]);

        Assert.Equal(["first", "second"], row.VisibleActions.Select(action => action.Key));
        Assert.Equal("Motivo fornecido.", row.VisibleActions[1].DisabledReason);

        // The accepted rule is inherited: a disabled action without a supplied reason is rejected.
        Assert.ThrowsAny<ArgumentException>(() =>
            SharedActionPresentation.Create("third", "Terceira", enabled: false));
    }

    [Fact]
    public void TR7_Create_FailsClosedOnSuppliedInconsistencies()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ToolSummaryRowPresentation.Create((CommonState)99, "item-1"));

        Assert.ThrowsAny<ArgumentException>(() =>
            ToolSummaryRowPresentation.Create(CommonState.Ready, " "));

        Assert.ThrowsAny<ArgumentException>(() => ToolSummaryFactPresentation.Create(" ", "valor"));

        Assert.ThrowsAny<ArgumentException>(() => ToolSummaryFactPresentation.Create("Etiqueta", " "));

        Assert.ThrowsAny<ArgumentException>(() =>
            ToolSummaryRowPresentation.Create(CommonState.Stale, "item-1"));
    }

    [Fact]
    public void TR8_SelectedIsAControlledFact_WithNoSelectionArbitration()
    {
        Assert.Equal(typeof(bool), typeof(ToolSummaryRowPresentation).GetProperty("Selected")!.PropertyType);

        string[] arbitration = ["Click", "Space", "Enter", "SelectedKey", "OnSelect", "Toggle", "Activate"];

        var names = P2T03TypeScan.MemberNames(P2T03TypeScan.SummaryTypes);

        foreach (var name in names)
        {
            foreach (var marker in arbitration)
            {
                Assert.DoesNotContain(marker, name, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void TR9_SummaryTypes_AreDomainNeutral_WithNoCanonicalIdentityName()
    {
        foreach (var type in P2T03TypeScan.SummaryTypes)
        {
            Assert.Equal(P2T03TypeScan.ContractsNamespace, type.Namespace);
        }

        var memberTypes = P2T03TypeScan.MemberTypes(P2T03TypeScan.SummaryTypes).ToList();

        foreach (var marker in new[]
                 {
                     "Service", "DbContext", "HttpClient", "Supabase", "Session", "Clock",
                     "DMO.Application", "DMO.Domain", "DMO.Infrastructure", "Uri",
                 })
        {
            Assert.DoesNotContain(
                memberTypes,
                type => (type.FullName ?? type.Name).Contains(marker, StringComparison.Ordinal));
        }

        var names = P2T03TypeScan.MemberNames(P2T03TypeScan.SummaryTypes)
            .Concat(P2T03TypeScan.ConstantStrings(P2T03TypeScan.SummaryTypes))
            .Concat(P2T03TypeScan.EnumMemberNames(P2T03TypeScan.SummaryTypes));

        foreach (var name in names)
        {
            foreach (var token in P2T03TypeScan.CanonicalIdentityTokens)
            {
                Assert.DoesNotContain(token, name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static ToolSummaryFactPresentation Fact(string label, string value) =>
        ToolSummaryFactPresentation.Create(label, value);
}
