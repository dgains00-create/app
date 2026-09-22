using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.UnitTests.Frontend.Shared;

/// <summary>
/// P2-T03 (A6) unit — the repeated-row carriers and the deterministic row mechanics.
/// Authority: <c>plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md</c> §3.3, §4.2 and
/// the matrix rows MR1–MR16 (AC-25 … AC-34, AC-45, AC-46).
/// Purpose: prove the frozen row mechanics as failing-if-removed unit evidence — stable frontend
/// row identity across edit and removal, configurable minimum enforcement (including exactly one)
/// with the supplied visible removal reason, the immediate-application invariant, add allocation and
/// the documented focus targets after an add and after a removal — and prove that the primitive
/// defines no measurement field list, formula, tolerance, nominal value or validation verdict.
/// Preconditions: none; the model is a deterministic presentation-only type.
/// Required non-effects: no re-keying, no minimum breach, no computed verdict and no domain
/// vocabulary on any member.
/// </summary>
public sealed class MeasurementRowsContractTests
{
    private const string MinimumReason = "É necessária pelo menos uma linha.";
    private const string StructuralReason = "Guardar em curso.";
    private const string RemovalReason = "Remoção indisponível.";

    [Fact]
    public void MR1_OneRowMinimum_RefusesRemoval_AndExposesTheSuppliedReason()
    {
        var interaction = new MeasurementRowsInteraction(1, minimumViolationReason: MinimumReason);
        interaction.Sync([Row("row-1", "campo-a")]);

        var outcome = interaction.Remove("row-1");

        Assert.True(outcome.Refused);
        Assert.False(outcome.HasEvent);
        Assert.Equal(MinimumReason, outcome.RefusalReason);
        Assert.Equal(MinimumReason, interaction.RemovalDisabledReason);
        Assert.Equal(MeasurementRowsFocusKind.None, outcome.FocusTarget.Kind);
        Assert.Equal(["row-1"], interaction.RowKeys);
    }

    [Fact]
    public void MR2_MinimumIsEnforced_AndConsecutiveRemovalsNeverBreachIt()
    {
        var interaction = new MeasurementRowsInteraction(2, minimumViolationReason: MinimumReason);
        interaction.Sync([Row("row-1"), Row("row-2"), Row("row-3")]);

        var accepted = interaction.Remove("row-1");

        Assert.False(accepted.Refused);
        Assert.Equal(MeasurementRowsEventKind.RemoveRequested, accepted.Event!.Kind);
        Assert.Equal(["row-2", "row-3"], interaction.RowKeys);
        Assert.True(interaction.RowCount >= interaction.MinimumRowCount);

        // The next removal would breach the minimum, so it is refused and applied nowhere.
        var refused = interaction.Remove("row-2");

        Assert.True(refused.Refused);
        Assert.Equal(MinimumReason, refused.RefusalReason);
        Assert.Equal(["row-2", "row-3"], interaction.RowKeys);

        Assert.True(interaction.RowCount >= interaction.MinimumRowCount);
    }

    [Fact]
    public void MR3_MinimumViolationReason_IsMandatoryWheneverTheMinimumIsAtLeastOne()
    {
        Assert.ThrowsAny<ArgumentException>(() => new MeasurementRowsInteraction(1));
        Assert.ThrowsAny<ArgumentException>(() => new MeasurementRowsInteraction(3, "dmo-row-", " "));

        var withoutMinimum = new MeasurementRowsInteraction(0);

        Assert.Null(withoutMinimum.MinimumViolationReason);
        Assert.Equal(0, withoutMinimum.MinimumRowCount);
    }

    [Fact]
    public void MR4_RowIdentity_IsStableAcrossEditsAndReSyncs()
    {
        var interaction = new MeasurementRowsInteraction(0);
        interaction.Sync([Row("row-1", "campo-a"), Row("row-2", "campo-b")]);

        interaction.ChangeValue("row-1", "campo-a", "novo valor");
        interaction.ChangeValue("row-1", "campo-a", "outro valor");

        Assert.Equal(["row-1", "row-2"], interaction.RowKeys);

        interaction.Sync([Row("row-1", "campo-a"), Row("row-2", "campo-b")]);

        Assert.Equal(["row-1", "row-2"], interaction.RowKeys);
    }

