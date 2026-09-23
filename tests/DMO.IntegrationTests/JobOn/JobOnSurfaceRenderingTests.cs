using System.Net;
using System.Text.RegularExpressions;
using DMO.Application.Access;
using DMO.Domain.JobOn;
using DMO.Domain.Tools;
using DMO.IntegrationTests.Host;

namespace DMO.IntegrationTests.JobOn;

/// <summary>
/// P2-T04 rendered-surface (<c>R</c>) proofs: the REAL Razor surfaces of the Job On area, rendered by
/// the REAL host over the controlled store.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §13.2 route 7, §11.2, §18 and §20.3 row CTX18 (AC-69): the frozen
/// triple of an existing context is presented as read-only historical data and is never exposed as an
/// ordinary editable field.
/// </remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class JobOnSurfaceRenderingTests
{
    /// <summary>The frozen Tool reference of the arranged context.</summary>
    private const string FrozenReference = "5447T173";

    /// <summary>The frozen Tool lot of the arranged context.</summary>
    private const string FrozenLot = "LOTE-CTX18";

    /// <summary>The Job On's own production reference (deliberately different from the Tool
    /// reference, so an editable fact can never be confused with the frozen triple).</summary>
    private const string JobOnReference = "REF-CTX18";

    /// <summary>CTX18 (contract §20.3) — proves AC-69: the Job On edit surface renders the frozen
    /// triple inside a read-only region and carries NO form control bound to those frozen values.</summary>
    [Fact]
    public async Task CTX18_EditSurfaceRendersTheFrozenTripleReadOnlyWithNoBoundFormControl()
    {
        var store = new P2T04TestStore();
        var tool = store.SeedTool(ToolType.Cm, FrozenReference, FrozenLot);
        var occurrence = store.SeedJobOn(
            JobOnReference,
            "1000",
            contexts:
            [
                new ToolContext(
                    ToolContextType.Cm,
                    Guid.NewGuid(),
                    JobOnId.New(),
                    tool.ToolId,
                    new ToolContextSnapshot(ToolType.Cm, FrozenReference, FrozenLot)),
            ]);

        using var factory = P2T04TestHost.ForUser([CreateOnly()], store);
        using var client = factory.CreateClient();

        using var response = await P2T04TestHost.GetAsync(client, $"/jobon/{occurrence.JobOnId.Value}/edit");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        // The frozen triple is rendered, in its own read-only region, as the context's frozen values.
        Assert.Contains("data-dmo-jobon-surface=\"edit\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-jobon-slot=\"CM\"", html, StringComparison.Ordinal);
        Assert.Contains("data-dmo-frozen-triple=\"true\"", html, StringComparison.Ordinal);

        var frozenRegion = Regex.Match(
            html,
            "<(?<tag>[a-zA-Z]+)[^>]*data-dmo-frozen-triple=\"true\"[^>]*>\\s*(?<value>.*?)\\s*</\\k<tag>>",
            RegexOptions.Singleline);

        Assert.True(frozenRegion.Success, "The rendered edit surface must carry a frozen-triple region.");

        // The region is a read-only element (never an input/select/textarea) and it carries the frozen
        // type, reference and lot verbatim.
        Assert.Equal("p", frozenRegion.Groups["tag"].Value, ignoreCase: true);
        Assert.Equal($"CM {FrozenReference} lote {FrozenLot}", frozenRegion.Groups["value"].Value);

        // The frozen values appear NOWHERE else on the surface: in particular no editable control
        // carries them.
        Assert.Equal(1, Count(html, FrozenReference));
        Assert.Equal(1, Count(html, FrozenLot));

        var controls = Regex
            .Matches(html, "<(input|select|textarea)\\b[^>]*>", RegexOptions.IgnoreCase)
            .Select(match => match.Value)
            .ToArray();

        // The surface does bind its four editable facts, so the exclusion below is meaningful.
        Assert.Contains(
            controls,
            control => control.Contains("name=\"reference\"", StringComparison.Ordinal)
                && control.Contains($"value=\"{JobOnReference}\"", StringComparison.Ordinal));

        Assert.All(
            controls,
            control =>
            {
                Assert.DoesNotContain(FrozenReference, control, StringComparison.Ordinal);
                Assert.DoesNotContain(FrozenLot, control, StringComparison.Ordinal);
            });

        // The frozen reference is not offered as a field name either.
        Assert.DoesNotContain("name=\"frozenReference\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"frozenLot\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"toolReference\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"toolLot\"", html, StringComparison.Ordinal);
    }

    /// <summary>The Job On Create grant alone.</summary>
    private static ModuleDefinition CreateOnly() =>
        P2T04TestHost.Definition(ModuleCatalog.JobOnCreate, "Job On Create", "job-on", "Job On");

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
