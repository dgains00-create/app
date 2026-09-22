using DMO.Web.Frontend.Shared.Contracts;
using DMO.IntegrationTests.Host;
using System.Net;
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
/// P2-T01 (A3) test helper: renders the real compiled shared Razor partials through the test
/// host's own <see cref="IRazorViewEngine"/>.
/// </summary>
/// <remarks>
/// The partials are production presentation artifacts that no operational page renders yet
/// (P2-T01 is shared infrastructure; no module page is in scope). This helper therefore renders
/// the real, compiled views — not copies — so the rendered-markup assertions exercise the same
/// Razor source that feature consumers will use. It advances no route and registers no Module:
/// it only asks the existing view engine to render an existing partial into a string.
/// </remarks>
internal static class SharedComponentRenderer
{
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

    public static Task<string> RenderCommonStateAsync(
        DmoWebApplicationFactory factory,
        CommonStateRegionPresentation model,
        string regionId) =>
        RenderAsync(factory, "/Pages/Shared/Components/_CommonStateRegion.cshtml", model,
            new Dictionary<string, object?> { ["CommonStateRegionId"] = regionId });

    public static Task<string> RenderRecordStatusAsync(
        DmoWebApplicationFactory factory,
        RecordStatusPresentation model) =>
        RenderAsync(factory, "/Pages/Shared/Components/_RecordStatus.cshtml", model);

    public static Task<string> RenderAvailabilityAsync(
        DmoWebApplicationFactory factory,
        AvailabilityPresentation model,
        string regionId) =>
        RenderAsync(factory, "/Pages/Shared/Components/_AvailabilityState.cshtml", model,
            new Dictionary<string, object?> { ["AvailabilityRegionId"] = regionId });
}
