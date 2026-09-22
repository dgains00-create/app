using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.IntegrationTests.Frontend.Shared;

/// <summary>
/// P2-T03 (A5/A6) presentation fixture: the supplied carriers the rendered tests render.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §8 ("rendered tests must exercise the real compiled partials") and
/// §2.3 (every carried key is opaque presentation data).
/// <para>
/// This fixture holds <b>only</b> opaque presentation keys and consumer-supplied display text. It
/// declares no canonical domain identity, no domain vocabulary and no backend, persistence,
/// authorization or navigation concept: the architecture rows ST3 and ST6 scan this file to prove
/// exactly that.
/// </para>
/// </remarks>
internal static class P2T03Fixtures
{
    public const string PickerRegionLabel = "Seleção de registo";

    public const string MinimumReason = "É necessária pelo menos uma linha.";

    public const string StructuralReason = "Guardar em curso.";

    public static ToolPickerFactPresentation Fact(string label, string value) =>
        ToolPickerFactPresentation.Create(label, value);

    public static ToolSummaryFactPresentation SummaryFact(string label, string value) =>
        ToolSummaryFactPresentation.Create(label, value);

    public static ToolPickerCandidatePresentation Candidate(string key) =>
        ToolPickerCandidatePresentation.Create(
            key,
            $"candidato {key}",
            [Fact("Lote", $"lote-{key}"), Fact("Máquinas", "linha 1")]);

    public static ToolPickerPresentation Picker(
        CommonState state,
        IReadOnlyList<ToolPickerCandidatePresentation>? candidates = null,
        string? selectedCandidateKey = null,
        string? query = null,
        string? originToken = null,
        SharedActionPresentation? createAction = null,
        SharedActionPresentation? cancelAction = null,
        SharedActionPresentation? retryAction = null,
        string? expectedTypeLabel = null,
        string? resultSummary = null,
        string? reason = null)
    {
        var suppliedCandidates = candidates ?? [];

        return ToolPickerPresentation.Create(
            state,
            PickerRegionLabel,
            query: query ?? string.Empty,
            candidates: suppliedCandidates,
            selectedCandidateKey: selectedCandidateKey,
            originToken: originToken,
            originContext: [Fact("Origem", "contexto de origem")],
            expectedTypeLabel: expectedTypeLabel,
            createAction: createAction,
            cancelAction: cancelAction,
            retryAction: retryAction,
            message: state == CommonState.Ready ? null : "Mensagem fornecida pelo consumidor.",
            reason: reason,
            resultSummary: resultSummary);
    }

    public static ToolSummaryRowPresentation Summary(
        CommonState state,
        ToolSummaryFactPresentation? quantity = null,
        RecordStatusPresentation? status = null,
        IReadOnlyList<SharedActionPresentation>? actions = null,
        IReadOnlyList<ToolSummaryFactPresentation>? additionalFacts = null,
        bool selected = false) =>
        ToolSummaryRowPresentation.Create(
            state,
            "item-1",
            accessibleContext: "contexto item-1",
            type: SummaryFact("Tipo", "MF"),
            reference: SummaryFact("Referência", "REF-1"),
            lot: SummaryFact("Lote", "L-9"),
            machines: SummaryFact("Máquinas", "linha 1, linha 2"),
            quantity: quantity,
            process: SummaryFact("Processo", "A"),
            context: null,
            additionalFacts: additionalFacts,
            status: status,
            actions: actions,
            selected: selected,
            message: state == CommonState.Ready ? null : "Mensagem fornecida pelo consumidor.");

    public static MeasurementRowFieldPresentation EditableField(string key, string value = "10") =>
        MeasurementRowFieldPresentation.Create(key, $"Campo {key}", value);

    public static MeasurementRowPresentation Row(
        string key,
        string? validationText = null,
        StatusTone validationTone = StatusTone.Neutral) =>
        MeasurementRowPresentation.Create(
            key,
            $"linha {key}",
            [EditableField($"{key}-a"), EditableField($"{key}-b")],
            validationText,
            validationTone);

    public static MeasurementRowsPresentation Rows(
        CommonState state,
        int minimumRowCount,
        IReadOnlyList<MeasurementRowPresentation>? rows = null,
        bool addEnabled = true,
        bool removeEnabled = true,
        bool structuralMutationAllowed = true) =>
        MeasurementRowsPresentation.Create(
            state,
            "Linhas de medição",
            minimumRowCount,
            rows: rows,
            minimumViolationReason: minimumRowCount >= 1 ? MinimumReason : null,
            addEnabled: addEnabled,
            addDisabledReason: addEnabled ? null : "Adição indisponível.",
            removeEnabled: removeEnabled,
            removeDisabledReason: removeEnabled ? null : "Remoção indisponível.",
            structuralMutationAllowed: structuralMutationAllowed,
            structuralMutationReason: structuralMutationAllowed ? null : StructuralReason,
            message: state == CommonState.Ready ? null : "Mensagem fornecida pelo consumidor.",
            reason: state == CommonState.Ready ? null : "Motivo fornecido pelo consumidor.");

    public static DecisionBarActionPresentation Action(
        string key,
        DecisionBarActionGroup group,
        bool enabled = true,
        string? pendingLabel = null) =>
        DecisionBarActionPresentation.Create(
            enabled
                ? SharedActionPresentation.CreateEnabled(key, $"Ação {key}", pendingLabel)
                : SharedActionPresentation.CreateDisabled(key, $"Ação {key}", "Motivo fornecido pelo consumidor."),
            group);

    public static DecisionBarPresentation Bar(
        CommonState state,
        IReadOnlyList<DecisionBarActionPresentation> actions,
        string? pendingActionKey = null,
        string? statusText = null,
        string? regionLabel = null) =>
        DecisionBarPresentation.Create(
            state,
            actions,
            pendingActionKey: pendingActionKey,
            regionLabel: regionLabel,
            statusText: statusText,
            message: state == CommonState.Ready ? null : "Mensagem fornecida pelo consumidor.",
            reason: state == CommonState.Ready ? null : "Motivo fornecido pelo consumidor.");
}
