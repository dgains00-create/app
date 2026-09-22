using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.UnitTests.Frontend.Shared;

/// <summary>
/// P2-T01 (A3) — <c>RecordStatus</c> (freeze §10).
/// Purpose: prove the supplied status text is always rendered, tone is supplementary (never
/// colour-only), an unknown supplied status uses the neutral tone, and status grants no action
/// and infers no lifecycle.
/// Authority: docs/frontend/SHARED_FRONTEND_CONTRACT_FREEZE.md §10;
/// dmo-beta-master/contracts/SHARED_FRONTEND.md "RecordStatus".
/// </summary>
public sealed class RecordStatusPresentationTests
{
    [Fact]
    public void SuppliedText_IsAlwaysPresent()
    {
        var status = RecordStatusPresentation.Create("Aguarda aprovação", StatusTone.Warning);

        Assert.True(status.HasText);
        Assert.Equal("Aguarda aprovação", status.Text);
    }

    [Fact]
    public void BlankText_Throws_TheComponentCannotBeColourOnly()
    {
        Assert.Throws<ArgumentException>(() => RecordStatusPresentation.Create("   "));
        Assert.Throws<ArgumentException>(() => RecordStatusPresentation.Neutral(string.Empty));
    }

    [Fact]
    public void UnknownSuppliedTone_UsesNeutralAndNeverGuesses()
    {
        var status = RecordStatusPresentation.Create("Estado desconhecido", (StatusTone)999);

        Assert.Equal(StatusTone.Neutral, status.Tone);
        Assert.Equal("neutral", status.ToneToken);
        Assert.Equal(StatusTone.Neutral, RecordStatusPresentation.Normalize((StatusTone)987));
    }

    [Fact]
    public void NoSuppliedTone_IsNeutral()
    {
        var status = RecordStatusPresentation.Neutral("Sem tom fornecido");

        Assert.Equal(StatusTone.Neutral, status.Tone);
        Assert.Equal("neutral", status.ToneToken);
        Assert.False(status.HasMarker);
        Assert.False(status.HasAssistiveDescription);
    }

    [Fact]
    public void EveryTone_HasADistinctToken_AndToneIsSupplementary()
    {
        var tones = new[]
        {
            StatusTone.Neutral, StatusTone.Info, StatusTone.Success, StatusTone.Warning, StatusTone.Danger,
        };

        var tokens = tones
            .Select(tone => RecordStatusPresentation.Create("texto", tone).ToneToken)
            .ToArray();

        Assert.Equal(tokens.Length, tokens.Distinct(StringComparer.Ordinal).Count());

        // Text survives regardless of tone: meaning is never carried by tone alone.
        Assert.All(tones, tone => Assert.True(RecordStatusPresentation.Create("texto", tone).HasText));
    }

    [Fact]
    public void StatusGrantsNoActions_AndInfersNoLifecycle()
    {
        // No member at all exposes an action, transition, next-state, approval or lifecycle fact.
        var propertyNames = typeof(RecordStatusPresentation)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain(propertyNames, name =>
            name.Contains("Lifecycle", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Transition", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Approval", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("NextState", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Action", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Enabled", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OptionalMarkerAndAssistiveDescription_AreCarriedWhenSupplied()
    {
        var status = RecordStatusPresentation.Create(
            "Ficheiro em falta", StatusTone.Warning, markerLabel: "!", assistiveDescription: "O ficheiro derivado não existe.");

        Assert.True(status.HasMarker);
        Assert.Equal("!", status.MarkerLabel);
        Assert.True(status.HasAssistiveDescription);
    }

    [Fact]
    public void NoSharedContractTypeReferencesAFeatureNamespaceOrService()
    {
        // Freeze §3 rule 1 / acceptance criterion 4: the shared contracts stay domain-neutral.
        var contractsAssembly = typeof(RecordStatusPresentation).Assembly;
        var contractTypes = new[]
        {
            typeof(CommonState),
            typeof(CommonStateTraits),
            typeof(CommonStateRegionPresentation),
            typeof(StatusTone),
            typeof(RecordStatusPresentation),
            typeof(AvailabilityState),
            typeof(AvailabilityTraits),
            typeof(AvailabilityPresentation),
            typeof(AvailabilityVersionPresentation),
            typeof(SharedActionPresentation),
        };

        var forbiddenNamespaces = new[]
        {
            ".JobOn", ".Controlo", ".Peso", ".Boquilhas", ".Ferramentas", ".Armazem", ".Reparacao",
        };

        foreach (var type in contractTypes)
        {
            Assert.Equal("DMO.Web.Frontend.Shared.Contracts", type.Namespace);
            Assert.All(forbiddenNamespaces, forbidden =>
                Assert.DoesNotContain(forbidden, type.Namespace ?? string.Empty, StringComparison.Ordinal));
        }

        // And no type in the shared contracts namespace pulls in a feature namespace at all.
        var contractNamespaceTypes = contractsAssembly
            .GetTypes()
            .Where(type => type.Namespace == "DMO.Web.Frontend.Shared.Contracts")
            .ToArray();

        Assert.NotEmpty(contractNamespaceTypes);
        Assert.All(contractNamespaceTypes, type =>
            Assert.All(forbiddenNamespaces, forbidden =>
                Assert.DoesNotContain(forbidden, type.FullName ?? string.Empty, StringComparison.Ordinal)));
    }
}
