using System.Reflection;
using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.UnitTests.Frontend.Shared;

/// <summary>
/// P2-T02 (A4) unit tests — the <c>AuditTrail</c> presentation contract.
/// Authority: <c>plans/contracts/P2-T02_DENSE_DATA_TABLE_AUDIT_TRAIL_CONTRACT.md</c> §6 and
/// AC-18 to AC-26. Contract rows covered: U14, U15, U16, U17, U18, U19, U20.
/// Purpose: prove supplied ordering is preserved with no chronology inference, that attribution
/// is never synthesized, that the carrier has no entry-level status (Q2 resolved ABSENT), that
/// the surface is read-only, and that the boundary is domain-neutral.
/// Preconditions: none beyond the compiled P2-T02 contract assembly.
/// Required non-effects: no P2-T01 type is modified and no session/clock/service is reachable.
/// </summary>
public sealed class AuditTrailContractTests
{
    private static readonly Type[] AuditTrailTypes =
    [
        typeof(AuditEntryPresentation),
        typeof(AuditTrailPresentation),
    ];

    private static readonly string[] ForbiddenDependencies =
    [
        "DbContext", "HttpClient", "Supabase", "ICurrentAccount", "TimeProvider", "DateTime.Now",
        "DateTime.UtcNow", "HttpContext", "IServiceProvider", "ILogger", "IStringLocalizer",
        "CultureInfo", "using DMO.",
        "ToolPicker", "ToolSummaryRow", "MeasurementRows", "DecisionBar", "ProductionContextStrip",
        "JobOn", "Boquilhas", "Ferramentas", "Peso", "Controlo", "tool_id", "jobon_id", "peso_id",
    ];

    private static readonly string[] OrderingMemberFragments =
    [
        "order", "sort", "newest", "oldest", "chronolog", "sequence", "reorder", "direction",
    ];

    private static readonly string[] MutationMemberFragments =
    [
        "edit", "delete", "restore", "remove", "mutat", "correct", "undo", "purge", "revise",
    ];

    /// <summary>U14 — entries render in exactly the supplied sequence, for any supplied sequence (AC-18).</summary>
    [Fact]
    public void Entries_PreserveExactlyTheSuppliedSequence()
    {
        var sequences = new[]
        {
            new[] { "e1", "e2", "e3" },
            new[] { "e3", "e2", "e1" },
            new[] { "e2", "e3", "e1" },
            new[] { "e1" },
            Array.Empty<string>(),
        };

        foreach (var sequence in sequences)
        {
            var entries = sequence.Select(key => AuditEntryPresentation.Create(key, $"Ação {key}")).ToList();

            var presentation = AuditTrailPresentation.Ready("Histórico", entries);

            Assert.Equal(sequence, presentation.Entries.Select(entry => entry.Key));
            Assert.Equal(sequence.Length, presentation.Entries.Count);

            // Both the newest-first and the oldest-first reading are supplied orders here and each
            // one is preserved verbatim: the component asserts no chronological direction.
            Assert.Equal(sequence.ToList(), presentation.Entries.Select(entry => entry.Key).ToList());
        }
    }

