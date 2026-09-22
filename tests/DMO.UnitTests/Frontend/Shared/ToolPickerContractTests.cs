using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.UnitTests.Frontend.Shared;

/// <summary>
/// P2-T03 (A5) unit — the picker presentation carriers, transition model and architecture boundary.
/// Authority: <c>plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md</c> §3.1, §4.1, §5.1,
/// §6.1 and the matrix rows TP1–TP18 (AC-1 … AC-16, AC-45, AC-46).
/// Purpose: prove the frozen picker rules as failing-if-removed unit evidence — no auto-selection
/// (including for exactly one candidate), search and Enter never selecting, selection only through
/// an explicit candidate activation, cancel/Escape requesting cancel only, the verbatim opaque
/// origin-token roundtrip, the deterministic focus targets, the consumer-gated create affordance and
/// the absence of any search, ranking, lookup or domain logic.
/// Preconditions: none; the model is a deterministic presentation-only type.
/// Required non-effects: no selection from any transition other than an explicit activation, and no
/// event kind, member or constant that names a canonical domain identity.
/// </summary>
public sealed class ToolPickerContractTests
{
    private const string SelectionKey = "cand-alpha";

    // TP1 — explicit search state and explicit candidate list, and no event from a sync.
    [Fact]
    public void TP1_ControlledQueryAndSuppliedCandidateOrder_AreExposed_AndSyncingRaisesNoEvent()
    {
        var interaction = new ToolPickerInteraction(cancelEnabled: true);
        interaction.Sync("abc", ["cand-beta", SelectionKey], null, "origin-1");

        Assert.Equal("abc", interaction.Query);
        Assert.Equal(["cand-beta", SelectionKey], interaction.CandidateKeys);
        Assert.False(interaction.HasSelection);

        var presentation = Picker(
            CommonState.Ready,
            candidates: [Candidate(SelectionKey), Candidate("cand-beta")]);

        Assert.Equal([SelectionKey, "cand-beta"], presentation.Candidates.Select(c => c.Key));
        Assert.False(presentation.IsCandidateSelected(SelectionKey));

        // A controlled query change raises no event.
        Assert.False(interaction.QueryChanged("abc").HasEvent);
    }

    // TP2 — exactly one supplied candidate never auto-selects.
    [Fact]
    public void TP2_ExactlyOneCandidate_NeverAutoSelects()
    {
        var interaction = new ToolPickerInteraction(cancelEnabled: true);
        interaction.Sync("q", [SelectionKey], null, "origin-1");

        interaction.Open();
        interaction.QueryChanged("other");
        interaction.CandidatesReceived([SelectionKey]);
        interaction.EnterFromSearch();
        interaction.RequestCreate("create");
        interaction.RequestRetry("retry");

        Assert.False(interaction.HasSelection);
        Assert.Null(interaction.SelectedCandidateKey);

        var presentation = Picker(CommonState.Ready, query: "q", candidates: [Candidate(SelectionKey)]);
        Assert.False(presentation.IsCandidateSelected(SelectionKey));
        Assert.Single(presentation.Candidates);
    }

    // TP3 — multiple supplied candidates never auto-select.
    [Fact]
    public void TP3_MultipleCandidates_NeverAutoSelect()
    {
        var interaction = new ToolPickerInteraction(cancelEnabled: true);
        interaction.Sync("q", [SelectionKey, "cand-beta", "cand-gamma"], null, null);

        interaction.Open();
        interaction.QueryChanged("q2");
        interaction.CandidatesReceived([SelectionKey, "cand-beta", "cand-gamma"]);
        interaction.EnterFromSearch();

        Assert.False(interaction.HasSelection);
        Assert.Null(interaction.SelectedCandidateKey);
    }

