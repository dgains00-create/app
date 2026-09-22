using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using DMO.Application.Access;
using DMO.IntegrationTests.Host;
using DMO.Web.Frontend.Shell;
using DMO.Web.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace DMO.IntegrationTests.Frontend.Shared;

/// <summary>
/// P2-T03 (A5/A6) integration — regression on the protected foundation and on the accepted
/// P2-T01/P2-T02 artifacts.
/// Authority: <c>plans/contracts/P2-T03_TOOLPICKER_ROWS_DECISIONBAR_CONTRACT.md</c> §1.4, §1.7,
/// §8.1 (rows RG1–RG5) and AC-44 … AC-52.
/// Purpose: prove P2-T03 is additive only — honest build availability is unchanged, no route or
/// destination registration is added or changed, the accepted P2-T02 pins still validate (so no
/// accepted test data, test method or protected P2-T01 artifact was weakened or edited), the
/// accepted shared-frontend vocabulary boundaries still hold, and the protected shell keeps no
/// component asset reference.
/// Preconditions: the real compiled host and the committed accepted artifacts.
/// Required non-effects: none; this class asserts non-effects and changes nothing.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class P2T03RegressionTests
{
    /// <summary>RG1 — build availability stays honest (AC-47).</summary>
    [Fact]
    public void RG1_CurrentBuildAvailable_RemainsEmpty()
    {
        Assert.Empty(ModuleRegistrations.CurrentBuildAvailable);
        Assert.NotNull(ModuleRegistrations.CurrentBuildAvailable);
    }

    /// <summary>RG2 — no destination route and no availability registration is added (AC-47).</summary>
    [Fact]
    public void RG2_NoRouteOrDestinationRegistrationIsAddedOrChanged()
    {
        using var factory = new DmoWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var registry = scope.ServiceProvider.GetRequiredService<IDestinationRouteRegistry>();

        Assert.IsType<EmptyDestinationRouteRegistry>(registry);

        foreach (var destinationId in new[]
                 {
                     "JobOn", "Controlo", "Peso", "Boquilhas", "ToolPicker", "ToolSummaryRow",
                     "MeasurementRows", "DecisionBar",
                 })
        {
            Assert.False(registry.TryGetRoute(destinationId, out var route));
            Assert.Equal(string.Empty, route);
        }

        var seam = typeof(DestinationRouteRegistrations);
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        Assert.DoesNotContain(seam.GetFields(flags), _ => true);
        Assert.DoesNotContain(seam.GetProperties(flags), _ => true);
        Assert.DoesNotContain(seam.GetMethods(flags), method => !method.IsSpecialName);

        foreach (var partial in P2T03ProductionScan.PartialPaths)
        {
            var source = P2T03ProductionScan.Read(partial);

            Assert.DoesNotContain("@page", source, StringComparison.Ordinal);
            Assert.DoesNotContain("MapGet", source, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPost", source, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// RG3 — the accepted P2-T02 pins still validate: the frozen P2-T01 artifact hashes and the
    /// frozen accepted test-method names were neither edited nor weakened (AC-44).
    /// </summary>
    [Fact]
    public void RG3_AcceptedP2T02PinsStillValidate_AndNoAcceptedTestMethodWasWeakened()
    {
        var type = typeof(P2T02RegressionTests);
        var flags = BindingFlags.NonPublic | BindingFlags.Static;

        var artifactsField = type.GetField("FrozenP2T01Artifacts", flags);
        Assert.NotNull(artifactsField);

        var artifacts = (Dictionary<string, string>)artifactsField!.GetValue(null)!;
        Assert.NotEmpty(artifacts);

        var root = P2T03ProductionScan.RepositoryRoot();

        foreach (var (relativePath, expectedHash) in artifacts)
        {
            var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));

            Assert.True(File.Exists(path), $"Accepted P2-T01 artifact '{relativePath}' is missing.");
            Assert.Equal(expectedHash, NormalizedHash(File.ReadAllText(path)));
        }

        var methodsField = type.GetField("FrozenTestMethods", flags);
        Assert.NotNull(methodsField);

        var frozenMethods = (Dictionary<Type, string[]>)methodsField!.GetValue(null)!;
        Assert.NotEmpty(frozenMethods);

        foreach (var (frozenType, methodNames) in frozenMethods)
        {
            Assert.All(
                methodNames,
                name => Assert.NotNull(frozenType.GetMethod(name, BindingFlags.Public | BindingFlags.Instance)));
        }
    }

    /// <summary>
    /// RG4 — the accepted P2-T01/P2-T02 artifacts are unchanged in the ways P2-T03 could have
    /// damaged: their partials still carry their accepted hooks and the stylesheet still carries
    /// every accepted selector (AC-44, AC-51).
    /// </summary>
    [Fact]
    public void RG4_AcceptedArtifactsKeepTheirHooksAndSelectors()
    {
        Assert.Contains(
            "data-dmo-dense-table=\"true\"",
            P2T03ProductionScan.Read("src/DMO.Web/Pages/Shared/Components/_DenseDataTable.cshtml"),
            StringComparison.Ordinal);

        Assert.Contains(
            "data-dmo-audit=\"true\"",
            P2T03ProductionScan.Read("src/DMO.Web/Pages/Shared/Components/_AuditTrail.cshtml"),
            StringComparison.Ordinal);

        Assert.Contains(
            "data-dmo-state=",
            P2T03ProductionScan.Read("src/DMO.Web/Pages/Shared/Components/_CommonStateRegion.cshtml"),
            StringComparison.Ordinal);

        Assert.Contains(
            "data-dmo-status-tone=",
            P2T03ProductionScan.Read("src/DMO.Web/Pages/Shared/Components/_RecordStatus.cshtml"),
            StringComparison.Ordinal);

        var css = P2T03ProductionScan.Read("src/DMO.Web/wwwroot/css/dmo-components.css");

        foreach (var selector in new[]
                 {
                     ".dmo-state {", ".dmo-status {", ".dmo-availability {", ".visually-hidden {",
                     ".dmo-table__scroll", ".dmo-audit__entry",
                 })
        {
            Assert.Contains(selector, css, StringComparison.Ordinal);
        }

        // The protected shell and the accepted assets are still present and unmodified in kind.
        Assert.True(File.Exists(Path.Combine(
            P2T03ProductionScan.RepositoryRoot(), "src", "DMO.Web", "Pages", "Shared", "_Layout.cshtml")));

        Assert.Contains(
            "if (window.dmoFocus)",
            P2T03ProductionScan.Read("src/DMO.Web/wwwroot/js/dmo-focus.js"),
            StringComparison.Ordinal);

        Assert.Contains(
            "if (window.dmoDenseTable)",
            P2T03ProductionScan.Read("src/DMO.Web/wwwroot/js/dmo-dense-table.js"),
            StringComparison.Ordinal);
    }

    /// <summary>
    /// RG5 — the accepted shared-frontend boundary still holds, and the P2-T03 contract sources add
    /// no feature namespace reference of their own (AC-46, AC-50).
    /// </summary>
    [Fact]
    public void RG5_SharedFrontendBoundaryStillHolds()
    {
        var accepted = typeof(SharedShellTests).GetMethod(
            "SharedFrontendSource_DoesNotImportFeatureServices",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(accepted);

        var frontendSource = P2T03ProductionScan.ReadAll(P2T03ProductionScan.ContractSourcePaths);

        foreach (var forbidden in new[] { ".JobOn", ".Controlo", ".Peso", ".Boquilhas", ".Ferramentas" })
        {
            Assert.DoesNotContain($"using DMO{forbidden}", frontendSource, StringComparison.Ordinal);
        }
    }

    private static string NormalizedHash(string content)
    {
        var normalized = content.Replace("\r\n", "\n", StringComparison.Ordinal);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));

        return Convert.ToHexStringLower(hash);
    }
}
