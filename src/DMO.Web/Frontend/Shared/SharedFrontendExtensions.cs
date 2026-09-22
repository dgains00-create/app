using DMO.Web.Frontend.Shell;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DMO.Web.Frontend.Shared;

/// <summary>A-owned registration and composition seam for shared Razor presentation.</summary>
public static class SharedFrontendExtensions
{
    public static IServiceCollection AddDmoSharedFrontend(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRazorPages();
        services.AddScoped<NavigationProjectionService>();
        services.AddScoped<ShellPresentationService>();
        services.TryAddSingleton<IDestinationRouteRegistry, EmptyDestinationRouteRegistry>();

        return services;
    }

    public static WebApplication UseDmoSharedFrontend(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseStaticFiles();
        return app;
    }

    public static WebApplication MapDmoSharedFrontend(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapRazorPages();
        return app;
    }
}
