using System.Reflection;
using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.UnitTests.Frontend.Shared;

/// <summary>
/// P2-T02 (A4) unit tests — the <c>DenseDataTable</c> presentation contract.
/// Authority: <c>plans/contracts/P2-T02_DENSE_DATA_TABLE_AUDIT_TRAIL_CONTRACT.md</c> §5 and
/// AC-1 to AC-17. Contract rows covered: U1, U2, U3, U9, U10, U11, U12, U13.
/// Purpose: prove the supplied column/row order is authoritative, that supplied
/// filter/paging/sort state never transforms the rows, that the carriers fail closed, and that
/// the boundary is domain-neutral and URL-free.
/// Preconditions: none beyond the compiled P2-T02 contract assembly.
/// Required non-effects: no P2-T01 type is modified and no domain namespace is referenced.
/// </summary>
public sealed class DenseDataTableContractTests
{
    private static readonly Type[] DenseTableTypes =
    [
        typeof(DenseTableColumnAlignment),
        typeof(DenseTableColumnWidthHint),
        typeof(DenseTableSortDirection),
        typeof(DenseTableColumnPresentation),
        typeof(DenseTableCellPresentation),
        typeof(DenseTableRowPresentation),
        typeof(DenseTableFilterOptionPresentation),
        typeof(DenseTableFilterPresentation),
        typeof(DenseTablePagingPresentation),
        typeof(DenseTableSortPresentation),
        typeof(DenseTablePresentation),
        typeof(DenseTableEventKind),
        typeof(DenseTableEvent),
        typeof(DenseTableOutcome),
        typeof(DenseTableInteraction),
    ];

    private static readonly string[] UrlBearingMemberFragments =
    [
        "url", "uri", "href", "route", "navigat", "endpoint", "link", "target",
    ];

    private static readonly string[] ForbiddenDependencies =
    [
        "DbContext", "HttpClient", "Supabase", "ICurrentAccount", "TimeProvider", "DateTime.Now",
        "DateTime.UtcNow", "HttpContext", "IServiceProvider", "ILogger", "IStringLocalizer",
        "CultureInfo", "using DMO.",
        "ToolPicker", "ToolSummaryRow", "MeasurementRows", "DecisionBar", "ProductionContextStrip",
        "JobOn", "Boquilhas", "Ferramentas", "Peso", "Controlo", "tool_id", "jobon_id", "peso_id",
    ];

    /// <summary>
    /// U1 — the rendered column order is the supplied order, identical for every priority and
    /// width-hint combination (AC-1).
    /// </summary>
    [Fact]
    public void Columns_RenderInSuppliedOrderForEveryPriorityAndWidthHintCombination()
    {
        var suppliedOrder = new[] { "gamma", "alpha", "beta" };

        foreach (var priority in new int?[] { null, 0, 5 })
        {
            foreach (var widthHint in Enum.GetValues<DenseTableColumnWidthHint>())
            {
                var columns = suppliedOrder
                    .Select(key => DenseTableColumnPresentation.Create(
                        key, key.ToUpperInvariant(), priority: priority, widthHint: widthHint))
                    .ToList();
                var rows = new[] { Row("r1", suppliedOrder.Length) };

                var presentation = DenseTablePresentation.Ready("Tabela", columns, rows);

                Assert.Equal(suppliedOrder, presentation.Columns.Select(column => column.Key));
                Assert.Equal(suppliedOrder.Length, presentation.Columns.Count);

                // Priority and width hint are presentation metadata only: neither hides nor
                // reorders a column at any width.
                foreach (var column in presentation.Columns)
                {
                    Assert.Equal(priority, column.Priority);
                    Assert.Equal(widthHint, column.WidthHint);
                }
            }
        }
    }

    /// <summary>
    /// U1 — priority is never authority to hide or convert a column: every supplied column is
    /// present even when every column declares a priority (AC-1).
    /// </summary>
    [Fact]
    public void Columns_EverySuppliedColumnIsPresentRegardlessOfPriority()
    {
        var columns = new[]
        {
            DenseTableColumnPresentation.Create("a", "A", priority: 1),
            DenseTableColumnPresentation.Create("b", "B", priority: 2),
            DenseTableColumnPresentation.Create("c", "C", priority: 3),
        };

        var presentation = DenseTablePresentation.Ready("Tabela", columns, [Row("r1", 3)]);

        Assert.Equal(3, presentation.Columns.Count);
        Assert.True(presentation.HasColumns);
    }

