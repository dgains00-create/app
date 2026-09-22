using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using DMO.IntegrationTests.Host;
using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.IntegrationTests.Frontend.Shared;

/// <summary>
/// P2-T03 (A5/A6) integration — additive stylesheet, generic assets, architecture boundary and asset
/// delivery.
/// Authority: <c>plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md</c> §1.7, §2.3, §3.6,
/// §4.5, §6, §8.4 and the matrix rows ST1–ST9 (AC-45, AC-46, AC-47, AC-48, AC-49, AC-50, AC-52).
/// Purpose: prove the stylesheet extension is additive, token-only and breakpoint-free (so the
/// accepted P2-T02 static scans stay green), that the two new assets are generic, non-fetching,
/// non-navigating, non-responsive and idempotent, that no canonical domain identity or later
/// workstream concept leaks into any P2-T03 source, that no partial declares a route or a form
/// target, and that the asset helper stayed additive.
/// Preconditions: the real host serves the real static files under test.
/// Required non-effects: no new design token, no parallel stylesheet, no shell modification.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class P2T03StaticAssetTests
{
    private const string P2T03BlockMarker = "P2-T03 (A5/A6)";

    private static readonly string[] AcceptedSelectors =
    [
        ".dmo-state--empty", ".dmo-state--lookup-failed", ".dmo-state--unavailable",
        ".dmo-state--permission-denied", ".dmo-status--neutral", ".dmo-availability--not-applicable",
        ".visually-hidden", ".dmo-table__scroll", ".dmo-table__table", ".dmo-table__row--selected",
        ".dmo-table__action", ".dmo-audit__entry", ".dmo-audit__entry-action",
    ];

    private static readonly Type[] P2T03Types =
    [
        typeof(ToolPickerFactPresentation), typeof(ToolPickerCandidatePresentation),
        typeof(ToolPickerEventKind), typeof(ToolPickerEvent), typeof(ToolPickerFocusTarget),
        typeof(ToolPickerOutcome), typeof(ToolPickerInteraction), typeof(ToolPickerPresentation),
        typeof(ToolSummaryFactPresentation), typeof(ToolSummaryRowPresentation),
        typeof(ToolSummaryRowEventKind), typeof(ToolSummaryRowEvent),
        typeof(MeasurementRowFieldKind), typeof(MeasurementRowFieldOptionPresentation),
        typeof(MeasurementRowFieldPresentation), typeof(MeasurementRowPresentation),
        typeof(MeasurementRowsPresentation), typeof(MeasurementRowsEventKind),
        typeof(MeasurementRowsEvent), typeof(MeasurementRowsFocusKind),
        typeof(MeasurementRowsFocusTarget), typeof(MeasurementRowsOutcome),
        typeof(MeasurementRowsInteraction),
        typeof(DecisionBarActionGroup), typeof(DecisionBarActionPresentation),
        typeof(DecisionBarEventKind), typeof(DecisionBarEvent), typeof(DecisionBarOutcome),
        typeof(DecisionBarInteraction), typeof(DecisionBarPresentation),
    ];

    [Fact]
    public async Task ST1_StylesheetExtensionIsAdditiveTokenOnlyAndBreakpointFree()
    {
        using var factory = new DmoWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/css/dmo-components.css");
        var css = await response.Content.ReadAsStringAsync();
        var tokens = await (await client.GetAsync("/css/dmo-tokens.css")).Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var block = Block(css);
        Assert.False(string.IsNullOrWhiteSpace(block), "The P2-T03 CSS block must exist.");

        var rules = P2T03ProductionScan.WithoutCssComments(block);

        Assert.DoesNotContain("@media", rules, StringComparison.Ordinal);
        Assert.DoesNotContain("@container", rules, StringComparison.Ordinal);
        Assert.DoesNotContain("@supports", rules, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b", rules);

        // No new token is declared and every referenced token is a published one.
        Assert.DoesNotContain(rules.Split('\n').Select(line => line.TrimStart()),
            line => line.StartsWith("--dmo-", StringComparison.Ordinal));

        var referenced = Regex.Matches(rules, @"var\((--dmo-[a-z0-9-]+)\)")
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(referenced);
        Assert.All(referenced, token => Assert.Contains($"{token}:", tokens, StringComparison.Ordinal));

        // Every accepted P2-T01/P2-T02 selector is still present, unchanged.
        Assert.All(AcceptedSelectors, selector => Assert.Contains(selector, css, StringComparison.Ordinal));

        // The P2-T03 selectors exist.
        Assert.Contains(".dmo-picker__candidate", css, StringComparison.Ordinal);
        Assert.Contains(".dmo-summary__fact", css, StringComparison.Ordinal);
        Assert.Contains(".dmo-rows__scroll", css, StringComparison.Ordinal);
        Assert.Contains(".dmo-decision__actions", css, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ST2_NewAssetsAreGenericNonFetchingNonNavigatingAndIdempotent()
    {
        using var factory = new DmoWebApplicationFactory();
        using var client = factory.CreateClient();

        foreach (var path in new[] { "/js/dmo-tool-picker.js", "/js/dmo-measurement-rows.js" })
        {
            var response = await client.GetAsync(path);
            var script = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEmpty(script);

            Assert.All(
                P2T03ProductionScan.ForbiddenScriptFragments,
                fragment => Assert.DoesNotContain(fragment, script, StringComparison.Ordinal));

            Assert.All(
                P2T03ProductionScan.DomainVocabulary,
                word => Assert.DoesNotContain(word, script, StringComparison.OrdinalIgnoreCase));

            foreach (var token in P2T03ProductionScan.CanonicalIdentityTokens)
            {
                Assert.DoesNotContain(token, script, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Each adapter states the same rules as its normative model and is idempotent.
        var picker = await (await client.GetAsync("/js/dmo-tool-picker.js")).Content.ReadAsStringAsync();

        Assert.Contains("if (window.dmoToolPicker)", picker, StringComparison.Ordinal);
        Assert.Contains("data-dmo-select-candidate", picker, StringComparison.Ordinal);
        Assert.Contains("data-dmo-search-input", picker, StringComparison.Ordinal);
        Assert.Contains("dmoFocus", picker, StringComparison.Ordinal);

        // The adapter handles Enter and Escape only: it never references the Tab key, so it cannot
        // swallow focus movement or trap focus.
        Assert.DoesNotContain("'Tab'", picker, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Tab\"", picker, StringComparison.Ordinal);
        Assert.DoesNotContain("event.key === 'Tab'", picker, StringComparison.Ordinal);

        var rows = await (await client.GetAsync("/js/dmo-measurement-rows.js")).Content.ReadAsStringAsync();

        Assert.Contains("if (window.dmoMeasurementRows)", rows, StringComparison.Ordinal);
        Assert.Contains("data-dmo-row", rows, StringComparison.Ordinal);
        Assert.Contains("data-dmo-field", rows, StringComparison.Ordinal);
        Assert.Contains("focusPending", rows, StringComparison.Ordinal);
    }

    [Fact]
    public void ST3_NoCanonicalIdentityLeaksIntoAnyP2T03CarrierPartialAssetOrFixture()
    {
        // Reflection over every P2-T03 contract type.
        var names = P2T03Types
            .SelectMany(type => type.GetMembers(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Select(member => member.Name)
            .Concat(P2T03Types
                .SelectMany(type => type.GetFields(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Where(field => field.FieldType == typeof(string))
                .Select(field => field.GetRawConstantValue() as string ?? field.GetValue(null) as string)
                .Where(value => value is not null)
                .Select(value => value!))
            .Concat(P2T03Types.Where(type => type.IsEnum).SelectMany(Enum.GetNames))
            .ToList();

        foreach (var name in names)
        {
            foreach (var token in P2T03ProductionScan.CanonicalIdentityTokens)
            {
                Assert.DoesNotContain(token, name, StringComparison.OrdinalIgnoreCase);
            }
        }

        // File scan over the production carriers, the partials, the assets and the fixture.
        var scanned = P2T03ProductionScan.ReadAll(
            P2T03ProductionScan.ContractSourcePaths
                .Concat(P2T03ProductionScan.PartialPaths)
                .Concat(P2T03ProductionScan.AssetPaths)
                .Concat(P2T03ProductionScan.FixturePaths));

        foreach (var token in P2T03ProductionScan.CanonicalIdentityTokens)
        {
            Assert.DoesNotContain(token, scanned, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ST4_NoFeatureNamespaceServiceBackendOrClockDependencyExists()
    {
        var scanned = P2T03ProductionScan.ReadAll(
            P2T03ProductionScan.ContractSourcePaths
                .Concat(P2T03ProductionScan.PartialPaths)
                .Concat(P2T03ProductionScan.AssetPaths));

        foreach (var marker in new[]
                 {
                     "using DMO.Application", "using DMO.Domain", "using DMO.Infrastructure",
                     "using DMO.JobOn", "using DMO.Controlo", "using DMO.Peso",
                     "using DMO.Boquilhas", "using DMO.Ferramentas",
                     "DbContext", "HttpClient", "Supabase", "IServiceProvider", "DateTime.Now",
                     "DateTime.UtcNow", "IDestinationRouteRegistry",
                 })
        {
            Assert.DoesNotContain(marker, scanned, StringComparison.Ordinal);
        }

        // The accepted shared-frontend feature-service regression still exists unchanged.
        var accepted = typeof(SharedShellTests).GetMethod(
            "SharedFrontendSource_DoesNotImportFeatureServices",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(accepted);
    }

    [Fact]
    public void ST5_NoRouteOrFormTargetIsDeclaredByAnyNewPartial()
    {
        foreach (var path in P2T03ProductionScan.PartialPaths)
        {
            var source = P2T03ProductionScan.WithoutRazorComments(P2T03ProductionScan.Read(path));

            Assert.DoesNotContain("@page", source, StringComparison.Ordinal);
            Assert.DoesNotContain("MapGet", source, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPost", source, StringComparison.Ordinal);
            Assert.DoesNotContain("href=", source, StringComparison.Ordinal);
            Assert.DoesNotContain("<form", source, StringComparison.Ordinal);
            Assert.DoesNotMatch(@"[\s""']action\s*=", source);
            Assert.DoesNotMatch(@"[\s""']method\s*=", source);
            Assert.DoesNotMatch(@"\blocation\s*\.", source);
            Assert.DoesNotMatch(@"\bwindow\s*\.\s*open", source);
        }
    }

    [Fact]
    public void ST6_NoLaterWorkstreamConceptLeaksIntoAnyP2T03Source()
    {
        var scanned = P2T03ProductionScan.ReadAll(
            P2T03ProductionScan.ContractSourcePaths
                .Concat(P2T03ProductionScan.PartialPaths)
                .Concat(P2T03ProductionScan.AssetPaths)
                .Concat(P2T03ProductionScan.FixturePaths));

        foreach (var token in P2T03ProductionScan.LaterWorkstreamVocabulary)
        {
            Assert.DoesNotContain(token, scanned, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("ProductionContextStrip", scanned, StringComparison.Ordinal);
        Assert.DoesNotContain("SecondNav", scanned, StringComparison.Ordinal);
        Assert.DoesNotContain("CurrentBuild", scanned, StringComparison.Ordinal);
    }

    [Fact]
    public void ST7_NoWidthConditionalRuleOrWidthListenerExists()
    {
        var css = P2T03ProductionScan.WithoutCssComments(
            Block(P2T03ProductionScan.Read("src/DMO.Web/wwwroot/css/dmo-components.css")));

        Assert.DoesNotContain("@media", css, StringComparison.Ordinal);
        Assert.DoesNotContain("@container", css, StringComparison.Ordinal);

        var scripts = P2T03ProductionScan.ReadAll(P2T03ProductionScan.AssetPaths);

        Assert.DoesNotContain("matchMedia", scripts, StringComparison.Ordinal);
        Assert.DoesNotContain("ResizeObserver", scripts, StringComparison.Ordinal);
        Assert.DoesNotContain("addEventListener('resize'", scripts, StringComparison.Ordinal);

        var partials = P2T03ProductionScan.ReadAll(P2T03ProductionScan.PartialPaths);

        Assert.DoesNotContain("matchMedia", partials, StringComparison.Ordinal);
        Assert.DoesNotContain("@media", partials, StringComparison.Ordinal);
    }

    [Fact]
    public void ST8_EveryRequiredControlRendersFromItsDocumentedHook()
    {
        var picker = P2T03ProductionScan.Read("src/DMO.Web/Pages/Shared/Components/_ToolPicker.cshtml");
        var summary = P2T03ProductionScan.Read("src/DMO.Web/Pages/Shared/Components/_ToolSummaryRow.cshtml");
        var rows = P2T03ProductionScan.Read("src/DMO.Web/Pages/Shared/Components/_MeasurementRows.cshtml");
        var bar = P2T03ProductionScan.Read("src/DMO.Web/Pages/Shared/Components/_DecisionBar.cshtml");

        Assert.Contains("data-dmo-picker=\"true\"", picker, StringComparison.Ordinal);
        Assert.Contains("data-dmo-search-input=\"true\"", picker, StringComparison.Ordinal);
        Assert.Contains("data-dmo-select-candidate=", picker, StringComparison.Ordinal);
        Assert.Contains("data-dmo-create=", picker, StringComparison.Ordinal);
        Assert.Contains("data-dmo-cancel=", picker, StringComparison.Ordinal);

        Assert.Contains("data-dmo-summary-row=\"true\"", summary, StringComparison.Ordinal);
        Assert.Contains("data-dmo-summary-facts=\"true\"", summary, StringComparison.Ordinal);
        Assert.Contains("data-dmo-summary-status=\"true\"", summary, StringComparison.Ordinal);
        Assert.Contains("data-dmo-summary-actions=\"true\"", summary, StringComparison.Ordinal);

        Assert.Contains("data-dmo-measurement-rows=\"true\"", rows, StringComparison.Ordinal);
        Assert.Contains("data-dmo-rows-scroll=\"true\"", rows, StringComparison.Ordinal);
        Assert.Contains("data-dmo-remove=", rows, StringComparison.Ordinal);
        Assert.Contains("data-dmo-add=\"true\"", rows, StringComparison.Ordinal);
        Assert.Contains("data-dmo-field=", rows, StringComparison.Ordinal);

        Assert.Contains("data-dmo-decision-bar=\"true\"", bar, StringComparison.Ordinal);
        Assert.Contains("data-dmo-action-key=", bar, StringComparison.Ordinal);
        Assert.Contains("data-dmo-action-group=", bar, StringComparison.Ordinal);

        // No partial relocates a control into a structurally different region on a condition.
        foreach (var partial in new[] { picker, summary, rows, bar })
        {
            Assert.DoesNotContain("overflow-menu", partial, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("d-none", partial, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ST9_AssetHelperStayedAdditive_AndTheShellIsUntouched()
    {
        using var factory = new DmoWebApplicationFactory();

        var rendered = await P2T03ComponentRenderer.RenderSharedComponentAssetsAsync(factory);

        Assert.Equal(1, Count(rendered, "dmo-components.css"));
        Assert.Equal(1, Count(rendered, "dmo-focus.js"));
        Assert.Equal(1, Count(rendered, "dmo-dense-table.js"));
        Assert.Equal(1, Count(rendered, "dmo-tool-picker.js"));
        Assert.Equal(1, Count(rendered, "dmo-measurement-rows.js"));

        Assert.Contains("src=\"/js/dmo-tool-picker.js\"", rendered, StringComparison.Ordinal);
        Assert.Contains("src=\"/js/dmo-measurement-rows.js\"", rendered, StringComparison.Ordinal);
        Assert.Contains("defer", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("dmo-picker__", rendered, StringComparison.Ordinal);

        // The protected shell declares none of the component assets itself.
        foreach (var shell in new[]
                 {
                     "src/DMO.Web/Pages/Shared/_Layout.cshtml",
                     "src/DMO.Web/Pages/Shared/_PublicLayout.cshtml",
                 })
        {
            var source = P2T03ProductionScan.Read(shell);

            Assert.DoesNotContain("dmo-tool-picker.js", source, StringComparison.Ordinal);
            Assert.DoesNotContain("dmo-measurement-rows.js", source, StringComparison.Ordinal);
            Assert.DoesNotContain("dmo-picker", source, StringComparison.Ordinal);
            Assert.DoesNotContain("dmo-decision", source, StringComparison.Ordinal);
        }
    }

    private static string Block(string css)
    {
        var index = css.IndexOf(P2T03BlockMarker, StringComparison.Ordinal);
        if (index < 0)
        {
            return string.Empty;
        }

        var start = css.LastIndexOf("/*", index, StringComparison.Ordinal);

        return start < 0 ? css[index..] : css[start..];
    }

    private static int Count(string value, string fragment) =>
        value.Split(fragment, StringSplitOptions.None).Length - 1;
}