    [Fact]
    public void MR5_SurvivingRowKeys_AreUnchangedAndOrderedAfterARemoval()
    {
        var interaction = new MeasurementRowsInteraction(1, minimumViolationReason: MinimumReason);
        interaction.Sync([Row("row-1"), Row("row-2"), Row("row-3")]);

        var outcome = interaction.Remove("row-2");

        Assert.False(outcome.Refused);
        Assert.Equal(["row-1", "row-3"], interaction.RowKeys);
    }

    [Fact]
    public void MR6_AddAllocatesANewStableFrontendRowKey_AndAppendsIt()
    {
        var interaction = new MeasurementRowsInteraction(0);
        interaction.Sync([Row("row-1"), Row("row-2")]);

        var outcome = interaction.Add();

        Assert.False(outcome.Refused);
        Assert.Equal(MeasurementRowsEventKind.AddRequested, outcome.Event!.Kind);
        Assert.NotNull(outcome.AllocatedRowKey);
        Assert.Equal(2, outcome.AllocatedIndex);
        Assert.Equal(2, outcome.Event.AllocatedIndex);
        Assert.DoesNotContain(outcome.AllocatedRowKey!, interaction.RowKeys.Take(2), StringComparer.Ordinal);
        Assert.Equal(interaction.LastAllocatedKey, outcome.AllocatedRowKey);
        Assert.StartsWith(MeasurementRowsInteraction.DefaultKeyPrefix, outcome.AllocatedRowKey!, StringComparison.Ordinal);

        // Frontend-only: the allocated key carries no canonical identity shape.
        foreach (var token in P2T03TypeScan.CanonicalIdentityTokens)
        {
            Assert.DoesNotContain(token, outcome.AllocatedRowKey!, StringComparison.OrdinalIgnoreCase);
        }

        // The key stays stable while the consumer keeps re-supplying that row.
        interaction.Sync(
        [
            Row("row-1"), Row("row-2"),
            Row(outcome.AllocatedRowKey!, "campo-novo"),
        ]);

        Assert.Contains(outcome.AllocatedRowKey!, interaction.RowKeys);
        Assert.Equal(outcome.AllocatedRowKey, interaction.LastAllocatedKey);
    }

    [Fact]
    public void MR7_FocusAfterAdd_IsTheNewRowsFirstEditableControl()
    {
        var interaction = new MeasurementRowsInteraction(0);
        interaction.Sync([Row("row-1", "campo-a")]);

        var outcome = interaction.Add();
        var allocated = outcome.AllocatedRowKey!;

        Assert.Equal(MeasurementRowsFocusKind.FirstEditableFieldInRow, outcome.FocusTarget.Kind);
        Assert.Equal(allocated, outcome.FocusTarget.RowKey);
        Assert.Null(outcome.FocusTarget.FieldKey);

        interaction.Sync(
        [
            Row("row-1", "campo-a"),
            Row(allocated, "campo-b", "campo-c"),
        ]);

        var resolved = interaction.ResolveFocusTarget();

        Assert.Equal(MeasurementRowsFocusKind.FirstEditableFieldInRow, resolved.Kind);
        Assert.Equal(allocated, resolved.RowKey);
        Assert.Equal("campo-b", resolved.FieldKey);

        // Documented fallback: a row with no editable field targets the add control.
        interaction.Sync(
        [
            Row("row-1", "campo-a"),
            Row(allocated),
        ]);

        Assert.True(interaction.ResolveFocusTarget().IsAddControl);
    }

    [Fact]
    public void MR8_FocusAfterRemoval_IsTheNearestSurvivingRowsFirstEditableControl()
    {
        var interaction = new MeasurementRowsInteraction(0);
        interaction.Sync([Row("row-1", "a1"), Row("row-2", "a2"), Row("row-3", "a3")]);

        // Removing a middle row targets the row that now occupies the removed index.
        var middle = interaction.Remove("row-2");

        Assert.Equal(MeasurementRowsFocusKind.NearestSurvivingRowFirstEditableField, middle.FocusTarget.Kind);
        Assert.Equal("row-3", middle.FocusTarget.RowKey);

        interaction.Sync([Row("row-1", "a1"), Row("row-3", "a3")]);

        var middleResolved = interaction.ResolveFocusTarget();

        Assert.Equal("row-3", middleResolved.RowKey);
        Assert.Equal("a3", middleResolved.FieldKey);

        // Removing the last row targets the preceding row.
        var last = interaction.Remove("row-3");

        Assert.Equal("row-1", last.FocusTarget.RowKey);

        interaction.Sync([Row("row-1", "a1")]);

        Assert.Equal("a1", interaction.ResolveFocusTarget().FieldKey);

        // Removing the only row falls back to the add control.
        var only = interaction.Remove("row-1");

        Assert.True(only.FocusTarget.IsAddControl);

        interaction.Sync([]);

        Assert.True(interaction.ResolveFocusTarget().IsAddControl);
    }

