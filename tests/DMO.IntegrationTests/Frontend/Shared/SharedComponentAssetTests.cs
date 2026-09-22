using System.Net;
using System.Text.RegularExpressions;
using DMO.IntegrationTests.Host;

namespace DMO.IntegrationTests.Frontend.Shared;

/// <summary>
/// P2-T02 (A4) integration — additive stylesheet and generic static assets.
/// Authority: <c>plans/contracts/P2_T02_DENSE_DATA_TABLE_AUDIT_TRAIL_CONTRACT.md</c> §4, §9,
/// §14.4, §14.5 and AC-15, AC-26, AC-29, AC-30. Contract rows covered: S1, S2, S3, S4, S5.
/// Purpose: prove the stylesheet extension is additive, token-only and breakpoint-free, that the
/// two JS adapters are generic, non-fetching and non-navigating, and that the asset include
/// helper emits each asset once.
/// Preconditions: the real host serves the real static files under test.
/// Required non-effects: no new design token, no parallel stylesheet, no shell modification.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class SharedComponentAssetTests
{
    private const string P2T02BlockMarker = "P2-T02 (A4)";

    private static readonly string[] P2T01Selectors =
    [
        ".dmo-state--empty",
        ".dmo-state--lookup-failed",
        ".dmo-state--unavailable",
        ".dmo-state--permission-denied",
        ".dmo-status--neutral",
        ".dmo-availability--not-applicable",
        ".dmo-availability--lookup-failed",
        ".visually-hidden",
    ];

    private static readonly string[] ForbiddenScriptFragments =
    [
        "fetch(", "XMLHttpRequest", "location.", "href", "window.open", "submit(",
    ];

    private static readonly string[] ForbiddenScriptVocabulary =
    [
        "tool", "jobon", "peso", "boquilhas", "controlo", "ferramentas", "armaz", "histórico",
        "tampões", "aprovação",
    ];

    [Fact]
    public async Task S1_ComponentStylesheet_ResolvesAndKeepsTheP2T01Selectors()
    {
        using var factory = new DmoWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/css/dmo-components.css");
        var css = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // The additive P2-T02 selectors exist.
        Assert.Contains(".dmo-table__scroll", css, StringComparison.Ordinal);
        Assert.Contains(".dmo-table__table", css, StringComparison.Ordinal);
        Assert.Contains(".dmo-table__row--selected", css, StringComparison.Ordinal);
        Assert.Contains(".dmo-table__action", css, StringComparison.Ordinal);
        Assert.Contains(".dmo-audit__entry", css, StringComparison.Ordinal);
        Assert.Contains(".dmo-audit__entry-action", css, StringComparison.Ordinal);

        // ... and every accepted P2-T01 selector is still present, unchanged (§13.3).
        Assert.All(P2T01Selectors, selector => Assert.Contains(selector, css, StringComparison.Ordinal));

        // The local horizontal scroll container is the accepted overflow mechanism.
        Assert.Contains("overflow-x: auto", P2T02Block(css), StringComparison.Ordinal);
    }

    [Fact]
    public async Task S2_NewCssContainsNoBreakpointOrContainerRule()
    {
        using var factory = new DmoWebApplicationFactory();
        using var client = factory.CreateClient();

        var css = await (await client.GetAsync("/css/dmo-components.css")).Content.ReadAsStringAsync();
        // Comments are documentation, not rules: the prohibition applies to the CSS rules.
        var block = WithoutComments(P2T02Block(css));

        Assert.False(string.IsNullOrWhiteSpace(block), "The P2-T02 CSS block must exist.");
        Assert.DoesNotContain("@media", block, StringComparison.Ordinal);
        Assert.DoesNotContain("@container", block, StringComparison.Ordinal);
        Assert.DoesNotContain("@supports", block, StringComparison.Ordinal);

        // The protected pre-existing shell breakpoint is untouched by P2-T02.
        var shell = await (await client.GetAsync("/css/dmo-shell.css")).Content.ReadAsStringAsync();
        Assert.Contains("@media (max-width: 62rem)", shell, StringComparison.Ordinal);
    }

    [Fact]
    public async Task S3_NewCssUsesOnlyPublishedTokensAndNoNewPalette()
    {
        using var factory = new DmoWebApplicationFactory();
        using var client = factory.CreateClient();

        var css = await (await client.GetAsync("/css/dmo-components.css")).Content.ReadAsStringAsync();
        var tokens = await (await client.GetAsync("/css/dmo-tokens.css")).Content.ReadAsStringAsync();
        var block = P2T02Block(css);

        var referenced = Regex.Matches(block, @"var\((--dmo-[a-z0-9-]+)\)")
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(referenced);
        Assert.All(
            referenced,
            token => Assert.Contains($"{token}:", tokens, StringComparison.Ordinal));

        // No new token is declared by the new block and no hex palette is introduced.
        var declarations = block
            .Split('\n')
            .Where(line => line.TrimStart().StartsWith("--dmo-", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(declarations);
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b", block);
    }

    [Fact]
    public async Task S4_GenericScripts_ResolveAndAreNonFetchingNonNavigatingAndDomainNeutral()
    {
        using var factory = new DmoWebApplicationFactory();
        using var client = factory.CreateClient();

        foreach (var path in new[] { "/js/dmo-focus.js", "/js/dmo-dense-table.js" })
        {
            var response = await client.GetAsync(path);
            var script = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEmpty(script);

            Assert.All(
                ForbiddenScriptFragments,
                fragment => Assert.DoesNotContain(fragment, script, StringComparison.Ordinal));
            Assert.All(
                ForbiddenScriptVocabulary,
                word => Assert.DoesNotContain(word, script, StringComparison.OrdinalIgnoreCase));
        }

        // The dense-table adapter states the same arbitration rules as the C# model and is
        // idempotent, so loading it twice installs no second listener.
        var tableScript = await (await client.GetAsync("/js/dmo-dense-table.js")).Content.ReadAsStringAsync();
        Assert.Contains("data-dmo-selection-enabled", tableScript, StringComparison.Ordinal);
        Assert.Contains("data-dmo-open-enabled", tableScript, StringComparison.Ordinal);
        Assert.Contains("openWindowRowKey", tableScript, StringComparison.Ordinal);
        Assert.Contains("if (window.dmoDenseTable)", tableScript, StringComparison.Ordinal);

        var focusScript = await (await client.GetAsync("/js/dmo-focus.js")).Content.ReadAsStringAsync();
        Assert.Contains("data-dmo-focus-return", focusScript, StringComparison.Ordinal);
        Assert.Contains("if (window.dmoFocus)", focusScript, StringComparison.Ordinal);
    }

    [Fact]
    public async Task S5_AssetIncludeHelper_EmitsEachAssetTagAtMostOnce()
    {
        using var factory = new DmoWebApplicationFactory();

        var rendered = await P2T02ComponentRenderer.RenderSharedComponentAssetsAsync(factory);

        Assert.Equal(1, Count(rendered, "dmo-components.css"));
        Assert.Equal(1, Count(rendered, "dmo-focus.js"));
        Assert.Equal(1, Count(rendered, "dmo-dense-table.js"));

        Assert.Contains("<link rel=\"stylesheet\"", rendered, StringComparison.Ordinal);
        Assert.Contains("href=\"/css/dmo-components.css", rendered, StringComparison.Ordinal);
        Assert.Contains("src=\"/js/dmo-focus.js\"", rendered, StringComparison.Ordinal);
        Assert.Contains("src=\"/js/dmo-dense-table.js\"", rendered, StringComparison.Ordinal);
        Assert.Contains("defer", rendered, StringComparison.Ordinal);

        // The helper emits asset tags only: it does not render component markup or a shell region.
        Assert.DoesNotContain("dmo-table__", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("dmo-audit__", rendered, StringComparison.Ordinal);
    }

    private static string P2T02Block(string css)
    {
        var marker = css.IndexOf(P2T02BlockMarker, StringComparison.Ordinal);
        if (marker < 0)
        {
            return string.Empty;
        }

        // The banner marker sits inside the block's opening comment, so the block starts at the
        // comment opener that introduces it.
        var start = css.LastIndexOf("/*", marker, StringComparison.Ordinal);

        return start < 0 ? css[marker..] : css[start..];
    }

    private static string WithoutComments(string css) =>
        Regex.Replace(css, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

    private static int Count(string value, string fragment) =>
        value.Split(fragment, StringSplitOptions.None).Length - 1;
}
