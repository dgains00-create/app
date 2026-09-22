using System.Text.RegularExpressions;
using DMO.IntegrationTests.Host;
using DMO.Web.Frontend.Shared.Contracts;

namespace DMO.IntegrationTests.Frontend.Shared;

/// <summary>
/// P2-T02 (A4) integration — the real rendered <c>DenseDataTable</c> surface.
/// Authority: <c>plans/contracts/P2-T02_DENSE_DATA_TABLE_AUDIT_TRAIL_CONTRACT.md</c> §5, §7.1,
/// §8.1, §9 and AC-1 to AC-17. Contract rows covered: R1, R2, R3, R4, R5, R6, R7, R8, R9, R13
/// plus the rendered halves of U1 and U2 and the Q3 encoded-text rule.
/// Purpose: prove the compiled partial renders stable supplied order, distinct P2-T01 state
/// surfaces, non-colour-only selection, associated disabled reasons, consumer-gated open, row
/// status reuse and URL-free controlled affordances.
/// Preconditions: the real compiled P2-T02 partials rendered through the test host view engine.
/// Required non-effects: no route is registered, no Module becomes available and the shared
/// shell/navigation composition is untouched.
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class DenseDataTableRenderingTests
{
    private static readonly Regex RowRegex = new(
        "<tr class=\"dmo-table__row[^>]*?data-dmo-row-key=\"(?<key>[^\"]+)\"",
        RegexOptions.Singleline);

    [Fact]
    public async Task Rendering_EmptyAndLookupFailed_AreDistinctAndNeverAliased()
    {
        using var factory = new DmoWebApplicationFactory();

        var empty = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory,
            DenseTablePresentation.Create(
                CommonState.Empty, "Ferramentas", Columns(), [], message: "Sem registos."));
        var lookupFailed = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory,
            DenseTablePresentation.LookupFailed(
                "Ferramentas", "A consulta falhou.",
                SharedActionPresentation.CreateEnabled("retry", "Tentar novamente")));

        Assert.Contains("dmo-state--empty", empty, StringComparison.Ordinal);
        Assert.Contains("Sem registos.", empty, StringComparison.Ordinal);
        Assert.DoesNotContain("dmo-state--lookup-failed", empty, StringComparison.Ordinal);
        Assert.DoesNotContain("A consulta falhou.", empty, StringComparison.Ordinal);

        Assert.Contains("dmo-state--lookup-failed", lookupFailed, StringComparison.Ordinal);
        Assert.Contains("A consulta falhou.", lookupFailed, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"assertive\"", lookupFailed, StringComparison.Ordinal);
        Assert.Contains("data-dmo-action=\"retry\"", lookupFailed, StringComparison.Ordinal);
        Assert.DoesNotContain("dmo-state--empty", lookupFailed, StringComparison.Ordinal);
        Assert.DoesNotContain("Sem registos.", lookupFailed, StringComparison.Ordinal);

        Assert.NotEqual(empty, lookupFailed);
    }

    [Fact]
    public async Task Rendering_EmptyRetainsHeaderContext_AndFailuresRenderNoDataBody()
    {
        using var factory = new DmoWebApplicationFactory();

        var empty = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory,
            DenseTablePresentation.Create(CommonState.Empty, "Ferramentas", Columns(), [], message: "Sem registos."));

        // The column/header context is retained, so an empty result still shows what is missing.
        Assert.Contains("<caption class=\"dmo-table__caption\">Ferramentas</caption>", empty, StringComparison.Ordinal);
        Assert.Contains("data-dmo-column=\"code\"", empty, StringComparison.Ordinal);
        Assert.Contains("data-dmo-column=\"lot\"", empty, StringComparison.Ordinal);
        Assert.DoesNotContain("<tbody>", empty, StringComparison.Ordinal);
        Assert.Contains("Sem registos.", empty, StringComparison.Ordinal);
        Assert.Contains("dmo-state--empty", empty, StringComparison.Ordinal);

        var protectedStates = new[]
        {
            DenseTablePresentation.LookupFailed("Ferramentas", "A consulta falhou."),
            DenseTablePresentation.Unavailable("Ferramentas", "Serviço indisponível.", "Manutenção."),
            DenseTablePresentation.PermissionDenied("Ferramentas", "Acesso negado.", "Sem permissão."),
            DenseTablePresentation.Conflict("Ferramentas", "Conflito de dados."),
        };

        var renderings = new List<string>();
        foreach (var presentation in protectedStates)
        {
            var rendered = await P2T02ComponentRenderer.RenderDenseTableAsync(factory, presentation);

            // A failure can never look like an empty table: no table, no header and no row data.
            Assert.DoesNotContain("<table", rendered, StringComparison.Ordinal);
            Assert.DoesNotContain("dmo-table__caption", rendered, StringComparison.Ordinal);
            Assert.DoesNotContain("dmo-table__row", rendered, StringComparison.Ordinal);
            Assert.Contains("dmo-state--", rendered, StringComparison.Ordinal);
            renderings.Add(rendered);
        }

        Assert.Equal(renderings.Count, renderings.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task Rendering_UnavailableAndPermissionDenied_AssociateTheSuppliedReason()
    {
        using var factory = new DmoWebApplicationFactory();

        var unavailable = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory,
            DenseTablePresentation.Unavailable("Ferramentas", "Serviço indisponível.", "Manutenção programada."));
        var denied = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory,
            DenseTablePresentation.PermissionDenied("Ferramentas", "Acesso negado.", "Sem permissão de leitura."));

        Assert.Contains("dmo-state--unavailable", unavailable, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"t1-state-reason\"", unavailable, StringComparison.Ordinal);
        Assert.Contains("id=\"t1-state-reason\"", unavailable, StringComparison.Ordinal);
        Assert.Contains("Manutenção programada.", unavailable, StringComparison.Ordinal);

        Assert.Contains("dmo-state--permission-denied", denied, StringComparison.Ordinal);
        Assert.Contains("Sem permissão de leitura.", denied, StringComparison.Ordinal);
        Assert.NotEqual(unavailable, denied);
    }

    [Fact]
    public async Task Rendering_LoadingWithRetainedRows_MarksTheRegionBusyAndLabelsEveryRowStale()
    {
        using var factory = new DmoWebApplicationFactory();

        var rows = new[]
        {
            Row("r1", "Ferramenta A", ["AAA", "Lote 1"]),
            Row("r2", "Ferramenta B", ["BBB", "Lote 2"]),
        };

        var retained = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory,
            DenseTablePresentation.Loading("Ferramentas", "A carregar.", Columns(), rows));

        Assert.Contains("aria-busy=\"true\"", retained, StringComparison.Ordinal);
        Assert.Contains("Desatualizada", retained, StringComparison.Ordinal);
        Assert.Equal(2, Count(retained, "data-dmo-row-stale=\"true\""));
        Assert.Equal(new[] { "r1", "r2" }, RowKeys(retained));

        // With rows retained, the busy marking replaces the P2-T01 loading surface.
        Assert.DoesNotContain("dmo-state--loading", retained, StringComparison.Ordinal);

        var withoutRows = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory,
            DenseTablePresentation.Loading("Ferramentas", "A carregar.", Columns()));

        Assert.Contains("dmo-state--loading", withoutRows, StringComparison.Ordinal);
        Assert.Contains("A carregar.", withoutRows, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rendering_StaleRetainsRowsAndShowsTheAcceptedStaleSurface()
    {
        using var factory = new DmoWebApplicationFactory();

        var rows = new[] { Row("r1", "Ferramenta A", ["AAA", "Lote 1"]) };
        var rendered = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory,
            DenseTablePresentation.Stale(
                "Ferramentas", "Informação desatualizada.", Columns(), rows,
                [SharedActionPresentation.CreateEnabled("refresh", "Atualizar")]));

        Assert.Contains("dmo-state--stale", rendered, StringComparison.Ordinal);
        Assert.Contains("Informação desatualizada.", rendered, StringComparison.Ordinal);
        Assert.Contains("data-dmo-action=\"refresh\"", rendered, StringComparison.Ordinal);
        Assert.Equal(new[] { "r1" }, RowKeys(rendered));
    }

    [Fact]
    public async Task Rendering_SelectedRow_IsProgrammaticAndCarriesAVisibleMarker()
    {
        using var factory = new DmoWebApplicationFactory();

        var rows = new[]
        {
            Row("r1", "Ferramenta A", ["AAA", "Lote 1"]),
            Row("r2", "Ferramenta B", ["BBB", "Lote 2"]),
        };

        var rendered = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory,
            DenseTablePresentation.Ready(
                "Ferramentas", Columns(), rows, selectionEnabled: true, selectedKey: "r2"));

        Assert.Equal(1, Count(rendered, "aria-selected=\"true\""));
        Assert.Equal(1, Count(rendered, "aria-selected=\"false\""));
        Assert.Equal(1, Count(rendered, "data-dmo-row-marker=\"true\""));

        var selected = RowSegment(rendered, "r2");
        Assert.Contains("aria-selected=\"true\"", selected, StringComparison.Ordinal);
        Assert.Contains("data-dmo-row-marker=\"true\"", selected, StringComparison.Ordinal);

        var unselected = RowSegment(rendered, "r1");
        Assert.Contains("aria-selected=\"false\"", unselected, StringComparison.Ordinal);
        Assert.DoesNotContain("data-dmo-row-marker", unselected, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rendering_DisabledRowAction_AssociatesItsReasonAndNamesTheRowContext()
    {
        using var factory = new DmoWebApplicationFactory();

        var disabled = SharedActionPresentation.CreateDisabled("open-doc", "Abrir documento", "Sem permissão.");
        var rows = new[]
        {
            Row("r1", "Ferramenta AAA lote 1", ["AAA", "Lote 1"], actions: [disabled]),
        };

        var rendered = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory,
            DenseTablePresentation.Ready("Ferramentas", Columns(), rows));

        Assert.Contains("data-dmo-action=\"open-doc\"", rendered, StringComparison.Ordinal);
        Assert.Contains("data-dmo-row-key=\"r1\"", rendered, StringComparison.Ordinal);
        Assert.Contains("data-dmo-action-scope=\"row\"", rendered, StringComparison.Ordinal);
        Assert.Contains("aria-disabled=\"true\"", rendered, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"t1-row-r1-action-open-doc-reason\"", rendered, StringComparison.Ordinal);
        Assert.Contains("id=\"t1-row-r1-action-open-doc-reason\"", rendered, StringComparison.Ordinal);
        Assert.Contains("Sem permissão.", rendered, StringComparison.Ordinal);

        var button = ButtonTag(rendered, "open-doc");
        Assert.Matches(@"\sdisabled(\s|>|$)", button);
        Assert.Contains("aria-label=\"Abrir documento — Ferramenta AAA lote 1\"", button, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rendering_OpenControl_ExistsOnlyWhenTheConsumerEnablesOpen()
    {
        using var factory = new DmoWebApplicationFactory();

        var rows = new[] { Row("r1", "Ferramenta A", ["AAA", "Lote 1"]) };

        var gated = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory, DenseTablePresentation.Ready("Ferramentas", Columns(), rows, openEnabled: true));
        var notGated = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory, DenseTablePresentation.Ready("Ferramentas", Columns(), rows));

        Assert.Contains("data-dmo-open=\"r1\"", gated, StringComparison.Ordinal);
        Assert.Contains("data-dmo-open-enabled=\"true\"", gated, StringComparison.Ordinal);
        Assert.Contains("Abrir — Ferramenta A", gated, StringComparison.Ordinal);

        Assert.DoesNotContain("data-dmo-open=", notGated, StringComparison.Ordinal);
        Assert.Contains("data-dmo-open-enabled=\"false\"", notGated, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rendering_ControlColumn_IsLastAndExistsOnlyWhenNeeded()
    {
        using var factory = new DmoWebApplicationFactory();

        var action = SharedActionPresentation.CreateEnabled("open-doc", "Abrir documento");
        var withActions = new[] { Row("r1", "Ferramenta A", ["AAA", "Lote 1"], actions: [action]) };
        var withoutActions = new[] { Row("r1", "Ferramenta A", ["AAA", "Lote 1"]) };

        var renderedWithActions = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory,
            DenseTablePresentation.Ready("Ferramentas", Columns(), withActions, selectionEnabled: true));
        var renderedWithoutActions = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory,
            DenseTablePresentation.Ready("Ferramentas", Columns(), withoutActions, selectionEnabled: true));

        var header = HeaderSegment(renderedWithActions);
        Assert.Contains("data-dmo-column-controls=\"true\"", header, StringComparison.Ordinal);
        Assert.True(
            header.LastIndexOf("data-dmo-column-controls", StringComparison.Ordinal) >
            header.LastIndexOf("data-dmo-column=\"", StringComparison.Ordinal),
            "The component-owned control cell must be the last rendered column.");
        Assert.Contains(">Ações</th>", header, StringComparison.Ordinal);

        // A row with no supplied action and no open control renders no interactive element.
        Assert.DoesNotContain("data-dmo-column-controls", renderedWithoutActions, StringComparison.Ordinal);
        Assert.DoesNotContain("<button", renderedWithoutActions, StringComparison.Ordinal);

        var customHeading = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory,
            DenseTablePresentation.Ready(
                "Ferramentas", Columns(), withActions, actionsColumnHeading: "Documentos"));
        Assert.Contains(">Documentos</th>", HeaderSegment(customHeading), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rendering_RowStatus_ReusesTheAcceptedP2T01StatusPartial()
    {
        using var factory = new DmoWebApplicationFactory();

        var rows = new[]
        {
            Row(
                "r1",
                "Ferramenta A",
                ["AAA", "Lote 1"],
                status: RecordStatusPresentation.Create("Aguarda aprovação", StatusTone.Warning)),
            Row("r2", "Ferramenta B", ["BBB", "Lote 2"]),
        };

        var rendered = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory,
            DenseTablePresentation.Ready("Ferramentas", Columns(), rows));

        Assert.Contains("dmo-status--warning", rendered, StringComparison.Ordinal);
        Assert.Contains("data-dmo-status-tone=\"warning\"", rendered, StringComparison.Ordinal);
        Assert.Contains("Aguarda aprovação", rendered, StringComparison.Ordinal);
        Assert.Contains("data-dmo-row-status=\"true\"", rendered, StringComparison.Ordinal);
        Assert.Equal(1, Count(rendered, "dmo-status--"));

        // A row without a supplied status renders no status at all.
        Assert.DoesNotContain("dmo-status", RowSegment(rendered, "r2"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rendering_ResultSummary_IsVerbatimInsideAPoliteLiveRegion()
    {
        using var factory = new DmoWebApplicationFactory();

        var rows = new[] { Row("r1", "Ferramenta A", ["AAA", "Lote 1"]) };
        var rendered = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory,
            DenseTablePresentation.Ready("Ferramentas", Columns(), rows, resultSummary: "12 registos encontrados"));

        Assert.Contains("data-dmo-table-summary=\"true\"", rendered, StringComparison.Ordinal);
        Assert.Contains("role=\"status\"", rendered, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"polite\"", rendered, StringComparison.Ordinal);
        Assert.Contains("12 registos encontrados", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rendering_ControlledAffordances_RenderTheirValuesAndConstructNoUrl()
    {
        using var factory = new DmoWebApplicationFactory();

        var columns = new[]
        {
            DenseTableColumnPresentation.CreateSortable("code", "Código"),
            DenseTableColumnPresentation.Create("lot", "Lote"),
        };
        var rows = new[] { Row("r1", "Ferramenta A", ["AAA", "Lote 1"]) };

        var rendered = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory,
            DenseTablePresentation.Ready(
                "Ferramentas",
                columns,
                rows,
                filters:
                [
                    DenseTableFilterPresentation.Create(
                        "lot", "Lote", "Lote 1",
                        [
                            DenseTableFilterOptionPresentation.Create("Lote 1", "Lote 1"),
                            DenseTableFilterOptionPresentation.Create("Lote 2", "Lote 2"),
                        ]),
                    DenseTableFilterPresentation.Create("text", "Texto", "abc"),
                ],
                paging: DenseTablePagingPresentation.Create(
                    "Página 2 de 4",
                    SharedActionPresentation.CreateEnabled("previous", "Anterior"),
                    SharedActionPresentation.CreateDisabled("next", "Seguinte", "Última página.")),
                sort: DenseTableSortPresentation.Create("code", DenseTableSortDirection.Descending)));

        Assert.Contains("data-dmo-filter=\"lot\"", rendered, StringComparison.Ordinal);
        Assert.Contains("data-dmo-filter-value=\"Lote 1\"", rendered, StringComparison.Ordinal);
        Assert.Contains("<select", rendered, StringComparison.Ordinal);
        Assert.Contains("<option value=\"Lote 1\"", rendered, StringComparison.Ordinal);
        Assert.Contains("selected", rendered, StringComparison.Ordinal);
        Assert.Contains("<input type=\"text\"", rendered, StringComparison.Ordinal);
        Assert.Contains("value=\"abc\"", rendered, StringComparison.Ordinal);

        Assert.Contains("data-dmo-page=\"previous\"", rendered, StringComparison.Ordinal);
        Assert.Contains("data-dmo-page=\"next\"", rendered, StringComparison.Ordinal);
        Assert.Contains("Página 2 de 4", rendered, StringComparison.Ordinal);
        Assert.Contains("Última página.", rendered, StringComparison.Ordinal);

        Assert.Contains("data-dmo-sort=\"code\"", rendered, StringComparison.Ordinal);
        Assert.Contains("aria-sort=\"descending\"", rendered, StringComparison.Ordinal);

        // No consumer URL is constructed and no form target is emitted by the component.
        Assert.DoesNotContain("href", rendered, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"[\s""']action\s*=", rendered);
        Assert.DoesNotMatch(@"[\s""']method\s*=", rendered);
        Assert.DoesNotContain("<form", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rendering_SortAffordance_ExposesOnlyTheSuppliedDirection()
    {
        using var factory = new DmoWebApplicationFactory();

        var columns = new[]
        {
            DenseTableColumnPresentation.CreateSortable("code", "Código"),
            DenseTableColumnPresentation.CreateSortable("lot", "Lote"),
        };

        var unsorted = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory, DenseTablePresentation.Ready("Ferramentas", columns, []));

        Assert.Contains("data-dmo-sort=\"code\"", unsorted, StringComparison.Ordinal);
        Assert.Contains("data-dmo-column-sortable=\"true\"", unsorted, StringComparison.Ordinal);
        Assert.DoesNotContain("aria-sort", unsorted, StringComparison.Ordinal);

        var sorted = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory,
            DenseTablePresentation.Ready("Ferramentas", columns, [], sort: DenseTableSortPresentation.Ascending("lot")));

        Assert.Equal(1, Count(sorted, "aria-sort="));

        var header = HeaderSegment(sorted);
        Assert.True(
            header.IndexOf("aria-sort=\"ascending\"", StringComparison.Ordinal) >
            header.IndexOf("data-dmo-column=\"lot\"", StringComparison.Ordinal),
            "aria-sort must be exposed on the supplied sorted column only.");
    }

    [Fact]
    public async Task Rendering_ScrollContainer_IsFocusableLabelledAndTrapsNoFocus()
    {
        using var factory = new DmoWebApplicationFactory();

        var rows = new[] { Row("r1", "Ferramenta A", ["AAA", "Lote 1"]) };
        var rendered = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory, DenseTablePresentation.Ready("Ferramentas", Columns(), rows));

        Assert.Contains("class=\"dmo-table__scroll\"", rendered, StringComparison.Ordinal);
        Assert.Contains("data-dmo-table-scroll=\"true\"", rendered, StringComparison.Ordinal);
        Assert.Contains("tabindex=\"0\"", rendered, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Ferramentas\"", rendered, StringComparison.Ordinal);
        Assert.Contains("role=\"region\"", rendered, StringComparison.Ordinal);

        // No composite-widget ARIA and no focus-trapping handler is invented.
        Assert.DoesNotContain("role=\"grid\"", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("onkeydown", rendered, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Rendering_StructureIsAFixedSemanticTableWithNoCardConversion()
    {
        using var factory = new DmoWebApplicationFactory();

        var rows = new[] { Row("r1", "Ferramenta A", ["AAA", "Lote 1"]) };
        var rendered = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory,
            DenseTablePresentation.Ready(
                "Ferramentas", Columns(), rows, selectionEnabled: true, selectedKey: "r1"));

        var table = TableSegment(rendered);

        Assert.StartsWith("<table", table, StringComparison.Ordinal);
        Assert.Contains("<thead>", table, StringComparison.Ordinal);
        Assert.Contains("<tbody>", table, StringComparison.Ordinal);
        Assert.Contains("scope=\"col\"", table, StringComparison.Ordinal);
        Assert.Contains("<caption class=\"dmo-table__caption\">Ferramentas</caption>", table, StringComparison.Ordinal);

        // No table-to-card conversion and no breakpoint-driven structural variant exists.
        Assert.DoesNotContain("dmo-table__card", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("dmo-table__cards", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("display:none", rendered, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Rendering_ColumnOrder_IsTheSuppliedOrderForEveryPriorityAndWidthHint()
    {
        using var factory = new DmoWebApplicationFactory();

        var suppliedOrder = new[] { "gamma", "alpha", "beta" };
        var baseline = Array.Empty<string>();

        foreach (var priority in new int?[] { null, 0, 7 })
        {
            foreach (var widthHint in Enum.GetValues<DenseTableColumnWidthHint>())
            {
                var columns = suppliedOrder
                    .Select(key => DenseTableColumnPresentation.Create(
                        key, key, priority: priority, widthHint: widthHint))
                    .ToList();
                var rows = new[] { Row("r1", "Ferramenta A", suppliedOrder.ToArray()) };

                var rendered = await P2T02ComponentRenderer.RenderDenseTableAsync(
                    factory, DenseTablePresentation.Ready("Ferramentas", columns, rows));

                var order = HeaderColumnKeys(rendered);
                Assert.Equal(suppliedOrder, order);

                // Every supplied column renders: no column is hidden at any width.
                Assert.All(
                    suppliedOrder,
                    key => Assert.Contains($"data-dmo-column=\"{key}\"", rendered, StringComparison.Ordinal));

                if (baseline.Length == 0)
                {
                    baseline = order.ToArray();
                }

                Assert.Equal(baseline, order);
            }
        }
    }

    [Fact]
    public async Task Rendering_SuppliedFilterPagingAndSortState_NeverChangesTheRenderedRows()
    {
        using var factory = new DmoWebApplicationFactory();

        var columns = new[]
        {
            DenseTableColumnPresentation.CreateSortable("code", "Código"),
            DenseTableColumnPresentation.Create("lot", "Lote"),
        };
        var rows = new[]
        {
            Row("r3", "Ferramenta C", ["CCC", "Lote 3"]),
            Row("r1", "Ferramenta A", ["AAA", "Lote 1"]),
            Row("r2", "Ferramenta B", ["BBB", "Lote 2"]),
        };

        var plain = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory, DenseTablePresentation.Ready("Ferramentas", columns, rows));

        var controlled = await P2T02ComponentRenderer.RenderDenseTableAsync(
            factory,
            DenseTablePresentation.Ready(
                "Ferramentas",
                columns,
                rows,
                filters: [DenseTableFilterPresentation.Create("text", "Texto", "zzz")],
                paging: DenseTablePagingPresentation.Create(
                    "Página 9 de 9", SharedActionPresentation.CreateEnabled("next", "Seguinte")),
                sort: DenseTableSortPresentation.Descending("code")));

        Assert.Equal(new[] { "r3", "r1", "r2" }, RowKeys(plain));
        Assert.Equal(new[] { "r3", "r1", "r2" }, RowKeys(controlled));
        Assert.Equal(BodySegment(plain), BodySegment(controlled));

        // Descending supplied sort state does not reorder the rows: the component never sorts.
        Assert.True(
            BodySegment(controlled).IndexOf("CCC", StringComparison.Ordinal) <
            BodySegment(controlled).IndexOf("AAA", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Rendering_CellText_IsEncodedAsVisiblePlainTextAndNeverAsMarkup()
    {
        using var factory = new DmoWebApplicationFactory();

        var rows = new[] { Row("r1", "Ferramenta A", ["<b>x</b> & \"y\"", "Lote 1"]) };
        var model = DenseTablePresentation.Ready("Ferramentas", Columns(), rows);

        // Raw markup: the supplied text arrives Razor-encoded, so no consumer markup is injected.
        var raw = await P2T02ComponentRenderer.RenderRawAsync(
            factory,
            "/Pages/Shared/Components/_DenseDataTable.cshtml",
            model,
            new Dictionary<string, object?> { ["DenseTableRegionId"] = "t1" });

        Assert.Contains("&lt;b&gt;x&lt;/b&gt; &amp; &quot;y&quot;", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>x</b>", raw, StringComparison.Ordinal);

        // Decoded markup: the same text is visible plain text, never interpreted markup.
        var decoded = await P2T02ComponentRenderer.RenderDenseTableAsync(factory, model);
        Assert.Contains("<b>x</b> & \"y\"", decoded, StringComparison.Ordinal);
    }

    private static IReadOnlyList<DenseTableColumnPresentation> Columns() =>
    [
        DenseTableColumnPresentation.Create("code", "Código"),
        DenseTableColumnPresentation.Create("lot", "Lote"),
    ];

    private static DenseTableRowPresentation Row(
        string key,
        string context,
        string[] cells,
        RecordStatusPresentation? status = null,
        IReadOnlyList<SharedActionPresentation>? actions = null) =>
        DenseTableRowPresentation.Create(
            key,
            context,
            cells.Select(DenseTableCellPresentation.Create).ToList(),
            status,
            actions);

    private static int Count(string value, string fragment) =>
        value.Split(fragment, StringSplitOptions.None).Length - 1;

    private static string TableSegment(string html)
    {
        var start = html.IndexOf("<table", StringComparison.Ordinal);
        var end = html.IndexOf("</table>", StringComparison.Ordinal);

        return start < 0 || end < 0 ? string.Empty : html[start..(end + "</table>".Length)];
    }

    private static string HeaderSegment(string html)
    {
        var start = html.IndexOf("<thead>", StringComparison.Ordinal);
        var end = html.IndexOf("</thead>", StringComparison.Ordinal);

        return start < 0 || end < 0 ? string.Empty : html[start..end];
    }

    private static string BodySegment(string html)
    {
        var start = html.IndexOf("<tbody>", StringComparison.Ordinal);
        var end = html.IndexOf("</tbody>", StringComparison.Ordinal);

        return start < 0 || end < 0 ? string.Empty : html[start..end];
    }

    private static IReadOnlyList<string> HeaderColumnKeys(string html) =>
        Regex.Matches(HeaderSegment(html), "data-dmo-column=\"(?<key>[^\"]+)\"")
            .Select(match => match.Groups["key"].Value)
            .ToList();

    private static IReadOnlyList<string> RowKeys(string html) =>
        RowRegex.Matches(BodySegment(html)).Select(match => match.Groups["key"].Value).ToList();

    private static string RowSegment(string html, string rowKey)
    {
        var body = BodySegment(html);
        var matches = RowRegex.Matches(body);

        for (var index = 0; index < matches.Count; index++)
        {
            if (matches[index].Groups["key"].Value != rowKey)
            {
                continue;
            }

            var start = matches[index].Index;
            var end = index + 1 < matches.Count ? matches[index + 1].Index : body.Length;

            return body[start..end];
        }

        return string.Empty;
    }

    private static string ButtonTag(string html, string actionKey) =>
        Regex.Match(html, $"<button[^>]*data-dmo-action=\"{Regex.Escape(actionKey)}\"[^>]*>", RegexOptions.Singleline)
            .Value;
}