    /// <summary>
    /// U1 — the accessible header name falls back to the visible heading and never replaces it
    /// (AC-1, §5.2).
    /// </summary>
    [Fact]
    public void Columns_AccessibleHeadingFallsBackToTheVisibleHeading()
    {
        var withoutAccessible = DenseTableColumnPresentation.Create("a", "Ferramenta");
        var withAccessible = DenseTableColumnPresentation.Create("b", "Ferramenta", accessibleHeading: "Ferramenta (código)");

        Assert.False(withoutAccessible.HasAccessibleHeading);
        Assert.Equal("Ferramenta", withoutAccessible.AccessibleHeaderName);
        Assert.True(withAccessible.HasAccessibleHeading);
        Assert.Equal("Ferramenta (código)", withAccessible.AccessibleHeaderName);
        Assert.Equal("Ferramenta", withAccessible.Heading);
    }

    /// <summary>
    /// U2 — rows render in the supplied order and supplied filter/paging/sort state never
    /// changes the rendered rows or their order (AC-2, AC-16).
    /// </summary>
    [Fact]
    public void Rows_SuppliedFilterPagingAndSortStateNeverTransformTheRows()
    {
        var columns = new[]
        {
            DenseTableColumnPresentation.CreateSortable("code", "Código"),
            DenseTableColumnPresentation.Create("name", "Nome"),
        };
        var rows = new[]
        {
            Row("r-3", 2, "CCC", "Terceiro"),
            Row("r-1", 2, "AAA", "Primeiro"),
            Row("r-2", 2, "BBB", "Segundo"),
        };

        var plain = DenseTablePresentation.Ready("Tabela", columns, rows);
        var withControlledState = DenseTablePresentation.Ready(
            "Tabela",
            columns,
            rows,
            filters:
            [
                DenseTableFilterPresentation.Create(
                    "q", "Pesquisa", "zzz",
                    [DenseTableFilterOptionPresentation.Create("zzz", "Zzz")]),
            ],
            paging: DenseTablePagingPresentation.Create(
                "Página 7 de 9",
                SharedActionPresentation.CreateEnabled("prev", "Anterior"),
                SharedActionPresentation.CreateEnabled("next", "Seguinte")),
            sort: DenseTableSortPresentation.Descending("code"));

        Assert.Equal(new[] { "r-3", "r-1", "r-2" }, plain.Rows.Select(row => row.Key));
        Assert.Equal(new[] { "r-3", "r-1", "r-2" }, withControlledState.Rows.Select(row => row.Key));
        Assert.Equal(RowSignature(plain), RowSignature(withControlledState));

        // The descending supplied sort state does not reverse the rows: the component never sorts.
        Assert.Equal(new[] { "CCC", "AAA", "BBB" }, withControlledState.Rows.Select(row => row.Cells[0].Text));
    }

    /// <summary>U3 — a row whose cell count differs from the column count fails before rendering (AC-3).</summary>
    [Fact]
    public void Rows_WithACellCountDifferentFromTheColumnCountAreRejected()
    {
        var columns = new[]
        {
            DenseTableColumnPresentation.Create("a", "A"),
            DenseTableColumnPresentation.Create("b", "B"),
        };

        var exception = Assert.Throws<ArgumentException>(
            () => DenseTablePresentation.Ready("Tabela", columns, [Row("r1", 1)]));

        Assert.Contains("r1", exception.Message, StringComparison.Ordinal);

        // The matching count is accepted, so the rejection is the mismatch and not the shape.
        var accepted = DenseTablePresentation.Ready("Tabela", columns, [Row("r1", 2)]);
        Assert.Single(accepted.Rows);
    }

    /// <summary>U9 — a selected key must reference a supplied row and requires selection enabled (AC-4).</summary>
    [Fact]
    public void SelectedKey_MustReferenceASuppliedRowAndRequireSelection()
    {
        var columns = new[] { DenseTableColumnPresentation.Create("a", "A") };
        var rows = new[] { Row("r1", 1), Row("r2", 1) };

        var phantom = Assert.Throws<ArgumentException>(
            () => DenseTablePresentation.Ready("Tabela", columns, rows, selectionEnabled: true, selectedKey: "nope"));
        Assert.Contains("selected", phantom.Message, StringComparison.OrdinalIgnoreCase);

        var disabled = Assert.Throws<ArgumentException>(
            () => DenseTablePresentation.Ready("Tabela", columns, rows, selectionEnabled: false, selectedKey: "r1"));
        Assert.Contains("selection", disabled.Message, StringComparison.OrdinalIgnoreCase);

        var controlled = DenseTablePresentation.Ready(
            "Tabela", columns, rows, selectionEnabled: true, selectedKey: "r2");
        Assert.Equal("r2", controlled.SelectedKey);
    }

