using System.Net;
using DMO.IntegrationTests.Host;
using DMO.Web.Frontend.Shared.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DMO.IntegrationTests.Frontend.Shared;

/// <summary>
/// P2-T03 (A5/A6) rendered-test helper: renders the real compiled P2-T03 shared Razor partials
/// through the test host's own <see cref="IRazorViewEngine"/>.
/// </summary>
/// <remarks>
/// Authority: P2-T03 contract §8 ("rendered tests must exercise the real compiled partials through
/// the existing test-host view engine", the accepted P2-T02 approach) and §8.4 (the accepted
/// verification strategy: C# model tests + rendered hook assertions + static-asset assertions).
/// <para>
/// This helper is additive: it follows the accepted P2-T01 <c>SharedComponentRenderer</c> and
/// P2-T02 <c>P2T02ComponentRenderer</c> approach without modifying either of them or any of their
/// test methods.
/// </para>
/// <para>
/// The partials are production presentation artifacts that no operational page renders yet (P2-T03
/// is shared infrastructure; no module page is in scope). Rendering the real, compiled views — not
/// copies — means the rendered-markup assertions exercise the same Razor source that feature
/// consumers will use. No route is advanced, no Module becomes available and no availability
/// registration is touched.
/// </para>
/// </remarks>
internal static class P2T03ComponentRenderer
{
    public static Task<string> RenderPickerAsync(
        DmoWebApplicationFactory factory,
        ToolPickerPresentation model,
        string regionId = "p1") =>
        RenderAsync(factory, "/Pages/Shared/Components/_ToolPicker.cshtml", model,
            new Dictionary<string, object?> { ["ToolPickerRegionId"] = regionId });

    public static Task<string> RenderSummaryRowAsync(
        DmoWebApplicationFactory factory,
        ToolSummaryRowPresentation model,
        string regionId = "s1") =>
        RenderAsync(factory, "/Pages/Shared/Components/_ToolSummaryRow.cshtml", model,
            new Dictionary<string, object?> { ["ToolSummaryRowRegionId"] = regionId });

    public static Task<string> RenderRowsAsync(
        DmoWebApplicationFactory factory,
        MeasurementRowsPresentation model,
        string regionId = "r1") =>
        RenderAsync(factory, "/Pages/Shared/Components/_MeasurementRows.cshtml", model,
            new Dictionary<string, object?> { ["MeasurementRowsRegionId"] = regionId });

    public static Task<string> RenderDecisionBarAsync(
        DmoWebApplicationFactory factory,
        DecisionBarPresentation model,
        string regionId = "d1") =>
        RenderAsync(factory, "/Pages/Shared/Components/_DecisionBar.cshtml", model,
            new Dictionary<string, object?> { ["DecisionBarRegionId"] = regionId });

    /// <summary>Renders the additively extended asset include helper (P2-T03 Appendix B.5, test ST9).</summary>
    public static Task<string> RenderSharedComponentAssetsAsync(DmoWebApplicationFactory factory) =>
        RenderAsync(factory, "/Pages/Shared/Components/_SharedComponentAssets.cshtml", new object());

    public static async Task<string> RenderAsync<TModel>(
        DmoWebApplicationFactory factory,
        string viewPath,
        TModel model,
        IDictionary<string, object?>? viewData = null)
        where TModel : class
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var viewEngine = services.GetRequiredService<IRazorViewEngine>();
        var tempDataProvider = services.GetRequiredService<ITempDataProvider>();

        var httpContext = new DefaultHttpContext { RequestServices = services };
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        var viewResult = viewEngine.GetView(executingFilePath: null, viewPath, isMainPage: false);
        if (!viewResult.Success)
        {
            var searched = string.Join(", ", viewResult.SearchedLocations ?? []);
            throw new InvalidOperationException(
                $"Shared partial '{viewPath}' was not found. Searched: {searched}");
        }

        var viewDataDictionary = new ViewDataDictionary<TModel>(
            new EmptyModelMetadataProvider(),
            new ModelStateDictionary())
        {
            Model = model,
        };

        if (viewData is not null)
        {
            foreach (var (key, value) in viewData)
            {
                viewDataDictionary[key] = value;
            }
        }

        await using var writer = new StringWriter();
        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewDataDictionary,
            new TempDataDictionary(httpContext, tempDataProvider),
            writer,
            new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);

        // Razor escapes model expressions, so accented text arrives as numeric entities; decoding
        // makes the rendered output comparable to the accepted user-visible strings.
        return WebUtility.HtmlDecode(writer.ToString());
    }
}