    [Fact]
    public void MR9_StructureChangesNeverResetFocusToPageStart()
    {
        var interaction = new MeasurementRowsInteraction(1, minimumViolationReason: MinimumReason);
        interaction.Sync([Row("row-1", "a1")]);

        var refusedRemoval = interaction.Remove("row-1");

        Assert.True(refusedRemoval.Refused);
        Assert.False(refusedRemoval.FocusTarget.HasTarget);
        Assert.Equal(MeasurementRowsFocusKind.None, refusedRemoval.FocusTarget.Kind);

        interaction.SetStructuralMutation(false, StructuralReason);

        var refusedAdd = interaction.Add();

        Assert.True(refusedAdd.Refused);
        Assert.Equal(MeasurementRowsFocusKind.None, refusedAdd.FocusTarget.Kind);
        Assert.True(interaction.ResolveFocusTarget().Kind == MeasurementRowsFocusKind.None);
    }

    [Fact]
    public void MR10_ZeroRowsIsValidOnlyWithoutAMinimum()
    {
        var presentation = MeasurementRowsPresentation.Create(CommonState.Empty, "Linhas", 0, [], message: "Sem linhas.");

        Assert.Empty(presentation.Rows);
        Assert.Equal(0, presentation.MinimumRowCount);

        Assert.ThrowsAny<ArgumentException>(() =>
            MeasurementRowsPresentation.Create(CommonState.Empty, "Linhas", 1, [], message: "Sem linhas."));

        Assert.ThrowsAny<ArgumentException>(() =>
            MeasurementRowsPresentation.Create(
                CommonState.Empty, "Linhas", 1, [], minimumViolationReason: MinimumReason,
                message: "Sem linhas."));

        var interaction = new MeasurementRowsInteraction(1, minimumViolationReason: MinimumReason);

        Assert.ThrowsAny<ArgumentException>(() => interaction.Sync([]));
    }

    [Fact]
    public void MR11_StructuralMutationIsRefused_WhileTheConsumerSaysItIsUnavailable()
    {
        var interaction = new MeasurementRowsInteraction(0);
        interaction.Sync([Row("row-1", "a1"), Row("row-2", "a2")]);
        interaction.SetStructuralMutation(false, StructuralReason);

        var add = interaction.Add();
        var remove = interaction.Remove("row-1");

        Assert.True(add.Refused);
        Assert.Equal(StructuralReason, add.RefusalReason);
        Assert.False(add.HasEvent);

        Assert.True(remove.Refused);
        Assert.Equal(StructuralReason, remove.RefusalReason);
        Assert.False(remove.HasEvent);

        // Supplied/tracked rows are unchanged.
        Assert.Equal(["row-1", "row-2"], interaction.RowKeys);
        Assert.Equal(StructuralReason, interaction.RemovalDisabledReason);

        Assert.ThrowsAny<ArgumentException>(() => interaction.SetStructuralMutation(false, null));
    }

