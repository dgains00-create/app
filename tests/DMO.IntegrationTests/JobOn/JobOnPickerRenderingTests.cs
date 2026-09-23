using System.Net;
using System.Text.RegularExpressions;
using DMO.Application.Access;
using DMO.Domain.Tools;
using DMO.IntegrationTests.Host;

namespace DMO.IntegrationTests.JobOn;

/// <summary>
/// P2-T04 rendered-picker (<c>R</c>) proofs: the REAL Job On create surface with the accepted P2-T03
/// ToolPicker loaded server-side from the real Tool orchestration.
/// </summary>
/// <remarks>
/// Authority: P2-T04 contract §18.2 rule 7 (the real surface never auto-selects) and §20.7 row ORC9
/// (AC-9): a one-candidate result renders ONE candidate entry carrying its own select control, and
/// <c>aria-selected="true"</c> appears nowhere.
/// </remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class JobOnPickerRenderingTests
{
    /// <summary>The reference shared by the Job On context and the single matching CM Tool.</summary>
    private const string Reference = "5447T173";

    /// <summary>The lot of the single matching CM Tool.</summary>
    private const string Lot = "LOTE-ORC9";

    /// <summary>ORC9 (contract §20.7) — proves AC-9: on the REAL create surface, exactly one matching
    /// Tool renders exactly one candidate entry with its own explicit select control and NO
    /// auto-selection anywhere.</summary>
    [Fact]
    public async Task ORC9_ASingleCandidateRendersOneEntryWithItsOwnSelectControlAndNoAutoSelection()
    {
        var store = new P2T04TestStore();
        var matching = store.SeedTool(ToolType.Cm, Reference, Lot, machines: ["B1", "C2"]);

        // A second CM Tool with a DIFFERENT reference: the contracted criteria (type = CM, reference =
        // the Job On reference) must exclude it, so the one candidate is a real one-candidate result
        // and not a truncated two-candidate one.
        store.SeedTool(ToolType.Cm, "OTHER-REFERENCE", Lot);

        using var factory = P2T04TestHost.ForUser([CreateOnly(), FerramentasOnly()], store);
        using var client = factory.CreateClient();

        using var response = await P2T04TestHost.GetAsync(
            client,
            $"/jobon/create?reference={Reference}&slot=CM");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        // The picker was loaded server-side from the real search: three slot regions, one real search.
        Assert.Equal(3, Count(html, "data-dmo-picker-state=\"ready\""));
        Assert.Equal(1, Count(html, "data-dmo-picker-summary=\"true\""));
        Assert.Contains("1 ferramenta(s) encontradas.", html, StringComparison.Ordinal);

        // Exactly ONE candidate entry, for the single matching Tool.
        Assert.Equal(1, Count(html, "data-dmo-candidate=\"true\""));
        Assert.Contains(Reference, html, StringComparison.Ordinal);
        Assert.DoesNotContain("OTHER-REFERENCE", html, StringComparison.Ordinal);

        // It carries its own explicit select control, bound to that candidate's opaque key.
        Assert.Equal(1, Count(html, "data-dmo-select-candidate="));

        var candidate = CandidateSegment(html);
        var candidateKey = Attribute(candidate, "data-dmo-candidate-key");

        Assert.False(string.IsNullOrWhiteSpace(candidateKey), "The rendered candidate must carry its opaque key.");
        Assert.Contains($"data-dmo-select-candidate=\"{candidateKey}\"", candidate, StringComparison.Ordinal);

        // The candidate is the matching Tool: its supplied facts carry the Tool's own reference and lot.
        // Razor attribute encoding escapes the non-ASCII glyph, so the hook is asserted in its encoded form.
        Assert.Contains("data-dmo-candidate-fact=\"Refer&#xEA;ncia\"", candidate, StringComparison.Ordinal);
        Assert.Contains("data-dmo-candidate-fact=\"Lote\"", candidate, StringComparison.Ordinal);
        Assert.Contains(Reference, candidate, StringComparison.Ordinal);
        Assert.Contains(Lot, candidate, StringComparison.Ordinal);
        Assert.Contains(matching.ToolId.Value.ToString(), html, StringComparison.Ordinal);

        // NO auto-selection, anywhere on the surface: not with one result, not on any slot.
        Assert.Equal(0, Count(html, "aria-selected=\"true\""));
        Assert.DoesNotContain("dmo-picker__candidate--selected", html, StringComparison.Ordinal);
        Assert.DoesNotContain("dmo-table__row--selected", html, StringComparison.Ordinal);
    }

    /// <summary>The Job On Create grant alone.</summary>
    private static ModuleDefinition CreateOnly() =>
        P2T04TestHost.Definition(ModuleCatalog.JobOnCreate, "Job On Create", "job-on", "Job On");

    /// <summary>The contextual Ferramentas grant alone (the calling context's Tool capability).</summary>
    private static ModuleDefinition FerramentasOnly() =>
        P2T04TestHost.Definition(ModuleCatalog.Ferramentas, "Ferramentas", null, "Ferramentas", contextual: true);

    /// <summary>Extracts the rendered candidate entry that carries the candidate hook.</summary>
    private static string CandidateSegment(string html)
    {
        const string marker = "data-dmo-candidate=\"true\"";
        const string end = "</li>";

        var index = html.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(index >= 0, "The rendered surface must carry exactly one candidate entry.");

        var start = html.LastIndexOf('<', index);
        var stop = html.IndexOf(end, index, StringComparison.Ordinal);
        Assert.True(stop > index, "The rendered candidate entry must be closed.");

        return html[start..(stop + end.Length)];
    }

    /// <summary>Reads an attribute value out of a rendered fragment, or the empty string.</summary>
    private static string Attribute(string fragment, string name)
    {
        var match = Regex.Match(fragment, name + "=\"([^\"]*)\"");

        return match.Success ? match.Groups[1].Value : string.Empty;
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
