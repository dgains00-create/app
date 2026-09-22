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
/// P2-T02 (A4) rendered-test helper: renders the real compiled P2-T02 shared Razor partials
/// through the test host's own <see cref="IRazorViewEngine"/>.
/// </summary>
/// <remarks>
/// Authority: P2-T02 contract §10 ("rendered tests must exercise the real compiled partials
/// through the existing test-host view engine"). This class is additive: it follows the accepted
/// P2-T01 <c>SharedComponentRenderer</c> approach without modifying that P2-T01 helper or any of
/// its test methods (§10, AC-28).
/// <para>
/// The partials are production presentation artifacts that no operational page renders yet
/// (P2-T02 is shared infrastructure; no module page is in scope). Rendering the real, compiled
/// views — not copies — means the rendered-markup assertions exercise the same Razor source that
/// feature consumers will use. No route is advanced, no Module becomes available and no
/// availability registration is touched.
/// </para>
/// </remarks>
internal static class P2T02ComponentRenderer
{
    public static Task<string> RenderDenseTableAsync(
        DmoWebApplicationFactory factory,
        DenseTablePresentation model,
        string regionId = "t1") =>
        RenderAsync(factory, "/Pages/Shared/Components/_DenseDataTable.cshtml", model,
            new Dictionary<string, object?> { ["DenseTableRegionId"] = regionId });

    public static Task<string> RenderAuditTrailAsync(
        DmoWebApplicationFactory factory,
        AuditTrailPresentation model,
        string regionId = "h1") =>
        RenderAsync(factory, "/Pages/Shared/Components/_AuditTrail.cshtml", model,
            new Dictionary<string, object?> { ["AuditTrailRegionId"] = regionId });

    /// <summary>Renders the additive asset include helper (contract §14.5, test S5).</summary>
    public static Task<string> RenderSharedComponentAssetsAsync(DmoWebApplicationFactory factory) =>
        RenderAsync(factory, "/Pages/Shared/Components/_SharedComponentAssets.cshtml", new object());

    /// <summary>
    /// Renders a partial and returns the <b>raw</b>, still-encoded markup, so an assertion can
    /// prove that supplied text is emitted Razor-encoded rather than as consumer markup (Q3).
    /// </summary>
    public static Task<string> RenderRawAsync<TModel>(
        DmoWebApplicationFactory factory,
        string viewPath,
        TModel model,
        IDictionary<string, object?>? viewData = null)
        where TModel : class =>
        RenderInternalAsync(factory, viewPath, model, viewData, decode: false);

    public static async Task<string> RenderAsync<TModel>(
        DmoWebApplicationFactory factory,
        string viewPath,
        TModel model,
        IDictionary<string, object?>? viewData = null)
        where TModel : class =>
        await RenderInternalAsync(factory, viewPath, model, viewData, decode: true);

    private static async Task<string> RenderInternalAsync<TModel>(
        DmoWebApplicationFactory factory,
        string viewPath,
        TModel model,
        IDictionary<string, object?>? viewData,
        bool decode)
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
        var output = writer.ToString();

        // Razor escapes model expressions, so accented text arrives as numeric entities; decoding
        // makes the rendered output comparable to the accepted user-visible strings.
        return decode ? WebUtility.HtmlDecode(output) : output;
    }
}