    /// <summary>U10 — the carriers fail closed on blank mandatory values and missing messages (AC-8, §5.8).</summary>
    [Fact]
    public void Create_RejectsBlankMandatoryValuesAndAMissingNonReadyMessage()
    {
        var column = DenseTableColumnPresentation.Create("a", "A");

        Assert.Throws<ArgumentException>(() => DenseTableColumnPresentation.Create(" ", "A"));
        Assert.Throws<ArgumentException>(() => DenseTableColumnPresentation.Create("a", " "));
        Assert.Throws<ArgumentException>(() => DenseTableCellPresentation.Create(" "));
        Assert.Throws<ArgumentException>(() => DenseTableRowPresentation.Create(" ", "contexto", [Cell("x")]));
        Assert.Throws<ArgumentException>(() => DenseTableRowPresentation.Create("r1", " ", [Cell("x")]));
        Assert.Throws<ArgumentException>(() => DenseTablePresentation.Create(CommonState.Ready, " ", [column], [Row("r1", 1)]));
        Assert.ThrowsAny<ArgumentException>(() => DenseTablePresentation.Create(CommonState.Empty, "Tabela", [column], []));
        Assert.Throws<ArgumentException>(() => DenseTablePresentation.Create(CommonState.Empty, "Tabela", [column], [], message: " "));
        Assert.ThrowsAny<ArgumentException>(() => DenseTablePresentation.Create(CommonState.LookupFailed, "Tabela", [], []));
        Assert.Throws<ArgumentOutOfRangeException>(() => DenseTablePresentation.Create((CommonState)999, "Tabela", [], []));

        // A ready state legitimately carries no message.
        Assert.False(DenseTablePresentation.Ready("Tabela", [column], [Row("r1", 1)]).HasMessage);
    }

    /// <summary>U10 — a supplied sort must reference a supplied sortable column (AC-16, §5.5).</summary>
    [Fact]
    public void Create_RejectsASortThatDoesNotReferenceASuppliedSortableColumn()
    {
        var columns = new[]
        {
            DenseTableColumnPresentation.Create("plain", "Simples"),
            DenseTableColumnPresentation.CreateSortable("sortable", "Ordenável"),
        };

        Assert.Throws<ArgumentException>(
            () => DenseTablePresentation.Ready("Tabela", columns, [Row("r1", 2)], sort: DenseTableSortPresentation.Ascending("plain")));
        Assert.Throws<ArgumentException>(
            () => DenseTablePresentation.Ready("Tabela", columns, [Row("r1", 2)], sort: DenseTableSortPresentation.Ascending("absent")));

        var accepted = DenseTablePresentation.Ready(
            "Tabela", columns, [Row("r1", 2)], sort: DenseTableSortPresentation.Ascending("sortable"));
        Assert.True(accepted.HasSort);
        Assert.Equal("ascending", accepted.Sort!.AriaSortToken);
    }

    /// <summary>U11 — no table type exposes a URL, route or navigation target (AC-15).</summary>
    [Fact]
    public void ContractTypes_ExposeNoUrlRouteOrNavigationMember()
    {
        foreach (var type in DenseTableTypes)
        {
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                Assert.DoesNotContain(
                    UrlBearingMemberFragments,
                    fragment => member.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var property in type.GetProperties())
            {
                Assert.NotEqual(typeof(Uri), property.PropertyType);
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                Assert.NotEqual(typeof(Uri), method.ReturnType);
                Assert.DoesNotContain(method.GetParameters(), parameter => parameter.ParameterType == typeof(Uri));
            }
        }

        // No emitted event carries a URL-shaped value.
        var events = new[]
        {
            DenseTableEvent.RowSelected("row"),
            DenseTableEvent.OpenRequested("row"),
            DenseTableEvent.ActionInvoked("row", "act"),
            DenseTableEvent.FilterChanged("filter", "value"),
            DenseTableEvent.PageChanged("next"),
            DenseTableEvent.SortRequested("column", DenseTableSortDirection.Ascending),
        };

        foreach (var raised in events)
        {
            foreach (var property in typeof(DenseTableEvent).GetProperties())
            {
                if (property.GetValue(raised) is string value)
                {
                    Assert.DoesNotContain("://", value, StringComparison.Ordinal);
                    Assert.DoesNotContain("/", value, StringComparison.Ordinal);
                }
            }
        }
    }

