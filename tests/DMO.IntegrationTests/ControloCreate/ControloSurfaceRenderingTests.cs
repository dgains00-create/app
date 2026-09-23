using System.Net;
using System.Text.RegularExpressions;
using DMO.Application.Access;
using DMO.Domain.Tools;
using DMO.IntegrationTests.JobOn;

namespace DMO.IntegrationTests.ControloCreate;

/// <summary>
/// P2-T05 rendered-surface (<c>R</c>) proofs: the REAL Razor surfaces of the Controlo Create area,
/// rendered by the REAL host over the controlled store, at the canonical fixed desktop composition.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §8.3 (regions R1–R8), §24 (the fixed desktop binding), §26.4 rows
/// LAY2/LAY3 (AC-N1). The region-stable structure and the settings-section order are asserted on the
/// RENDERED pages; the keyboard-reachable local overflow containers and the full heading sets are
/// asserted on the page MARKUP (via <see cref="P2T04ProductionScan"/>), and the breakpoint-free
/// stylesheet is read from the repository.</remarks>
public sealed class ControloSurfaceRenderingTests
{
    private const string CreatePath = "/controlo/create";
    private const string DefinicoesPath = "/controlo/create/definicoes";

    /// <summary>
    /// LAY2 (contract §26.4) — proves AC-N1: the rendered Create page keeps the eight regions in
    /// their structural order (strip → selection → cm-context → inputs → results → actions →
    /// status → documents) for a granted caller opening a production with a determined CM context.
    /// </summary>
    [Fact]
    public async Task LAY2_CreatePageRendersTheRegionsInTheirStructuralOrder()
    {
        var composition = new P2T05TestComposition();
        var tool = composition.SeedTool(ToolType.Cm, "5447T173", "LOTE-LAY2", Processo.Nnpb);
        var jobOn = composition.SeedJobOnWithCmContext(
            "LAY2-TEST", "1000", "B1", tool.ToolId.Value, ToolType.Cm, tool.Reference, tool.Lot);

        using var factory = P2T05TestHost.ForUser(P2T05TestHost.AllGranted(), composition);
        using var client = factory.CreateClient();

        using var response = await P2T05TestHost.GetAsync(client, $"{CreatePath}?jobonId={jobOn.JobOnId.Value}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        AssertInOrder(html,
            "data-dmo-controlo-strip=\"true\"",
            "data-dmo-controlo-region=\"selection\"",
            "data-dmo-controlo-region=\"cm-context\"",
            "data-dmo-controlo-region=\"inputs\"",
            "data-dmo-controlo-region=\"results\"",
            "data-dmo-controlo-region=\"actions\"",
            "data-dmo-controlo-region=\"status\"",
            "data-dmo-controlo-region=\"documents\"");

        // The determined context renders its frozen triple (the region is not empty).
        Assert.Contains("data-dmo-frozen-triple=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("5447T173", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// LAY2 (contract §26.4) — proves AC-N1: the rendered Definições page keeps the FIVE section
    /// titles in their structural order, and the larger-desktop composition is preserved by the
    /// static rule that the linked <c>dmo-controlo.css</c> carries no breakpoint rule.
    /// </summary>
    [Fact]
    public async Task LAY2_DefinicoesPageRendersTheFiveSectionsInOrderAndLinksTheBreakpointFreeStylesheet()
    {
        var composition = new P2T05TestComposition();

        using var factory = P2T05TestHost.ForUser(P2T05TestHost.AllGranted(), composition);
        using var client = factory.CreateClient();

        using var response = await P2T05TestHost.GetAsync(client, DefinicoesPath);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        AssertInOrder(html,
            "Reparadores",
            "Reparador por máquina",
            "Diretório de PDF/documentos",
            "Listas de email",
            "Templates de email");

        // The rendered page links the P2-T05 stylesheet, and that stylesheet is breakpoint-free
        // (LAY1 covers the full static rule; this row proves the rendered page actually links it).
        Assert.Contains("dmo-controlo.css", html, StringComparison.Ordinal);

        var css = P2T04ProductionScan.WithoutCssComments(
            P2T04ProductionScan.Read("src/DMO.Web/wwwroot/css/dmo-controlo.css"));

        Assert.False(string.IsNullOrWhiteSpace(css), "The P2-T05 stylesheet must carry real rules.");
        Assert.DoesNotContain("@media", css, StringComparison.Ordinal);
    }

    /// <summary>
    /// LAY3 (contract §26.4) — proves AC-N1: the results table (Create markup) and the four
    /// settings tables (Definições markup) are wrapped in keyboard-reachable local overflow
    /// containers (<c>role="region"</c> + <c>tabindex="0"</c>), and no required column is hidden:
    /// every results heading is present in full.
    /// </summary>
    [Fact]
    public void LAY3_ResultsAndSettingsTablesUseKeyboardReachableLocalOverflowContainers()
    {
        var createMarkup = P2T04ProductionScan.Read("src/DMO.Web/Pages/Controlo/Create.cshtml");
        var definicoesMarkup = P2T04ProductionScan.Read("src/DMO.Web/Pages/Controlo/Definicoes.cshtml");

        // The results table sits inside the local scroll container, which is keyboard reachable.
        var resultsScroll = Regex.Match(
            createMarkup,
            "<div\\b[^>]*data-dmo-controlo-results-scroll=\"true\"[^>]*>",
            RegexOptions.IgnoreCase);

        Assert.True(resultsScroll.Success, "The results region must carry a local scroll container.");
        Assert.Contains("role=\"region\"", resultsScroll.Value, StringComparison.Ordinal);
        Assert.Contains("tabindex=\"0\"", resultsScroll.Value, StringComparison.Ordinal);

        Assert.True(
            createMarkup.IndexOf("data-dmo-controlo-results-scroll=\"true\"", StringComparison.Ordinal)
            < createMarkup.IndexOf("data-dmo-results-table", StringComparison.Ordinal),
            "The results table must be rendered INSIDE its scroll container.");

        // The four settings tables each sit inside a keyboard-reachable local scroll container, in
        // the same sequence as their regions.
        var settingsScrolls = Regex.Matches(
                definicoesMarkup,
                "<div\\b[^>]*class=\"[^\"]*dmo-controlo__settings-scroll[^\"]*\"[^>]*>",
                RegexOptions.IgnoreCase)
            .Cast<Match>()
            .ToArray();

        Assert.Equal(4, settingsScrolls.Length);
        Assert.All(settingsScrolls, scroll =>
        {
            Assert.Contains("role=\"region\"", scroll.Value, StringComparison.Ordinal);
            Assert.Contains("tabindex=\"0\"", scroll.Value, StringComparison.Ordinal);
        });

        var settingsTables = new[]
        {
            "data-dmo-repairers-table",
            "data-dmo-assignments-table",
            "data-dmo-lists-table",
            "data-dmo-templates-table",
        };

        for (var index = 0; index < settingsTables.Length; index++)
        {
            var tableIndex = definicoesMarkup.IndexOf(settingsTables[index], StringComparison.Ordinal);
            var scrollIndex = definicoesMarkup.IndexOf(settingsScrolls[index].Value, StringComparison.Ordinal);
            var nextScrollIndex = index + 1 < settingsScrolls.Length
                ? definicoesMarkup.IndexOf(settingsScrolls[index + 1].Value, StringComparison.Ordinal)
                : definicoesMarkup.Length;

            Assert.True(
                tableIndex > scrollIndex && tableIndex < nextScrollIndex,
                $"The settings table '{settingsTables[index]}' must be INSIDE its own scroll container.");
        }

        // No required results column is hidden: every heading is present in full text.
        foreach (var heading in new[]
                 {
                     ">Leitura<",
                     ">Peso em água (g)<",
                     ">Capacidade (cm³)<",
                     ">Desvio cm³<",
                     ">Desvio %<",
                     ">Peso do vidro (g)<",
                 })
        {
            Assert.Contains(heading, createMarkup, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// LAY3 (contract §26.4) — proves AC-N1: actions never move between regions — the
    /// <c>actions</c> region appears exactly once in the RENDERED Create page and contains the
    /// decision-bar partial (the <c>data-dmo-decision-bar</c> marker sits between the actions
    /// region and the following status region).
    /// </summary>
    [Fact]
    public async Task LAY3_TheDecisionBarIsRenderedInsideTheActionsRegionOnly()
    {
        var composition = new P2T05TestComposition();

        using var factory = P2T05TestHost.ForUser(P2T05TestHost.AllGranted(), composition);
        using var client = factory.CreateClient();

        using var response = await P2T05TestHost.GetAsync(client, CreatePath);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(1, Count(html, "data-dmo-controlo-region=\"actions\""));
        Assert.Equal(1, Count(html, "data-dmo-decision-bar=\"true\""));

        var actionsIndex = html.IndexOf("data-dmo-controlo-region=\"actions\"", StringComparison.Ordinal);
        var decisionBarIndex = html.IndexOf("data-dmo-decision-bar=\"true\"", StringComparison.Ordinal);
        var statusIndex = html.IndexOf("data-dmo-controlo-region=\"status\"", StringComparison.Ordinal);

        Assert.True(actionsIndex >= 0, "The actions region must be rendered.");
        Assert.True(
            actionsIndex < decisionBarIndex && decisionBarIndex < statusIndex,
            "The decision bar must be rendered INSIDE the actions region, before the status region.");
    }

    // ---- arrangement helpers -------------------------------------------------------------

    /// <summary>Asserts that the supplied fragments appear in the text in the supplied order.</summary>
    private static void AssertInOrder(string text, params string[] fragments)
    {
        var previous = -1;

        foreach (var fragment in fragments)
        {
            var index = text.IndexOf(fragment, StringComparison.Ordinal);

            Assert.True(
                index >= 0,
                $"The rendered page must contain '{fragment}'.");

            Assert.True(
                index > previous,
                $"The rendered page must contain '{fragment}' AFTER the previous region marker.");

            previous = index;
        }
    }

    /// <summary>Counts non-overlapping occurrences of a fragment in rendered markup.</summary>
    private static int Count(string html, string fragment)
    {
        var count = 0;
        var index = 0;

        while ((index = html.IndexOf(fragment, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += fragment.Length;
        }

        return count;
    }
}