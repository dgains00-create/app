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
/// P2-T02 (A4) integration — regression on the protected foundation.
/// Authority: <c>plans/contracts/P2_T02_DENSE_DATA_TABLE_AUDIT_TRAIL_CONTRACT.md</c> §10.5,
/// §13.1, §13.3 and AC-27 to AC-31. Contract rows covered: G1, G2, G3, G4.
/// Purpose: prove P2-T02 is additive only — build availability stays honest, no route or
/// availability registration is touched, no protected P2-T01 artifact changed, and no existing
/// test was deleted or renamed.
/// Preconditions: the real compiled host and the committed P2-T01 artifacts.
/// Required non-effects: none; this class asserts non-effects and changes nothing.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class P2T02RegressionTests
{
    /// <summary>
    /// The 10 accepted P2-T01 contract files and 3 accepted P2-T01 partials, pinned by their
    /// normalized content hash (contract §13.3, AC-28, G4). A change here is an A1 §16
    /// contract-change proposal, not a P2-T02 edit.
    /// </summary>
    private static readonly Dictionary<string, string> FrozenP2T01Artifacts = new(StringComparer.Ordinal)
    {
        ["src/DMO.Web/Frontend/Shared/Contracts/CommonState.cs"] = "6fcbb6c5722e55110cb3e24998d542da210e30faf92f7379dbf03ee2a4edca00",
        ["src/DMO.Web/Frontend/Shared/Contracts/CommonStateTraits.cs"] = "734aa5301d1058598749403b21255e7a66e6a4b5d244dc2cf04d4cab53b81ff7",
        ["src/DMO.Web/Frontend/Shared/Contracts/CommonStateRegionPresentation.cs"] = "7eb4c14d884b6d7e26ccd524d4d601f10800aa0eb413eec320735ae04ec244d5",
        ["src/DMO.Web/Frontend/Shared/Contracts/StatusTone.cs"] = "068322db5fac137dea7c7b4fd1da22a9988dec748d8831c9bc64a762b36a92b4",
        ["src/DMO.Web/Frontend/Shared/Contracts/RecordStatusPresentation.cs"] = "7dd4cc06f7a8caba156c032b4552cb7102b330b2a9eee06c72aaa4d44313efb6",
        ["src/DMO.Web/Frontend/Shared/Contracts/AvailabilityState.cs"] = "e6de1bc28d1a794a2259f14756111ebbf1af47e8650d5de9829d45bced187a41",
        ["src/DMO.Web/Frontend/Shared/Contracts/AvailabilityTraits.cs"] = "430fc25c70b3ad9dc7b42819881ded4f8dad32b153809bc366c4552bb250b5f1",
        ["src/DMO.Web/Frontend/Shared/Contracts/AvailabilityPresentation.cs"] = "2979bd8b7595492ba6a893fee83247318cfccf8b4dd8420717b1b44004c06a91",
        ["src/DMO.Web/Frontend/Shared/Contracts/AvailabilityVersionPresentation.cs"] = "de2e685dfd12b4095cc605112bebb1384c9041deff4be8011279848da90e6f71",
        ["src/DMO.Web/Frontend/Shared/Contracts/SharedActionPresentation.cs"] = "92674f51ba8d9dc5d88ab83d2bd44a76669aa82ead99dec2113ccb3c7503e99f",
        ["src/DMO.Web/Pages/Shared/Components/_CommonStateRegion.cshtml"] = "fbf796578aaa6cc36967d14b98a9a138e8c11d426a185283662103c92857d26d",
        ["src/DMO.Web/Pages/Shared/Components/_RecordStatus.cshtml"] = "147403ad555ab80f4202812552957cdf2e9bb85f9e5e2fd09f1fdb7d8299542a",
        ["src/DMO.Web/Pages/Shared/Components/_AvailabilityState.cshtml"] = "cb62da65fbccf79360ecd4092a9fda83ec7c3cf2c05399ab3146f3e467514c61",
    };

    /// <summary>
    /// The accepted P2-T01 test methods and the shared-shell regression methods, pinned by name
    /// (contract §10: no existing test method may be deleted, renamed or weakened; G3).
    /// </summary>
    private static readonly Dictionary<Type, string[]> FrozenTestMethods = new()
    {
        [typeof(SharedStatesRenderingTests)] =
        [
            "Rendering_LiveShellAndNavigationBehaviourIsUnchanged",
            "CommonState_FourDistinctStates_RenderDistinctTextualMarkup",
            "CommonState_SuppliedRegionLabel_IsExposedAsTheAccessibleRegionLabel",
            "CommonState_LookupFailed_AnnouncesAssertively_BusyRegionIsMarkedBusy",
            "CommonState_WrappedOutcome_KeepsBothTokens",
            "CommonState_DisabledAction_ReasonIsProgrammaticallyAssociated",
            "RecordStatus_UnknownStatus_RendersNeutralToneWithVisibleText",
            "RecordStatus_TextIsAlwaysPresent_AndToneIsSupplementary",
            "Availability_EightStates_RenderDistinctTokensAndText",
            "Availability_NotApplicableAndLookupFailed_AreNotStyledAlike",
            "Availability_DisabledAction_ReasonIsProgrammaticallyAssociated",
            "ComponentStylesheet_Resolves_AndIsAdditiveOnlyToNewSelectors",
            "Availability_VersionsAvailable_PresentsSuppliedOpaqueVersions",
        ],
        [typeof(SharedShellTests)] =
        [
            "Shell_RendersIdentityRegionsAndPublishedNavigationProjection",
            "Shell_NoLiveDestinations_RendersOperationalEmptyState",
            "Shell_ProductionOutput_DoesNotContainProvisionalContractMarker",
            "Shell_DeniedAccess_RendersFailClosedStatusWithoutFakeDestinations",
            "Shell_Admin_RendersNoOperationalDestinationLinksOrFixtures",
            "StaticAssetsResolveAndContainDesktopTabletFoundation",
            "SharedFrontendSource_DoesNotImportFeatureServices",
        ],
    };

    /// <summary>G1 — build availability stays honest: no module becomes available (AC-27).</summary>
    [Fact]
    public void G1_CurrentBuildAvailable_RemainsEmpty()
    {
        Assert.Empty(ModuleRegistrations.CurrentBuildAvailable);
        Assert.NotNull(ModuleRegistrations.CurrentBuildAvailable);
    }

    /// <summary>G2 — no destination route and no availability registration is added or changed (AC-27).</summary>
    [Fact]
    public void G2_NoRouteOrDestinationRegistrationIsAddedOrChanged()
    {
        using var factory = new DmoWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var registry = scope.ServiceProvider.GetRequiredService<IDestinationRouteRegistry>();

        Assert.IsType<EmptyDestinationRouteRegistry>(registry);
        foreach (var destinationId in new[] { "JobOn", "Controlo", "Peso", "Boquilhas", "Ferramentas", "DenseDataTable", "AuditTrail" })
        {
            Assert.False(registry.TryGetRoute(destinationId, out var route));
            Assert.Equal(string.Empty, route);
        }

        // The documented registration seam still carries zero live registrations.
        var seam = typeof(DestinationRouteRegistrations);
        Assert.Empty(seam.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Empty(seam.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Empty(seam.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName));

        // P2-T02 ships presentation partials only: no `@page` route directive exists in them.
        var partials = new[]
        {
            "src/DMO.Web/Pages/Shared/Components/_DenseDataTable.cshtml",
            "src/DMO.Web/Pages/Shared/Components/_AuditTrail.cshtml",
            "src/DMO.Web/Pages/Shared/Components/_SharedComponentAssets.cshtml",
        };

        foreach (var partial in partials)
        {
            var source = File.ReadAllText(Path.Combine(RepositoryRoot(), partial));
            Assert.DoesNotContain("@page", source, StringComparison.Ordinal);
            Assert.DoesNotContain("MapGet", source, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPost", source, StringComparison.Ordinal);
        }
    }

    /// <summary>G3 — the accepted P2-T01 and shared-shell test methods still exist (regression R).</summary>
    [Fact]
    public void G3_NoAcceptedTestMethodWasDeletedOrRenamed()
    {
        foreach (var (type, methods) in FrozenTestMethods)
        {
            Assert.All(
                methods,
                name => Assert.NotNull(type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance)));
        }
    }

    /// <summary>G4 — the accepted P2-T01 contract files and partials are unchanged (AC-28).</summary>
    [Fact]
    public void G4_ProtectedP2T01Artifacts_AreUnchanged()
    {
        var root = RepositoryRoot();

        foreach (var (relativePath, expectedHash) in FrozenP2T01Artifacts)
        {
            var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));

            Assert.True(File.Exists(path), $"Protected P2-T01 artifact '{relativePath}' is missing.");
            Assert.Equal(expectedHash, NormalizedHash(File.ReadAllText(path)));
        }
    }

    /// <summary>
    /// G4 (continued) — the additive stylesheet still carries every accepted P2-T01 selector; the
    /// component partials never emit a consumer URL, a form target or a method.
    /// </summary>
    [Fact]
    public void G4_ProtectedP2T01SelectorsRemainAndP2T02PartialsConstructNoUrl()
    {
        var root = RepositoryRoot();
        var css = File.ReadAllText(Path.Combine(root, "src", "DMO.Web", "wwwroot", "css", "dmo-components.css"));

        foreach (var selector in new[]
                 {
                     ".dmo-state {", ".dmo-state--empty", ".dmo-state--lookup-failed", ".dmo-state--stale",
                     ".dmo-status {", ".dmo-status--neutral", ".dmo-availability {", ".visually-hidden {",
                 })
        {
            Assert.Contains(selector, css, StringComparison.Ordinal);
        }

        foreach (var partial in new[] { "_DenseDataTable.cshtml", "_AuditTrail.cshtml" })
        {
            // Razor comments are documentation; the scan targets the emitted markup and code.
            var source = WithoutRazorComments(File.ReadAllText(
                Path.Combine(root, "src", "DMO.Web", "Pages", "Shared", "Components", partial)));

            // The component markup emits no consumer URL, no form target and no method: only the
            // `data-dmo-action` hook is present, which is not an `action` attribute.
            Assert.DoesNotContain("href=", source, StringComparison.Ordinal);
            Assert.DoesNotMatch(@"[\s""']action\s*=", source);
            Assert.DoesNotMatch(@"[\s""']method\s*=", source);
            Assert.DoesNotContain("<form", source, StringComparison.Ordinal);
            Assert.DoesNotMatch(@"\blocation\s*\.", source);
            Assert.DoesNotMatch(@"\bwindow\s*\.\s*open", source);

            // P2-T02 introduces no P2-T03 concept in the shared frontend.
            foreach (var forbidden in new[]
                     {
                         "ToolPicker", "ToolSummaryRow", "MeasurementRows", "DecisionBar",
                         "ProductionContextStrip",
                     })
            {
                Assert.DoesNotContain(forbidden, source, StringComparison.Ordinal);
            }
        }
    }

    private static string WithoutRazorComments(string source) =>
        System.Text.RegularExpressions.Regex.Replace(
            source, @"@\*.*?\*@", string.Empty, System.Text.RegularExpressions.RegexOptions.Singleline);

    private static string NormalizedHash(string content)    {
        var normalized = content.Replace("\r\n", "\n", StringComparison.Ordinal);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));

        return Convert.ToHexStringLower(hash);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DMO.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
