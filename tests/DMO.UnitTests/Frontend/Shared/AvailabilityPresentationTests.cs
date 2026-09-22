using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.UnitTests.Frontend.Shared;

/// <summary>
/// P2-T01 (A3) — <c>AvailabilityState</c> (freeze §11).
/// Purpose: prove the eight availability states stay distinct, <c>not-applicable</c> is not an
/// error, <c>file-missing</c> is not <c>not-generated</c>, and <c>lookup-failed</c> is not
/// no-file/no-record.
/// Authority: docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md §11;
/// dmo-beta-master/contracts/SHARED_FRONTEND.md "AvailabilityState";
/// dmo-beta-master/contracts/DOCUMENTS_AND_FILES.md §5.
/// </summary>
public sealed class AvailabilityPresentationTests
{
    [Fact]
    public void Vocabulary_HasExactlyTheEightFrozenStates()
    {
        Assert.Equal(
            new[]
            {
                AvailabilityState.Available,
                AvailabilityState.NotGenerated,
                AvailabilityState.AwaitingApproval,
                AvailabilityState.WorkspaceUnavailable,
                AvailabilityState.FileMissing,
                AvailabilityState.VersionsAvailable,
                AvailabilityState.NotApplicable,
                AvailabilityState.LookupFailed,
            },
            AvailabilityTraits.All);
    }

    [Fact]
    public void EveryState_HasADistinctToken()
    {
        var tokens = AvailabilityTraits.All.Select(AvailabilityTraits.CssToken).ToArray();

        Assert.Equal(8, tokens.Length);
        Assert.Equal(8, tokens.Distinct(StringComparer.Ordinal).Count());
        Assert.All(tokens, token => Assert.False(string.IsNullOrWhiteSpace(token)));
    }

    [Fact]
    public void NotApplicable_IsNotAnError()
    {
        Assert.False(AvailabilityTraits.IsError(AvailabilityState.NotApplicable));
        Assert.True(AvailabilityTraits.IsNeutral(AvailabilityState.NotApplicable));

        var presentation = AvailabilityPresentation.NotApplicable(
            "Não aplicável a este registo.", "Documento");

        Assert.False(presentation.IsError);
        Assert.True(presentation.IsNeutral);
        Assert.Equal("not-applicable", presentation.StateToken);
    }

    [Fact]
    public void FileMissing_IsNotNotGenerated()
    {
        var missing = AvailabilityPresentation.Create(
            AvailabilityState.FileMissing, "Ficheiro em falta.", "Documento");
        var notGenerated = AvailabilityPresentation.Create(
            AvailabilityState.NotGenerated, "Ainda não gerado.", "Documento");

        Assert.NotEqual(missing.State, notGenerated.State);
        Assert.NotEqual(missing.StateToken, notGenerated.StateToken);
        Assert.False(missing.IsError);
        Assert.False(notGenerated.IsError);
        Assert.True(notGenerated.IsNeutral);
    }

    [Fact]
    public void LookupFailed_IsNeverRenderedAsNoFileOrNoRecord()
    {
        var lookup = AvailabilityPresentation.LookupFailed("A consulta falhou.", "Documento");
        var notGenerated = AvailabilityPresentation.Create(
            AvailabilityState.NotGenerated, "Ainda não gerado.", "Documento");
        var missing = AvailabilityPresentation.Create(
            AvailabilityState.FileMissing, "Ficheiro em falta.", "Documento");

        Assert.True(lookup.IsError);
        Assert.NotEqual(lookup.State, notGenerated.State);
        Assert.NotEqual(lookup.State, missing.State);
        Assert.NotEqual(lookup.StateToken, notGenerated.StateToken);
        Assert.NotEqual(lookup.StateToken, missing.StateToken);
    }

    [Fact]
    public void OnlyLookupFailed_IsAnError()
    {
        var errors = AvailabilityTraits.All.Where(AvailabilityTraits.IsError).ToArray();

        Assert.Equal([AvailabilityState.LookupFailed], errors);
    }

    [Fact]
    public void EightStates_AreMutuallyDistinctPresentations()
    {
        var presentations = AvailabilityTraits.All
            .Select(state => AvailabilityPresentation.Create(state, $"texto-{state}", "Documento"))
            .ToArray();

        Assert.Equal(8, presentations.Length);
        Assert.Equal(8, presentations.Select(item => item.StateToken).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(8, presentations.Select(item => item.Text).Distinct(StringComparer.Ordinal).Count());
        Assert.All(presentations, presentation => Assert.False(string.IsNullOrWhiteSpace(presentation.Text)));
    }

    [Fact]
    public void UndefinedState_Throws_InsteadOfGuessing()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AvailabilityPresentation.Create((AvailabilityState)999, "texto", "Documento"));
        Assert.Throws<ArgumentOutOfRangeException>(() => AvailabilityTraits.CssToken((AvailabilityState)999));
    }

    [Fact]
    public void BlankTextOrRegionLabel_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            AvailabilityPresentation.Create(AvailabilityState.Available, "  ", "Documento"));
        Assert.Throws<ArgumentException>(() =>
            AvailabilityPresentation.Create(AvailabilityState.Available, "Disponível.", "  "));
    }

    [Fact]
    public void Versions_AreOpaqueAndPresentedOnlyWhereTheStateAllowsThem()
    {
        var version = AvailabilityVersionPresentation.Create("v-opaca", "Versão anterior", selected: true);
        var presentation = AvailabilityPresentation.Create(
            AvailabilityState.VersionsAvailable, "Versões disponíveis.", "Documento", versions: [version]);

        Assert.True(presentation.HasVersions);
        Assert.Equal("v-opaca", presentation.Versions[0].Key);
        Assert.True(presentation.Versions[0].Selected);

        // A lookup failure must never present versions: that would read as a no-file/no-record outcome.
        Assert.Throws<ArgumentException>(() => AvailabilityPresentation.Create(
            AvailabilityState.LookupFailed, "A consulta falhou.", "Documento", versions: [version]));
    }

    [Fact]
    public void Version_BlankKeyOrLabel_Throws()
    {
        Assert.Throws<ArgumentException>(() => AvailabilityVersionPresentation.Create("  ", "Versão"));
        Assert.Throws<ArgumentException>(() => AvailabilityVersionPresentation.Create("v1", "  "));
    }

    [Fact]
    public void AvailabilityActions_CarrySuppliedDisabledReasons()
    {
        var disabled = SharedActionPresentation.CreateDisabled("open", "Abrir", "Ficheiro em falta.");
        var presentation = AvailabilityPresentation.Create(
            AvailabilityState.FileMissing, "Ficheiro em falta.", "Documento", actions: [disabled]);

        Assert.True(presentation.HasActions);
        Assert.False(presentation.Actions[0].Enabled);
        Assert.Equal("Ficheiro em falta.", presentation.Actions[0].DisabledReason);
        Assert.True(presentation.Actions[0].RequiresDisabledReason);
    }

    [Fact]
    public void RetryAction_IsSuppliedForLookupFailure()
    {
        var retry = SharedActionPresentation.CreateEnabled("retry", "Tentar novamente");
        var presentation = AvailabilityPresentation.LookupFailed("A consulta falhou.", "Documento", retry);

        Assert.True(presentation.HasActions);
        Assert.Equal("retry", presentation.Actions[0].Key);
        Assert.True(AvailabilityTraits.AllowsRetry(AvailabilityState.LookupFailed));
    }
}
