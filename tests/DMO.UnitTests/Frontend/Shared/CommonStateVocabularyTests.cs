using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.UnitTests.Frontend.Shared;

/// <summary>
/// P2-T01 (A3) — the frozen common state vocabulary (freeze §4).
/// Purpose: prove all ten presentation states are representable, carry distinct semantics, and
/// that the mandated distinctions empty / lookup-failed / unavailable / permission-denied hold
/// in the presentation model.
/// Authority: docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md §4;
/// dmo-beta-master/contracts/SHARED_FRONTEND.md "Common state vocabulary".
/// </summary>
public sealed class CommonStateVocabularyTests
{
    [Fact]
    public void Vocabulary_HasExactlyTheTenFrozenStates()
    {
        Assert.Equal(
            new[]
            {
                CommonState.Loading,
                CommonState.Ready,
                CommonState.Empty,
                CommonState.LookupFailed,
                CommonState.Unavailable,
                CommonState.PermissionDenied,
                CommonState.Saving,
                CommonState.Submitting,
                CommonState.Stale,
                CommonState.Conflict,
            },
            CommonStateTraits.All);
    }

    [Fact]
    public void EveryState_HasADistinctCssToken_AndNoStateIsUnrepresented()
    {
        var tokens = CommonStateTraits.All.Select(CommonStateTraits.CssToken).ToArray();

        Assert.All(tokens, token => Assert.False(string.IsNullOrWhiteSpace(token)));
        Assert.Equal(tokens.Length, tokens.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void MatchedDistinctions_Empty_LookupFailed_Unavailable_PermissionDenied_AreFourDistinctStates()
    {
        var four = CommonStateTraits.MutuallyDistinctFromEmpty;

        Assert.Equal(4, four.Count);
        Assert.Equal(4, four.Distinct().Count());

        // Each is its own vocabulary member — never aliased onto another.
        Assert.Contains(CommonState.Empty, four);
        Assert.Contains(CommonState.LookupFailed, four);
        Assert.Contains(CommonState.Unavailable, four);
        Assert.Contains(CommonState.PermissionDenied, four);

        // And each has its own rendering token, so they cannot collapse to one rendering.
        var tokens = four.Select(CommonStateTraits.CssToken).ToArray();
        Assert.Equal(4, tokens.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void BusyStates_AreLoadingSavingSubmitting_Only()
    {
        var busy = CommonStateTraits.All.Where(CommonStateTraits.IsBusy).ToArray();

        Assert.Equal([CommonState.Loading, CommonState.Saving, CommonState.Submitting], busy);
    }

    [Fact]
    public void AssertiveAnnouncement_AppliesToLookupFailedAndConflictOnly()
    {
        var assertive = CommonStateTraits.All.Where(CommonStateTraits.IsAssertive).ToArray();

        Assert.Equal([CommonState.LookupFailed, CommonState.Conflict], assertive);
    }

    [Fact]
    public void AssociatedReason_RequiredForUnavailablePermissionDeniedLookupFailed()
    {
        var required = CommonStateTraits.All.Where(CommonStateTraits.RequiresAssociatedReason).ToArray();

        Assert.Equal(
            [CommonState.LookupFailed, CommonState.Unavailable, CommonState.PermissionDenied],
            required);
    }

    [Fact]
    public void CssToken_UndefinedState_Throws_InsteadOfGuessing()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CommonStateTraits.CssToken((CommonState)999));
    }

    [Fact]
    public void Region_UndefinedState_Throws_InsteadOfRenderingAmbiguously()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CommonStateRegionPresentation.Create((CommonState)999, "texto", "Região"));
    }

    [Fact]
    public void Region_BlankMessage_Throws_SoTheSurfaceIsNeverColourOnly()
    {
        Assert.Throws<ArgumentException>(() =>
            CommonStateRegionPresentation.Create(CommonState.Empty, "   ", "Região"));
    }

    [Fact]
    public void Region_TheFourDistinctSurfaces_CarryTheirOwnTokenMessageAndReason()
    {
        var empty = CommonStateRegionPresentation.Empty("Sem registos.", "Resultados");
        var lookup = CommonStateRegionPresentation.LookupFailed("A consulta falhou.", "Resultados");
        var unavailable = CommonStateRegionPresentation.Unavailable(
            "Serviço indisponível.", "Resultados", "Manutenção em curso.");
        var denied = CommonStateRegionPresentation.PermissionDenied(
            "Acesso negado.", "Resultados", "Sem permissão publicada.");

        var regions = new[] { empty, lookup, unavailable, denied };

        // Never the same rendering: distinct token, distinct message.
        Assert.Equal(4, regions.Select(region => region.StateToken).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(4, regions.Select(region => region.Message).Distinct(StringComparer.Ordinal).Count());

        // The lookup failure is never presented as "no records".
        Assert.NotEqual(empty.Message, lookup.Message);
        Assert.NotEqual(empty.State, lookup.State);
        Assert.False(empty.RequiresAssociatedReason && lookup.RequiresAssociatedReason && empty.State == lookup.State);

        // Unavailable is non-permission; permission-denied is the published access decision.
        Assert.Equal("permission-denied", denied.StateToken);
        Assert.Equal("unavailable", unavailable.StateToken);
        Assert.True(denied.RequiresAssociatedReason);
        Assert.True(unavailable.RequiresAssociatedReason);
        Assert.False(empty.RequiresAssociatedReason);
    }

    [Fact]
    public void Region_WrappingState_IsPreservedWithoutReplacingTheOutcome()
    {
        // Freeze §11: common states may wrap — never replace — a supplied outcome.
        var region = CommonStateRegionPresentation.Create(
            CommonState.Empty,
            "Sem registos.",
            "Resultados",
            wrappingState: CommonState.Loading);

        Assert.True(region.IsWrapped);
        Assert.Equal(CommonState.Empty, region.State);
        Assert.Equal("loading", region.WrappingStateToken);
        Assert.Equal("empty", region.StateToken);
    }

    [Fact]
    public void Region_UndefinedWrappingState_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CommonStateRegionPresentation.Create(
                CommonState.Empty, "Sem registos.", "Resultados", wrappingState: (CommonState)999));
    }

    [Fact]
    public void Region_BusyAndAssertiveTraits_MatchTheVocabulary()
    {
        Assert.True(CommonStateRegionPresentation.Create(CommonState.Loading, "A carregar.", "R").IsBusy);
        Assert.True(CommonStateRegionPresentation.Create(CommonState.Conflict, "Conflito.", "R").IsAssertive);
        Assert.False(CommonStateRegionPresentation.Create(CommonState.Ready, "Pronto.", "R").IsBusy);
        Assert.False(CommonStateRegionPresentation.Create(CommonState.Empty, "Sem registos.", "R").IsAssertive);
    }
}