    /// <summary>U12 — row actions reuse the accepted P2-T01 carrier and keep the disabled-reason rule (AC-12).</summary>
    [Fact]
    public void RowActions_ReuseTheAcceptedSharedActionCarrier()
    {
        var property = typeof(DenseTableRowPresentation).GetProperty(nameof(DenseTableRowPresentation.Actions));

        Assert.NotNull(property);
        Assert.Equal(typeof(IReadOnlyList<SharedActionPresentation>), property!.PropertyType);

        var disabled = SharedActionPresentation.CreateDisabled("open", "Abrir", "Ficheiro em falta.");
        Assert.False(disabled.Enabled);
        Assert.Equal("Ficheiro em falta.", disabled.DisabledReason);

        // The accepted P2-T01 carrier already rejects a disabled action without a supplied reason.
        Assert.Throws<ArgumentException>(
            () => SharedActionPresentation.Create("open", "Abrir", enabled: false));

        var row = DenseTableRowPresentation.Create("r1", "contexto", [Cell("x")], actions: [disabled]);
        Assert.True(row.HasActions);
        Assert.Equal("open", row.VisibleActions[0].Key);

        // An action that is not presented at all is not rendered.
        var hidden = DenseTableRowPresentation.Create(
            "r2", "contexto", [Cell("x")],
            actions: [SharedActionPresentation.Create("k", "L", enabled: true, visible: false)]);
        Assert.False(hidden.HasActions);
    }

    /// <summary>U13 — no table contract type references a feature/domain namespace, service or session (AC-25, AC-26).</summary>
    [Fact]
    public void ContractSource_IsDomainNeutralAndServiceFree()
    {
        var source = P2T02ContractSource();

        // The contract files import nothing at all: implicit framework usings suffice, so no
        // service, persistence or feature namespace is reachable from the shared carriers.
        Assert.DoesNotContain(
            source.Split('\n'),
            line => line.TrimStart().StartsWith("using ", StringComparison.Ordinal));

        Assert.All(
            ForbiddenDependencies,
            forbidden => Assert.DoesNotContain(forbidden, source, StringComparison.Ordinal));
    }

    /// <summary>U13 — no table contract member takes or returns a type outside the shared contract namespace.</summary>
    [Fact]
    public void ContractTypes_ReferenceOnlyFrameworkAndSharedContractTypes()
    {
        foreach (var type in DenseTableTypes)
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

    /// <summary>U13 (control-table separation) — the table never invents a table-specific state vocabulary.</summary>
    [Fact]
    public void StateSurface_UsesTheAcceptedP2T01VocabularyOnly()
    {
        var presentation = DenseTablePresentation.LookupFailed("Tabela", "A consulta falhou.");

        Assert.Equal(CommonState.LookupFailed, presentation.State);
        Assert.Equal("lookup-failed", presentation.StateToken);
        Assert.True(presentation.IsAssertive);
        Assert.True(presentation.ShowsStateSurface);
        Assert.True(presentation.RendersStateSurfaceOnly);

        // A failure can never look like an empty table.
        Assert.NotEqual(CommonState.Empty, presentation.State);
        Assert.False(presentation.RendersTable);
        Assert.False(presentation.RetainsHeaderContext);

        // The projection consumes the accepted P2-T01 carrier unchanged.
        var region = presentation.ToStateRegion();
        Assert.Equal(CommonState.LookupFailed, region.State);
        Assert.Equal("lookup-failed", region.StateToken);
        Assert.Equal("Tabela", region.RegionLabel);
    }

    /// <summary>U13 — an empty state retains the column/header context and is never a failure (AC-8).</summary>
    [Fact]
    public void EmptyState_RetainsHeaderContextAndIsNotAFailure()
    {
        var columns = new[] { DenseTableColumnPresentation.Create("a", "A") };

        var empty = DenseTablePresentation.Create(
            CommonState.Empty, "Tabela", columns, [], message: "Sem registos.",
            stateActions: [SharedActionPresentation.CreateEnabled("next", "Criar")]);

        Assert.True(empty.RetainsHeaderContext);
        Assert.True(empty.ShowsStateSurface);
        Assert.False(empty.IsAssertive);
        Assert.Equal("empty", empty.StateToken);
        Assert.Single(empty.Columns);
        Assert.Empty(empty.Rows);
    }

    private static DenseTableCellPresentation Cell(string text) => DenseTableCellPresentation.Create(text);

    private static DenseTableRowPresentation Row(string key, int cellCount, params string[] texts)
    {
        var cells = texts.Length == cellCount
            ? texts.Select(Cell).ToList()
            : Enumerable.Range(0, cellCount).Select(index => Cell($"{key}-{index}")).ToList();

        return DenseTableRowPresentation.Create(key, $"contexto {key}", cells);
    }

    private static string RowSignature(DenseTablePresentation presentation) =>
        string.Join(
            '\n',
            presentation.Rows.Select(row =>
                $"{row.Key}:{string.Join('|', row.Cells.Select(cell => cell.Text))}"));

    private static string P2T02ContractSource()
    {
        var contracts = Path.Combine(RepositoryRoot(), "src", "DMO.Web", "Frontend", "Shared", "Contracts");
        var files = Directory.EnumerateFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(path =>
            {
                var name = Path.GetFileName(path);
                return name.StartsWith("DenseTable", StringComparison.Ordinal) ||
                       name.StartsWith("Audit", StringComparison.Ordinal);
            })
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