    /// <summary>U14 — no ordering or chronology metadata exists to change the supplied sequence (AC-18).</summary>
    [Fact]
    public void AuditTypes_ExposeNoOrderingOrChronologyMember()
    {
        foreach (var type in AuditTrailTypes)
        {
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                Assert.DoesNotContain(
                    OrderingMemberFragments,
                    fragment => member.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    /// <summary>U15 — action text is mandatory and a missing attribution produces no substitute text (AC-20).</summary>
    [Fact]
    public void Attribution_IsNeverSynthesized()
    {
        Assert.Throws<ArgumentException>(() => AuditEntryPresentation.Create("e1", " "));

        var anonymous = AuditEntryPresentation.Create("e1", "Ação registada");

        Assert.False(anonymous.HasActor);
        Assert.False(anonymous.HasAttribution);
        Assert.Null(anonymous.AttributionText);
        Assert.False(anonymous.HasTimestampText);
        Assert.False(anonymous.HasTimestampValue);

        var unavailable = AuditEntryPresentation.Create(
            "e2", "Ação registada", actorUnavailableText: "Atribuição indisponível");

        Assert.False(unavailable.HasActor);
        Assert.True(unavailable.HasAttribution);
        Assert.Equal("Atribuição indisponível", unavailable.AttributionText);

        // The explicit unavailable text is rendered only when the actor is absent.
        var attributed = AuditEntryPresentation.Create(
            "e3", "Ação registada", actor: "Ana", actorUnavailableText: "Atribuição indisponível");

        Assert.True(attributed.HasActor);
        Assert.Equal("Ana", attributed.AttributionText);
    }

    /// <summary>U16 — no audit type reaches a session, account, clock, service or culture (AC-25).</summary>
    [Fact]
    public void AuditTypes_ReferenceNoSessionClockServiceOrCulture()
    {
        var source = P2T02AuditSource();

        Assert.DoesNotContain(
            source.Split('\n'),
            line => line.TrimStart().StartsWith("using ", StringComparison.Ordinal));

        Assert.All(
            ForbiddenDependencies,
            forbidden => Assert.DoesNotContain(forbidden, source, StringComparison.Ordinal));

        foreach (var type in AuditTrailTypes)
        {
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                Assert.DoesNotContain("Format", member.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Parse", member.Name, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>U16 — the audit carrier depends only on framework and shared contract types (AC-25, AC-26).</summary>
    [Fact]
    public void AuditTypes_ReferenceOnlyFrameworkAndSharedContractTypes()
    {
        foreach (var type in AuditTrailTypes)
        {
            var referenced = type.GetProperties().Select(property => property.PropertyType)
                .Concat(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType)
                        .Append(method.ReturnType)));

            foreach (var reference in referenced)
            {
                var candidate = Nullable.GetUnderlyingType(reference) ?? reference;
                if (candidate.IsGenericType)
                {
                    candidate = candidate.GetGenericArguments()[0];
                }

                if (candidate.Namespace is null)
                {
                    continue;
                }

                Assert.True(
                    candidate.Namespace.StartsWith("System", StringComparison.Ordinal) ||
                    candidate.Namespace == "DMO.Web.Frontend.Shared.Contracts",
                    $"Unexpected dependency '{candidate.FullName}' on '{type.Name}'.");
            }
        }
    }

    /// <summary>U17 — the supplied display text is verbatim and the semantic value is machine-only (AC-19).</summary>
    [Fact]
    public void Timestamp_DisplayTextIsVerbatimAndTheSemanticValueIsMachineOnly()
    {
        var semantic = new DateTimeOffset(2026, 3, 14, 9, 26, 53, TimeSpan.Zero);
        var entry = AuditEntryPresentation.Create(
            "e1",
            "Ação registada",
            timestampText: "14/03/2026 às 09:26 (hora local)",
            timestampValue: semantic);

        // The supplied display text is preserved character for character.
        Assert.Equal("14/03/2026 às 09:26 (hora local)", entry.TimestampText);
        Assert.True(entry.HasTimestampText);
        Assert.Equal(semantic, entry.TimestampValue);

        // The semantic value never becomes the display text.
        Assert.NotEqual(entry.TimestampValue!.Value.ToString("O"), entry.TimestampText);

        // A semantic value without a supplied display value is not turned into display text.
        var machineOnly = AuditEntryPresentation.Create("e2", "Ação registada", timestampValue: semantic);
        Assert.False(machineOnly.HasTimestampText);
        Assert.True(machineOnly.HasTimestampValue);
        Assert.Null(machineOnly.TimestampText);
    }

    /// <summary>U18 — detail/before/after are rendered only when supplied and no diff is computed (AC-21).</summary>
    [Fact]
    public void DetailFacts_ArePresentOnlyWhenSupplied()
    {
        var bare = AuditEntryPresentation.Create("e1", "Ação registada");

        Assert.False(bare.HasDetail);
        Assert.False(bare.HasBeforeDetail);
        Assert.False(bare.HasAfterDetail);

        var detailed = AuditEntryPresentation.Create(
            "e2", "Ação registada", detail: "Detalhe", beforeDetail: "Antes", afterDetail: "Depois");

        Assert.True(detailed.HasDetail);
        Assert.True(detailed.HasBeforeDetail);
        Assert.True(detailed.HasAfterDetail);
        Assert.Equal("Antes", detailed.BeforeDetail);
        Assert.Equal("Depois", detailed.AfterDetail);

        foreach (var member in typeof(AuditEntryPresentation).GetMembers())
        {
            Assert.DoesNotContain("Diff", member.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Compare", member.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>U19 — the only interactive element per entry is the supplied generic detail action (AC-24).</summary>
    [Fact]
    public void Entry_IsReadOnlyApartFromTheSuppliedDetailAction()
    {
        var bare = AuditEntryPresentation.Create("e1", "Ação registada");
        Assert.False(bare.HasDetailAction);
        Assert.False(bare.IsInteractive);

        var enabled = AuditEntryPresentation.Create(
            "e2", "Ação registada", detailAction: SharedActionPresentation.CreateEnabled("detail", "Ver detalhe"));
        Assert.True(enabled.HasDetailAction);
        Assert.True(enabled.IsInteractive);

        var disabled = AuditEntryPresentation.Create(
            "e3", "Ação registada", detailAction: SharedActionPresentation.CreateDisabled("detail", "Ver detalhe", "Sem permissão."));
        Assert.True(disabled.HasDetailAction);

        var hidden = AuditEntryPresentation.Create(
            "e4", "Ação registada",
            detailAction: SharedActionPresentation.Create("detail", "Ver detalhe", enabled: true, visible: false));
        Assert.False(hidden.HasDetailAction);
        Assert.False(hidden.IsInteractive);

        var actionCarriers = typeof(AuditEntryPresentation).GetProperties()
            .Where(property => property.PropertyType == typeof(SharedActionPresentation))
            .Select(property => property.Name);

        Assert.Equal(new[] { nameof(AuditEntryPresentation.DetailAction) }, actionCarriers);

        foreach (var member in typeof(AuditEntryPresentation).GetMembers())
        {
            Assert.DoesNotContain(
                MutationMemberFragments,
                fragment => member.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// U19 / Q2 — the entry carries no entry-level status and no equivalent renamed field
    /// (contract §6.2, Q2 resolved ABSENT).
    /// </summary>
    [Fact]
    public void Entry_HasNoEntryLevelRecordStatus()
    {
        var entry = AuditEntryPresentation.Create("e1", "Ação registada", actor: "Ana");

        Assert.Null(typeof(AuditEntryPresentation).GetProperty("Status"));

        foreach (var property in typeof(AuditEntryPresentation).GetProperties())
        {
            Assert.NotEqual(typeof(RecordStatusPresentation), property.PropertyType);
            Assert.NotEqual(typeof(StatusTone), property.PropertyType);
            Assert.DoesNotContain("status", property.Name, StringComparison.OrdinalIgnoreCase);
        }

        // The region-level carrier is equally status-free for entries.
        Assert.Null(typeof(AuditTrailPresentation).GetProperty("Status"));
        Assert.Equal("e1", entry.Key);
    }

    /// <summary>U20 — no audit type references a domain namespace, canonical identity or table concept (AC-26).</summary>
    [Fact]
    public void AuditTypes_AreDomainNeutral()
    {
        foreach (var type in AuditTrailTypes)
        {
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                Assert.DoesNotContain("DenseTable", member.Name, StringComparison.Ordinal);
            }
        }

        // The audit trail consumes the accepted P2-T01 state vocabulary rather than its own.
        var empty = AuditTrailPresentation.Empty("Histórico", "Sem histórico registado.");

        Assert.Equal(CommonState.Empty, empty.State);
        Assert.Equal("empty", empty.StateToken);
        Assert.False(empty.IsAssertive);
        Assert.True(empty.RendersStateSurfaceOnly);
        Assert.Empty(empty.Entries);

        var failed = AuditTrailPresentation.LookupFailed("Histórico", "A consulta falhou.");

        Assert.Equal("lookup-failed", failed.StateToken);
        Assert.NotEqual(empty.StateToken, failed.StateToken);

        var region = failed.ToStateRegion();
        Assert.Equal(CommonState.LookupFailed, region.State);
        Assert.Equal("Histórico", region.RegionLabel);
    }

    /// <summary>U20 — state delegation keeps unavailable/permission-denied free of entry markup (AC-23).</summary>
    [Fact]
    public void StateDelegation_NeverRendersEntriesForProtectedStates()
    {
        var entries = new[] { AuditEntryPresentation.Create("e1", "Ação registada") };

        var unavailable = AuditTrailPresentation.Unavailable("Histórico", "Serviço indisponível.", "Manutenção.");
        var denied = AuditTrailPresentation.PermissionDenied("Histórico", "Acesso negado.", "Sem permissão.");
        var loading = AuditTrailPresentation.Loading("Histórico", "A carregar o histórico.");
        var conflict = AuditTrailPresentation.Create(CommonState.Conflict, "Histórico", entries, message: "Conflito.");

        Assert.True(unavailable.RendersStateSurfaceOnly);
        Assert.True(denied.RendersStateSurfaceOnly);
        Assert.True(loading.RendersStateSurfaceOnly);
        Assert.True(conflict.RendersStateSurfaceOnly);

        Assert.Empty(unavailable.Entries);
        Assert.Empty(denied.Entries);
        Assert.Empty(loading.Entries);
        Assert.True(loading.IsBusy);

        // The stale state retains the supplied entries alongside the accepted stale surface.
        var stale = AuditTrailPresentation.Stale("Histórico", "Informação desatualizada.", entries);
        Assert.True(stale.RendersEntries);
        Assert.True(stale.ShowsStateSurface);
        Assert.Single(stale.Entries);
        Assert.Equal(new[] { "e1" }, stale.Entries.Select(entry => entry.Key));
    }

    /// <summary>U20 — the region carrier fails closed on a blank label and a missing state message (§6.6).</summary>
    [Fact]
    public void RegionCreate_FailsClosedOnBlankLabelAndMissingNonReadyMessage()
    {
        Assert.Throws<ArgumentException>(() => AuditTrailPresentation.Create(CommonState.Ready, " ", []));
        Assert.ThrowsAny<ArgumentException>(() => AuditTrailPresentation.Create(CommonState.Empty, "Histórico", []));
        Assert.Throws<ArgumentException>(() => AuditTrailPresentation.Create(CommonState.Empty, "Histórico", [], message: " "));
        Assert.ThrowsAny<ArgumentException>(() => AuditTrailPresentation.Create(CommonState.LookupFailed, "Histórico", []));
        Assert.Throws<ArgumentOutOfRangeException>(() => AuditTrailPresentation.Create((CommonState)999, "Histórico", []));
        Assert.Throws<ArgumentException>(() => AuditEntryPresentation.Create(" ", "Ação"));

        var ready = AuditTrailPresentation.Ready("Histórico", []);
        Assert.False(ready.HasMessage);
        Assert.False(ready.HasEntries);
        Assert.False(ready.ShowsStateSurface);
    }

    private static string P2T02AuditSource()
    {
        var contracts = Path.Combine(RepositoryRoot(), "src", "DMO.Web", "Frontend", "Shared", "Contracts");
        var files = Directory.EnumerateFiles(contracts, "Audit*.cs", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal);

        return string.Join('\n', files.Select(File.ReadAllText));
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