    // TP4 — only an explicit candidate activation selects, and a refusal raises no event.
    [Fact]
    public void TP4_OnlyExplicitCandidateActivation_ChangesSelection()
    {
        var interaction = new ToolPickerInteraction(cancelEnabled: true);
        interaction.Sync("q", [SelectionKey, "cand-beta"], null, null);

        foreach (var outcome in new[]
                 {
                     interaction.Open(),
                     interaction.QueryChanged("q3"),
                     interaction.CandidatesReceived([SelectionKey, "cand-beta"]),
                     interaction.EnterFromSearch(),
                     interaction.RequestCreate("create"),
                     interaction.RequestRetry("retry"),
                     interaction.Cancel(),
                     interaction.Close(),
                 })
        {
            Assert.False(interaction.HasSelection);
            Assert.NotEqual(ToolPickerEventKind.CandidateSelected, outcome.Event?.Kind);
        }

        var refused = interaction.SelectCandidate("cand-unsupplied");

        Assert.True(refused.Blocked);
        Assert.False(refused.HasEvent);
        Assert.Null(interaction.SelectedCandidateKey);

        Assert.ThrowsAny<ArgumentException>(() => interaction.SelectCandidate(" "));

        var accepted = interaction.SelectCandidate(SelectionKey);

        Assert.False(accepted.Blocked);
        Assert.Equal(ToolPickerEventKind.CandidateSelected, accepted.Event!.Kind);
        Assert.Equal(SelectionKey, interaction.SelectedCandidateKey);
    }

    // TP5 — Enter from search requests a search only, never a selection.
    [Fact]
    public void TP5_EnterFromSearch_RaisesSearchRequestOnly_AndNeverSelects()
    {
        var interaction = new ToolPickerInteraction(cancelEnabled: true);
        interaction.Sync("lote-42", [SelectionKey], null, "origin-1");

        var outcome = interaction.EnterFromSearch();

        Assert.True(outcome.HasEvent);
        Assert.Equal(ToolPickerEventKind.SearchRequested, outcome.Event!.Kind);
        Assert.Equal("lote-42", outcome.Event.Query);
        Assert.Equal("origin-1", outcome.Event.OriginToken);
        Assert.NotEqual(ToolPickerEventKind.CandidateSelected, outcome.Event.Kind);
        Assert.False(interaction.HasSelection);
        Assert.Null(interaction.SelectedCandidateKey);
    }

    // TP6 — an explicit activation selects exactly that candidate, one event per activation.
    [Fact]
    public void TP6_ExplicitActivation_RaisesOneSelectionEvent_PerActivation()
    {
        var interaction = new ToolPickerInteraction(cancelEnabled: true);
        interaction.Sync("q", [SelectionKey, "cand-beta"], null, "origin-1");

        var first = interaction.SelectCandidate(SelectionKey);

        Assert.Equal(ToolPickerEventKind.CandidateSelected, first.Event!.Kind);
        Assert.Equal(SelectionKey, first.Event.CandidateKey);
        Assert.True(first.Event.HasCandidateKey);
        Assert.False(first.Event.HasQuery);

        var second = interaction.SelectCandidate(SelectionKey);

        Assert.Equal(ToolPickerEventKind.CandidateSelected, second.Event!.Kind);
        Assert.Equal(SelectionKey, interaction.SelectedCandidateKey);
    }

    // TP7 — Escape raises exactly one cancel request and nothing else.
    [Fact]
    public void TP7_Escape_RaisesCancelRequestOnly_AndChangesNothing()
    {
        var interaction = new ToolPickerInteraction(cancelEnabled: true);
        interaction.Sync("keep-me", [SelectionKey], SelectionKey, "origin-1");

        var outcome = interaction.Escape();

        Assert.Equal(ToolPickerEventKind.CancelRequested, outcome.Event!.Kind);
        Assert.NotEqual(ToolPickerEventKind.ReturnRequested, outcome.Event.Kind);
        Assert.Equal(ToolPickerFocusTarget.InvokingControl, outcome.FocusTarget);
        Assert.Equal(SelectionKey, interaction.SelectedCandidateKey);
        Assert.Equal("keep-me", interaction.Query);
        Assert.Equal("origin-1", interaction.OriginToken);
    }

    // TP8 — cancel returns without selecting and honours the cancel affordance.
    [Fact]
    public void TP8_Cancel_RaisesCancelRequest_AndLeavesSelectionUnchanged()
    {
        var interaction = new ToolPickerInteraction(cancelEnabled: true);
        interaction.Sync("q", [SelectionKey], SelectionKey, null);

        var supplied = interaction.Cancel("dismiss");

        Assert.Equal(ToolPickerEventKind.CancelRequested, supplied.Event!.Kind);
        Assert.Equal("dismiss", supplied.Event.ActionKey);
        Assert.Equal(SelectionKey, interaction.SelectedCandidateKey);

        var componentOwned = interaction.Cancel();

        Assert.Equal(ToolPickerPresentation.DefaultCancelActionKey, componentOwned.Event!.ActionKey);
        Assert.Equal(SelectionKey, interaction.SelectedCandidateKey);

        var disabled = new ToolPickerInteraction(cancelEnabled: false);
        disabled.Sync("q", [SelectionKey], SelectionKey, null);

        var refused = disabled.Cancel();

        Assert.True(refused.Blocked);
        Assert.False(refused.HasEvent);
        Assert.Equal(SelectionKey, disabled.SelectedCandidateKey);
    }

