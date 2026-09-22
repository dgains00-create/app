using System.Text.RegularExpressions;

namespace DMO.IntegrationTests.Frontend.Shared;

/// <summary>
/// P2-T03 architecture scan helper: the production paths and token tables used by the static and
/// architecture rows of the accepted test matrix.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §2.3, §8.1 (rows ST1–ST9, RMR7, RDB6) and §1.7.
/// <para>
/// The canonical-identity token table is composed from parts, so this assertion source itself never
/// contains a canonical identity literal. The scan targets the P2-T03 production sources, the four
/// partials, the two assets and the P2-T03 presentation fixture — never this assertion source.
/// </para>
/// </remarks>
internal static class P2T03ProductionScan
{
    private static readonly string[] IdentityPrefixes =
    [
        "tool", "jobon", "job_on", "cm", "mf", "bq", "peso", "boquilhas",
        "movement", "repairer", "pegamentos", "controlo_sheet", "resumo", "production",
    ];

    /// <summary>The canonical identity tokens that may never be declared by a P2-T03 carrier.</summary>
    public static IReadOnlyList<string> CanonicalIdentityTokens { get; } =
        IdentityPrefixes.Select(prefix => string.Concat(prefix, "_", "id")).ToList();

    /// <summary>
    /// The domain vocabulary the two new generic assets must not contain. The bare token
    /// <c>tool</c> is deliberately excluded because the frozen component identity contains it.
    /// </summary>
    public static IReadOnlyList<string> DomainVocabulary { get; } =
    [
        "jobon", "peso", "boquilhas", "controlo", "ferramentas", "armaz", "histórico", "tampões",
        "aprovação", "aprovar", "rejeitar", "reabrir", "irreparável", "pegamentos", "nominal",
        "tolerância", "densidade", "movement", "repairer", "warehouse",
    ];

    /// <summary>The P2-T04 and later concepts that may not leak into any P2-T03 source.</summary>
    public static IReadOnlyList<string> LaterWorkstreamVocabulary { get; } =
    [
        "ProductionContextStrip", "JobOn", "Job On", "jobon", "Controlo", "Peso", "Pegamentos",
        "Folha", "Resumo", "Comparação", "Comparacao", "Boquilhas", "Armazém", "Armazem",
        "História", "Historia", "PDF", "Reparação", "Reparacao", "Tampões", "Tampoes",
    ];

    /// <summary>The fragments the two new generic assets must not contain.</summary>
    public static IReadOnlyList<string> ForbiddenScriptFragments { get; } =
    [
        "fetch(", "XMLHttpRequest", "WebSocket", "location.", "href", "window.open", "submit(",
        "matchMedia", "ResizeObserver", "addEventListener('resize'",
    ];

    /// <summary>The thirty P2-T03 contract source file names (contract Appendix B.1).</summary>
    private static readonly string[] ContractSourceFileNames =
    [
        "ToolPickerFactPresentation.cs", "ToolPickerCandidatePresentation.cs", "ToolPickerEventKind.cs",
        "ToolPickerEvent.cs", "ToolPickerFocusTarget.cs", "ToolPickerOutcome.cs",
        "ToolPickerInteraction.cs", "ToolPickerPresentation.cs",
        "ToolSummaryFactPresentation.cs", "ToolSummaryRowPresentation.cs",
        "ToolSummaryRowEventKind.cs", "ToolSummaryRowEvent.cs",
        "MeasurementRowFieldKind.cs", "MeasurementRowFieldOptionPresentation.cs",
        "MeasurementRowFieldPresentation.cs", "MeasurementRowPresentation.cs",
        "MeasurementRowsPresentation.cs", "MeasurementRowsEventKind.cs", "MeasurementRowsEvent.cs",
        "MeasurementRowsFocusKind.cs", "MeasurementRowsFocusTarget.cs", "MeasurementRowsOutcome.cs",
        "MeasurementRowsInteraction.cs",
        "DecisionBarActionGroup.cs", "DecisionBarActionPresentation.cs", "DecisionBarEventKind.cs",
        "DecisionBarEvent.cs", "DecisionBarOutcome.cs", "DecisionBarInteraction.cs",
        "DecisionBarPresentation.cs",
    ];

    /// <summary>The thirty P2-T03 contract sources (contract Appendix B.1).</summary>
    public static IReadOnlyList<string> ContractSourcePaths { get; } =
        ContractSourceFileNames
            .Select(name => $"src/DMO.Web/Frontend/Shared/Contracts/{name}")
            .ToList();

    /// <summary>The four new P2-T03 partials (contract Appendix B.2).</summary>
    public static IReadOnlyList<string> PartialPaths { get; } =
    [
        "src/DMO.Web/Pages/Shared/Components/_ToolPicker.cshtml",
        "src/DMO.Web/Pages/Shared/Components/_ToolSummaryRow.cshtml",
        "src/DMO.Web/Pages/Shared/Components/_MeasurementRows.cshtml",
        "src/DMO.Web/Pages/Shared/Components/_DecisionBar.cshtml",
    ];

    /// <summary>The two new generic assets (contract Appendix B.4).</summary>
    public static IReadOnlyList<string> AssetPaths { get; } =
    [
        "src/DMO.Web/wwwroot/js/dmo-tool-picker.js",
        "src/DMO.Web/wwwroot/js/dmo-measurement-rows.js",
    ];

    /// <summary>The P2-T03 presentation fixture scanned by the architecture rows.</summary>
    public static IReadOnlyList<string> FixturePaths { get; } =
    [
        "tests/DMO.IntegrationTests/Frontend/Shared/P2T03Fixtures.cs",
    ];

    /// <summary>Reads the supplied repository-relative paths.</summary>
    public static string ReadAll(IEnumerable<string> relativePaths) =>
        string.Join(
            '\n',
            relativePaths.Select(path => File.ReadAllText(
                Path.Combine(RepositoryRoot(), path.Replace('/', Path.DirectorySeparatorChar)))));

    /// <summary>Reads one repository-relative path.</summary>
    public static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(
            RepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>Removes Razor comments so a documentation comment is not mistaken for markup.</summary>
    public static string WithoutRazorComments(string source) =>
        Regex.Replace(source, @"@\*.*?\*@", string.Empty, RegexOptions.Singleline);

    /// <summary>Removes CSS comments so a documentation comment is not mistaken for a rule.</summary>
    public static string WithoutCssComments(string source) =>
        Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

    /// <summary>Finds the repository root by the solution marker.</summary>
    public static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DMO.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