    [Fact]
    public void MR12_Create_FailsClosedOnEverySuppliedInconsistency()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MeasurementRowsPresentation.Create((CommonState)99, "Linhas", 0));

        Assert.ThrowsAny<ArgumentException>(() =>
            MeasurementRowsPresentation.Create(CommonState.Ready, " ", 0));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MeasurementRowsPresentation.Create(CommonState.Ready, "Linhas", -1));

        Assert.ThrowsAny<ArgumentException>(() =>
            MeasurementRowsPresentation.Create(CommonState.Ready, "Linhas", 2, [Row("row-1")],
                minimumViolationReason: MinimumReason));

        Assert.ThrowsAny<ArgumentException>(() =>
            MeasurementRowsPresentation.Create(CommonState.Ready, "Linhas", 1, [Row("row-1")]));

        Assert.ThrowsAny<ArgumentException>(() =>
            MeasurementRowsPresentation.Create(CommonState.Ready, "Linhas", 0, [Row("row-1"), Row("row-1")]));

        Assert.ThrowsAny<ArgumentException>(() =>
            MeasurementRowsPresentation.Create(CommonState.Ready, "Linhas", 0, [Row("row-1")], addEnabled: false));

        Assert.ThrowsAny<ArgumentException>(() =>
            MeasurementRowsPresentation.Create(CommonState.Ready, "Linhas", 0, [Row("row-1")], removeEnabled: false));

        Assert.ThrowsAny<ArgumentException>(() =>
            MeasurementRowsPresentation.Create(
                CommonState.Ready, "Linhas", 0, [Row("row-1")], structuralMutationAllowed: false));

        Assert.ThrowsAny<ArgumentException>(() =>
            MeasurementRowsPresentation.Create(CommonState.Unavailable, "Linhas", 0, [Row("row-1")]));

        // Field-level fail-closed rules.
        Assert.ThrowsAny<ArgumentException>(() =>
            MeasurementRowFieldPresentation.Create(" ", "Etiqueta"));

        Assert.ThrowsAny<ArgumentException>(() =>
            MeasurementRowFieldPresentation.Create("campo", " "));

        Assert.ThrowsAny<ArgumentException>(() =>
            MeasurementRowFieldPresentation.Create("campo", "Etiqueta", kind: MeasurementRowFieldKind.Choice));

        Assert.ThrowsAny<ArgumentException>(() =>
            MeasurementRowFieldPresentation.Create(
                "campo", "Etiqueta",
                options: [MeasurementRowFieldOptionPresentation.Create("a", "A")]));

        Assert.ThrowsAny<ArgumentException>(() =>
            MeasurementRowFieldPresentation.Create(
                "campo", "Etiqueta",
                kind: MeasurementRowFieldKind.Choice,
                options:
                [
                    MeasurementRowFieldOptionPresentation.Create("a", "A"),
                    MeasurementRowFieldOptionPresentation.Create("a", "Outra"),
                ]));

        Assert.ThrowsAny<ArgumentException>(() =>
            MeasurementRowFieldPresentation.Create("campo", "Etiqueta", editable: false));

        Assert.ThrowsAny<ArgumentException>(() =>
            MeasurementRowPresentation.Create("row-1", "contexto", [Field("campo"), Field("campo")]));

        Assert.ThrowsAny<ArgumentException>(() =>
            MeasurementRowPresentation.Create("row-1", " "));
    }

    [Fact]
    public void MR13_NoMeasurementDomainConceptOrComputedVerdictOnAnyMember()
    {
        string[] domain =
        [
            "nominal", "toler", "capacidade", "peso", "pegamentos", "costura", "ovaliz",
            "densidade", "formula", "volume", "corridor",
        ];

        string[] computed = ["Calculate", "Compute", "Evaluate", "Verdict", "Formula", "Convert"];

        var names = P2T03TypeScan.MemberNames(P2T03TypeScan.RowsTypes)
            .Concat(P2T03TypeScan.ConstantStrings(P2T03TypeScan.RowsTypes))
            .Concat(P2T03TypeScan.EnumMemberNames(P2T03TypeScan.RowsTypes));

        foreach (var name in names)
        {
            foreach (var marker in domain.Concat(computed))
            {
                Assert.DoesNotContain(marker, name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void MR14_OutOfDomainSuppliedValues_AreAcceptedVerbatim_WithNoComputedVerdict()
    {
        var field = MeasurementRowFieldPresentation.Create(
            "campo-a", "Etiqueta fornecida", value: "999999", kind: MeasurementRowFieldKind.Numeric);

        Assert.Equal("999999", field.Value);

        var arbitrary = MeasurementRowFieldPresentation.Create("campo-b", "Etiqueta fornecida", value: "abc-Ω");
        var row = MeasurementRowPresentation.Create("row-1", "contexto", [arbitrary, field]);

        Assert.Equal("abc-Ω", row.Fields[0].Value);

        var presentation = MeasurementRowsPresentation.Create(
            CommonState.Ready, "Linhas", 1, [row], minimumViolationReason: MinimumReason);

        Assert.Equal("999999", presentation.Rows[0].Fields[1].Value);

        // The primitive keeps the supplied validation display only; it computes nothing itself.
        var withDisplay = MeasurementRowFieldPresentation.Create(
            "campo-c", "Etiqueta fornecida", value: "1",
            validationText: "Texto de validação fornecido.", validationTone: StatusTone.Warning);

        Assert.True(withDisplay.HasValidationText);
        Assert.Equal("warning", withDisplay.ValidationToneToken);
    }

    [Fact]
    public void MR15_SuppliedKeysAreOpaque_AndRoundTripVerbatim()
    {
        var canonicalLooking = string.Concat("tool", "_", "id", ":opaque");

        var interaction = new MeasurementRowsInteraction(0);
        interaction.Sync([Row("row-1", "campo-a"), Row(canonicalLooking, "campo-b")]);

        Assert.Equal(["row-1", canonicalLooking], interaction.RowKeys);

        var outcome = interaction.ChangeValue(canonicalLooking, "campo-b", "valor");

        Assert.Equal(canonicalLooking, outcome.Event!.RowKey);
        Assert.Equal("campo-b", outcome.Event.FieldKey);
        Assert.Equal("valor", outcome.Event.Value);
    }

    [Fact]
    public void MR16_RowsTypes_AreDomainNeutral()
    {
        foreach (var type in P2T03TypeScan.RowsTypes)
        {
            Assert.Equal(P2T03TypeScan.ContractsNamespace, type.Namespace);
        }

        var memberTypes = P2T03TypeScan.MemberTypes(P2T03TypeScan.RowsTypes).ToList();

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
    }

    // Additive evidence for Architect observation O5 (contract §3.3.6/§4.2.1 state↔flag mapping).
    [Fact]
    public void O5_SuppliedStateAndStructuralFlag_StayConsistentPerThePinnedMapping()
    {
        foreach (var state in new[]
                 {
                     CommonState.Saving, CommonState.Submitting, CommonState.Unavailable,
                     CommonState.PermissionDenied,
                 })
        {
            // The four pinned states block structural mutation even if the supplied flag says otherwise.
            var presentation = MeasurementRowsPresentation.Create(
                state, "Linhas", 1, [Row("row-1")],
                minimumViolationReason: MinimumReason,
                structuralMutationAllowed: true,
                message: "Mensagem fornecida.");

            Assert.True(presentation.StructuralMutationBlocked, $"Structural mutation must be blocked in {state}.");
            Assert.False(presentation.AddControlEnabled);
            Assert.False(presentation.RemoveControlEnabled);
            Assert.True(presentation.FieldsReadOnly);

            // The supplied reason is the structural one the consumer provided for that state.
            var consistent = MeasurementRowsPresentation.Create(
                state, "Linhas", 1, [Row("row-1")],
                minimumViolationReason: MinimumReason,
                structuralMutationAllowed: false,
                structuralMutationReason: StructuralReason,
                message: "Mensagem fornecida.");

            Assert.Equal(StructuralReason, consistent.RemovalDisabledReason);
        }

        // A flag of false blocks mutation even in an otherwise editable state.
        var flagged = MeasurementRowsPresentation.Create(
            CommonState.Stale, "Linhas", 1, [Row("row-1"), Row("row-2")],
            minimumViolationReason: MinimumReason,
            structuralMutationAllowed: false,
            structuralMutationReason: StructuralReason,
            message: "Mensagem fornecida.");

        Assert.True(flagged.StructuralMutationBlocked);
        Assert.False(flagged.AddControlEnabled);

        // The model side mirrors the same supplied decision.
        var interaction = new MeasurementRowsInteraction(1, minimumViolationReason: MinimumReason);
        interaction.Sync([Row("row-1")]);

        interaction.SetStructuralMutation(false, StructuralReason);

        Assert.True(interaction.Add().Refused);
        Assert.Equal(StructuralReason, interaction.RemovalDisabledReason);
    }

    private static MeasurementRowFieldPresentation Field(string key, string? label = null) =>
        MeasurementRowFieldPresentation.Create(key, label ?? $"Etiqueta {key}");

    private static MeasurementRowPresentation Row(string key, params string[] editableFieldKeys)
    {
        var fields = editableFieldKeys.Select(fieldKey => Field(fieldKey)).ToList();

        return MeasurementRowPresentation.Create(key, $"contexto {key}", fields);
    }
}