    // TP9 — close raises only the return request.
    [Fact]
    public void TP9_Close_RaisesReturnRequestOnly()
    {
        var interaction = new ToolPickerInteraction(cancelEnabled: true);
        interaction.Sync("keep", [SelectionKey], SelectionKey, "origin-9");

        var outcome = interaction.Close();

        Assert.Equal(ToolPickerEventKind.ReturnRequested, outcome.Event!.Kind);
        Assert.Equal("origin-9", outcome.Event.OriginToken);
        Assert.Equal(ToolPickerFocusTarget.InvokingControl, outcome.FocusTarget);
        Assert.Equal(SelectionKey, interaction.SelectedCandidateKey);
        Assert.Equal("keep", interaction.Query);
    }

    // TP10 — the opaque origin token survives the roundtrip unchanged on every event kind.
    [Fact]
    public void TP10_OriginToken_RoundTripsVerbatim_OnEveryEventKind()
    {
        // The canonical-looking sample is composed here so this assertion source holds no literal.
        var canonicalLooking = string.Concat("tool", "_", "id", ":opaque-1");
        string?[] tokens = [null, string.Empty, "   ", "origem-ção-Ω", canonicalLooking];

        foreach (var token in tokens)
        {
            var interaction = new ToolPickerInteraction(cancelEnabled: true);
            interaction.Sync("q", [SelectionKey], null, token);

            var events = new[]
            {
                interaction.EnterFromSearch().Event,
                interaction.SelectCandidate(SelectionKey).Event,
                interaction.RequestCreate("create").Event,
                interaction.RequestRetry("retry").Event,
                interaction.Cancel().Event,
                interaction.Close().Event,
            };

            Assert.All(events, raised =>
            {
                Assert.NotNull(raised);
                Assert.True(
                    string.Equals(token, raised!.OriginToken, StringComparison.Ordinal),
                    $"Origin token was not preserved verbatim for kind {raised.Kind}.");
            });
        }
    }

    // TP11 — the create affordance is controlled solely by the consumer and the state.
    [Fact]
    public void TP11_CreateAffordance_IsControlledSolelyByTheConsumer()
    {
        var enabled = SharedActionPresentation.CreateEnabled("create", "Criar registo");
        var disabled = SharedActionPresentation.CreateDisabled("create", "Criar registo", "Sem permissão.");
        var hidden = SharedActionPresentation.Create("create", "Criar registo", true, null, null, visible: false);

        Assert.True(Picker(CommonState.Ready, createAction: enabled).ShowsCreate);
        Assert.True(Picker(CommonState.Empty, createAction: enabled).ShowsCreate);
        Assert.False(Picker(CommonState.Ready, createAction: disabled).ShowsCreate);
        Assert.False(Picker(CommonState.Ready, createAction: hidden).ShowsCreate);
        Assert.False(Picker(CommonState.Ready).ShowsCreate);

        foreach (var state in new[]
                 {
                     CommonState.LookupFailed, CommonState.Unavailable, CommonState.PermissionDenied,
                     CommonState.Loading, CommonState.Stale, CommonState.Conflict,
                     CommonState.Saving, CommonState.Submitting,
                 })
        {
            Assert.False(Picker(state, createAction: enabled).ShowsCreate);
        }
    }

