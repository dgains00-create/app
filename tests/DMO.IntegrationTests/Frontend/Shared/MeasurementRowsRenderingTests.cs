using System.Text.RegularExpressions;
using DMO.IntegrationTests.Host;
using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.IntegrationTests.Frontend.Shared;

/// <summary>
/// P2-T03 (A6) integration — the real rendered repeated-row mechanics surface.
/// Authority: <c>plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md</c> §3.3, §3.3.4,
/// §3.3.6, §5.3, §6.4, §6.5 and the matrix rows RMR1–RMR8 (AC-28, AC-30 … AC-34, AC-48, AC-49).
/// Purpose: prove in rendered markup that the add control carries its supplied availability with an
/// associated reason, that every row renders its supplied context, labelled fields and a
/// context-qualified remove control, that removal is disabled at the minimum with the rendered
/// supplied reason, that supplied validation display renders verbatim with a supplementary tone,
/// that retained states keep the supplied values read-only, and that no measurement domain token or
/// computed verdict appears anywhere.
/// Preconditions: the real compiled P2-T03 partials rendered through the test host view engine.
/// Required non-effects: no field list, no formula, no tolerance and no nominal value is produced.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class MeasurementRowsRenderingTests
{
    [Fact]
    public async Task RMR1_AddControlCarriesSuppliedAvailabilityWithItsAssociatedReason()
    {
        using var factory = new DmoWebApplicationFactory();

        var enabled = await P2T03ComponentRenderer.RenderRowsAsync(
            factory,
            P2T03Fixtures.Rows(CommonState.Ready, 1, [P2T03Fixtures.Row("row-1")]));

        Assert.Contains("data-dmo-add=\"true\"", enabled, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-add-reason=\"true\"", enabled, StringComparison.Ordinal);

        var disabled = await P2T03ComponentRenderer.RenderRowsAsync(
            factory,
            P2T03Fixtures.Rows(CommonState.Ready, 1, [P2T03Fixtures.Row("row-1")], addEnabled: false));

        Assert.Contains("data-dmo-add=\"true\"", disabled, StringComparison.Ordinal);
        Assert.Contains("aria-disabled=\"true\"", disabled, StringComparison.Ordinal);
        Assert.Contains("data-dmo-add-reason=\"true\"", disabled, StringComparison.Ordinal);
        Assert.Contains("Adição indisponível.", disabled, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"r1-add-reason\"", disabled, StringComparison.Ordinal);
        Assert.Contains("id=\"r1-add-reason\"", disabled, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RMR2_EveryRowRendersItsContext_LabelledFieldsAndContextQualifiedRemoveControl()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderRowsAsync(
            factory,
            P2T03Fixtures.Rows(
                CommonState.Ready, 0,
                [
                    MeasurementRowPresentation.Create(
                        "row-1", "linha um",
                        [
                            MeasurementRowFieldPresentation.Create("campo-a", "Primeiro campo", "10"),
                            MeasurementRowFieldPresentation.Create("campo-b", "Segundo campo", "20"),
                        ]),
                ]));

        Assert.Contains("data-dmo-row=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-row-key=\"row-1\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-row-context=\"linha um\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-row-context-label=\"true\"", html, StringComparison.Ordinal);

        Assert.Contains("data-dmo-field=\"campo-a\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-field=\"campo-b\"", html, StringComparison.Ordinal);
        Assert.Contains("Primeiro campo", html, StringComparison.Ordinal);
        Assert.Contains("Segundo campo", html, StringComparison.Ordinal);
        Assert.Contains("value=\"10\"", html, StringComparison.Ordinal);

        // One remove control per row, qualified by the supplied row context.
        Assert.Equal(1, Count(html, "data-dmo-remove=\"row-1\""));
        Assert.Contains("Remover — linha um", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RMR3_AtTheMinimumEveryRemoveControlIsDisabledWithTheRenderedSuppliedReason()
    {
        using var factory = new DmoWebApplicationFactory();

        var atMinimum = await P2T03ComponentRenderer.RenderRowsAsync(
            factory,
            P2T03Fixtures.Rows(CommonState.Ready, 1, [P2T03Fixtures.Row("row-1")]));

        Assert.Contains("data-dmo-minimum-reached=\"true\"", atMinimum, StringComparison.Ordinal);
        Assert.Contains("disabled", atMinimum, StringComparison.Ordinal);
        Assert.Contains("aria-disabled=\"true\"", atMinimum, StringComparison.Ordinal);
        Assert.Contains("data-dmo-remove-reason=\"row-1\"", atMinimum, StringComparison.Ordinal);
        Assert.Contains(P2T03Fixtures.MinimumReason, atMinimum, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"r1-row-row-1-remove-reason\"", atMinimum, StringComparison.Ordinal);
        Assert.Contains("id=\"r1-row-row-1-remove-reason\"", atMinimum, StringComparison.Ordinal);

        var aboveMinimum = await P2T03ComponentRenderer.RenderRowsAsync(
            factory,
            P2T03Fixtures.Rows(
                CommonState.Ready, 1,
                [P2T03Fixtures.Row("row-1"), P2T03Fixtures.Row("row-2")]));

        Assert.Contains("data-dmo-minimum-reached=\"false\"", aboveMinimum, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-remove-reason=", aboveMinimum, StringComparison.Ordinal);
        Assert.Equal(0, Count(aboveMinimum, "aria-disabled=\"true\""));
    }

    [Fact]
    public async Task RMR4_SuppliedValidationDisplayRendersVerbatim_WithASupplementaryTone()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderRowsAsync(
            factory,
            P2T03Fixtures.Rows(
                CommonState.Ready, 0,
                [P2T03Fixtures.Row("row-1", "Texto de validação fornecido.", StatusTone.Warning)]));

        Assert.Contains("data-dmo-row-validation=\"row-1\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-validation-tone=\"warning\"", html, StringComparison.Ordinal);
        Assert.Contains("Texto de validação fornecido.", html, StringComparison.Ordinal);

        // Nothing beyond the supplied display text is produced.
        Assert.DoesNotContain("data-dmo-validation-tone=\"danger\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RMR5_RetainedStatesKeepSuppliedValuesReadOnly_WithTheAcceptedStateSurface()
    {
        using var factory = new DmoWebApplicationFactory();

        foreach (var state in new[]
                 {
                     CommonState.Saving, CommonState.Submitting, CommonState.Unavailable,
                     CommonState.PermissionDenied,
                 })
        {
            var html = await P2T03ComponentRenderer.RenderRowsAsync(
                factory,
                P2T03Fixtures.Rows(state, 1, [P2T03Fixtures.Row("row-1")]));

            Assert.Contains("data-dmo-row-key=\"row-1\"", html, StringComparison.Ordinal);
            Assert.Contains("value=\"10\"", html, StringComparison.Ordinal);
            Assert.Contains("readonly=\"readonly\"", html, StringComparison.Ordinal);
            Assert.Contains($"dmo-state--{Token(state)}", html, StringComparison.Ordinal);
            Assert.Contains("Mensagem fornecida pelo consumidor.", html, StringComparison.Ordinal);

            // Structural mutation and removal are unavailable in these states.
            Assert.Contains("aria-disabled=\"true\"", html, StringComparison.Ordinal);
            Assert.Contains("data-dmo-remove-reason=\"row-1\"", html, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task RMR6_ComponentCarriesTheDocumentedHooksAndAPoliteAnnouncement()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderRowsAsync(
            factory,
            P2T03Fixtures.Rows(
                CommonState.Ready, 1,
                [
                    MeasurementRowPresentation.Create(
                        "row-1", "linha um",
                        [MeasurementRowFieldPresentation.Create("campo-a", "Primeiro campo", "10")]),
                ]));

        Assert.Contains("data-dmo-measurement-rows=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-rows-scroll=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-rows-announcement=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"polite\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-add=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-remove=\"row-1\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-field=\"campo-a\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-row-key=\"row-1\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RMR7_RenderedMarkupCarriesNoMeasurementDomainToken()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderRowsAsync(
            factory,
            P2T03Fixtures.Rows(
                CommonState.Ready, 1,
                [
                    MeasurementRowPresentation.Create(
                        "row-1", "linha um",
                        [MeasurementRowFieldPresentation.Create("campo-a", "Primeiro campo", "10")]),
                ]));

        foreach (var token in P2T03ProductionScan.DomainVocabulary)
        {
            Assert.DoesNotContain(token, html, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task RMR8_OverWidthRowGridScrollsLocally_InsteadOfReflowing()
    {
        using var factory = new DmoWebApplicationFactory();

        var html = await P2T03ComponentRenderer.RenderRowsAsync(
            factory,
            P2T03Fixtures.Rows(CommonState.Ready, 1, [P2T03Fixtures.Row("row-1")]));

        Assert.Contains("data-dmo-rows-scroll=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("tabindex=\"0\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Linhas de medição\"", html, StringComparison.Ordinal);

        // The component's own local overflow container is the accepted narrow-region mechanism.
        var css = P2T03ProductionScan.Read("src/DMO.Web/wwwroot/css/dmo-components.css");
        var block = P2T03ProductionScan.WithoutCssComments(Block(css));

        Assert.Contains("overflow-x: auto", block, StringComparison.Ordinal);
        Assert.DoesNotContain("@media", block, StringComparison.Ordinal);
        Assert.DoesNotContain("@container", block, StringComparison.Ordinal);
    }

    private static string Block(string css)
    {
        const string marker = "P2-T03 (A5/A6)";
        var index = css.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
        {
            return string.Empty;
        }

        var start = css.LastIndexOf("/*", index, StringComparison.Ordinal);

        return start < 0 ? css[index..] : css[start..];
    }

    private static string Token(CommonState state) => CommonStateTraits.CssToken(state);

    private static int Count(string value, string fragment) =>
        value.Split(fragment, StringSplitOptions.None).Length - 1;
}