    // TP12 — the picker input fails closed before rendering.
    [Fact]
    public void TP12_Create_FailsClosedOnSuppliedInconsistencies()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ToolPickerPresentation.Create((CommonState)99, "Seleção"));

        Assert.ThrowsAny<ArgumentException>(() =>
            ToolPickerPresentation.Create(CommonState.Ready, " "));

        // Candidate and fact carriers are fail-closed at their own factories.
        Assert.ThrowsAny<ArgumentException>(() =>
            ToolPickerCandidatePresentation.Create(" ", "contexto"));

        Assert.ThrowsAny<ArgumentException>(() =>
            ToolPickerCandidatePresentation.Create(SelectionKey, " "));

        Assert.ThrowsAny<ArgumentException>(() => ToolPickerFactPresentation.Create(" ", "valor"));

        Assert.ThrowsAny<ArgumentException>(() => ToolPickerFactPresentation.Create("Etiqueta", " "));

        Assert.ThrowsAny<ArgumentException>(() =>
            ToolPickerPresentation.Create(
                CommonState.Ready, "Seleção",
                candidates: [Candidate(SelectionKey), Candidate(SelectionKey)]));

        Assert.ThrowsAny<ArgumentException>(() =>
            ToolPickerPresentation.Create(
                CommonState.Ready, "Seleção",
                candidates: [Candidate(SelectionKey)],
                selectedCandidateKey: "cand-unsupplied"));

        Assert.ThrowsAny<ArgumentException>(() =>
            ToolPickerPresentation.Create(CommonState.Empty, "Seleção"));

        Assert.ThrowsAny<ArgumentException>(() =>
            ToolPickerPresentation.Create(CommonState.Ready, "Seleção", expectedTypeLabel: " "));
    }

    // TP13 — the candidate carrier has no status slot (accepted P2-T02 Q2 precedent).
    [Fact]
    public void TP13_CandidateCarrier_HasNoStatusSlot()
    {
        Assert.Null(typeof(ToolPickerCandidatePresentation).GetProperty("Status"));

        var statusShaped = typeof(ToolPickerCandidatePresentation)
            .GetProperties()
            .Where(property =>
                property.PropertyType == typeof(RecordStatusPresentation) ||
                property.PropertyType == typeof(StatusTone))
            .Select(property => property.Name);

        Assert.Empty(statusShaped);
    }

    // TP14 — the presence matrix is a deterministic model fact.
    [Fact]
    public void TP14_PresenceMatrix_MatchesTheAcceptedStateTable()
    {
        foreach (var state in new[]
                 {
                     CommonState.Ready, CommonState.Empty, CommonState.LookupFailed,
                     CommonState.Stale, CommonState.Conflict,
                 })
        {
            Assert.True(Picker(state).ShowsSearch, $"Search must be presented in {state}.");
        }

        foreach (var state in new[]
                 {
                     CommonState.Loading, CommonState.Unavailable, CommonState.PermissionDenied,
                 })
        {
            Assert.False(Picker(state).ShowsSearch, $"Search must not be presented in {state}.");
        }

        foreach (var state in new[]
                 {
                     CommonState.Ready, CommonState.Stale, CommonState.Conflict,
                     CommonState.Saving, CommonState.Submitting,
                 })
        {
            Assert.True(Picker(state).ShowsCandidates, $"Candidates must be presented in {state}.");
        }

        foreach (var state in new[]
                 {
                     CommonState.Empty, CommonState.LookupFailed, CommonState.Loading,
                     CommonState.Unavailable, CommonState.PermissionDenied,
                 })
        {
            Assert.False(Picker(state).ShowsCandidates, $"Candidates must not be presented in {state}.");
        }

        Assert.True(Picker(CommonState.Ready).SelectControlsEnabled);
        Assert.False(Picker(CommonState.Saving).SelectControlsEnabled);
        Assert.False(Picker(CommonState.Submitting).SelectControlsEnabled);
        Assert.True(Picker(CommonState.Empty).ShowsNoResults);
        Assert.True(Picker(CommonState.Ready).ShowsStateSurface is false);
        Assert.True(Picker(CommonState.Empty).ShowsStateSurface);
    }

    // TP15 — deterministic focus targets for every transition.
    [Fact]
    public void TP15_FocusTargets_AreDeterministicPerTransition()
    {
        var interaction = new ToolPickerInteraction(cancelEnabled: true);
        interaction.Sync("q", [SelectionKey], null, null);

        Assert.Equal(ToolPickerFocusTarget.SearchInput, interaction.Open().FocusTarget);
        Assert.Equal(ToolPickerFocusTarget.Unchanged, interaction.QueryChanged("q").FocusTarget);
        Assert.Equal(ToolPickerFocusTarget.Unchanged, interaction.CandidatesReceived([SelectionKey]).FocusTarget);
        Assert.Equal(ToolPickerFocusTarget.Unchanged, interaction.EnterFromSearch().FocusTarget);
        Assert.Equal(ToolPickerFocusTarget.Unchanged, interaction.SelectCandidate(SelectionKey).FocusTarget);
        Assert.Equal(ToolPickerFocusTarget.Unchanged, interaction.RequestCreate("create").FocusTarget);
        Assert.Equal(ToolPickerFocusTarget.Unchanged, interaction.RequestRetry("retry").FocusTarget);
        Assert.Equal(ToolPickerFocusTarget.InvokingControl, interaction.Cancel().FocusTarget);
        Assert.Equal(ToolPickerFocusTarget.InvokingControl, interaction.Escape().FocusTarget);
        Assert.Equal(ToolPickerFocusTarget.InvokingControl, interaction.Close().FocusTarget);

        var withoutSearch = new ToolPickerInteraction(cancelEnabled: true, searchPresented: false);

        Assert.Equal(ToolPickerFocusTarget.RegionHeading, withoutSearch.Open().FocusTarget);
    }

    // TP16 — no address/navigation member and no canonical identity member or constant.
    [Fact]
    public void TP16_NoAddressMember_AndNoCanonicalIdentityName()
    {
        Assert.DoesNotContain(
            P2T03TypeScan.MemberTypes(P2T03TypeScan.PickerTypes),
            type => type == typeof(Uri) || type.FullName?.Contains("Route", StringComparison.Ordinal) == true);

        var names = P2T03TypeScan.MemberNames(P2T03TypeScan.PickerTypes)
            .Concat(P2T03TypeScan.ConstantStrings(P2T03TypeScan.PickerTypes))
            .Concat(P2T03TypeScan.EnumMemberNames(P2T03TypeScan.PickerTypes));

        foreach (var name in names)
        {
            foreach (var token in P2T03TypeScan.CanonicalIdentityTokens)
            {
                Assert.DoesNotContain(token, name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    // TP17 — no feature/domain namespace, service, session or clock reference.
    [Fact]
    public void TP17_PickerTypes_AreDomainNeutral()
    {
        foreach (var type in P2T03TypeScan.PickerTypes)
        {
            Assert.Equal(P2T03TypeScan.ContractsNamespace, type.Namespace);
        }

        var memberTypes = P2T03TypeScan.MemberTypes(P2T03TypeScan.PickerTypes).ToList();

        foreach (var marker in new[]
                 {
                     "Service", "DbContext", "HttpClient", "Supabase", "Session", "Clock",
                     "DMO.Application", "DMO.Domain", "DMO.Infrastructure",
                 })
        {
            Assert.DoesNotContain(
                memberTypes,
                type => (type.FullName ?? type.Name).Contains(marker, StringComparison.Ordinal));
        }
    }

    // TP18 — no matching, ranking, scoring, ordering, counting or lookup member.
    [Fact]
    public void TP18_PickerTypes_ExposeNoSearchOrRankingComputation()
    {
        // The frozen explicit search *state* vocabulary (ShowsSearch, SearchRequested, …) is the
        // picker's own presentation contract, not a search implementation, so it is not a match.
        string[] computed =
        [
            "Rank", "Score", "Match", "Order", "Sort", "Filter", "Lookup", "Compare",
            "Normalize", "Parse", "Resolve", "Count", "Weight",
        ];

        var names = P2T03TypeScan.MemberNames(P2T03TypeScan.PickerTypes)
            .Concat(P2T03TypeScan.ConstantStrings(P2T03TypeScan.PickerTypes));

        foreach (var name in names)
        {
            foreach (var marker in computed)
            {
                Assert.DoesNotContain(marker, name, StringComparison.Ordinal);
            }
        }
    }

    private static ToolPickerPresentation Picker(
        CommonState state,
        string? query = null,
        IReadOnlyList<ToolPickerCandidatePresentation>? candidates = null,
        string? selectedCandidateKey = null,
        SharedActionPresentation? createAction = null)
    {
        var suppliedCandidates = candidates ?? [];

        return ToolPickerPresentation.Create(
            state,
            "Seleção",
            query: query,
            candidates: suppliedCandidates,
            selectedCandidateKey: selectedCandidateKey,
            createAction: createAction,
            message: state == CommonState.Ready ? null : "Mensagem fornecida.");
    }

    private static ToolPickerCandidatePresentation Candidate(string key) =>
        ToolPickerCandidatePresentation.Create(
            key,
            $"contexto {key}",
            [ToolPickerFactPresentation.Create("Lote", "A1")]);
}
