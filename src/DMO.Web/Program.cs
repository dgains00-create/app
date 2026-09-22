using DMO.Application.Accounts;
using DMO.Application.Authentication;
using DMO.Application.Migrations;
using DMO.Application.Session;
using DMO.Infrastructure;
using DMO.Infrastructure.Database;
using DMO.Web.Auth;
using DMO.Web.Authorization;
using DMO.Web.Endpoints;
using DMO.Web.Startup;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Composition root. The host wires the runtime: configuration binding, shared
// infrastructure registration and the HTTP pipeline. It owns no industrial business rule.
try
{
    builder.Services.AddDmoInfrastructure(builder.Configuration);

    // ---- P1-T02/P1-T03 authentication + account boundary ---------------------------
    // ADMIN authentication is real Supabase Auth against the DEV/TEST project using only
    // the project URL and the publishable key. The USER flow (P1-T03) resolves the
    // persisted carrier email for company_number through the narrow IUserAuthenticationLookup
    // and then uses the same Supabase password grant. No service_role, no secret key, no
    // fake provider, no in-memory account store.

    builder.Services.Configure<SupabaseOptions>(builder.Configuration.GetSection(SupabaseOptions.SectionName));

    // Fail fast at startup, exactly like the database configuration: the host must never
    // report itself as started when the Supabase configuration it will need is absent or
    // invalid, and it must never substitute a default.
    var supabaseOptions = new SupabaseOptions();
    builder.Configuration.GetSection(SupabaseOptions.SectionName).Bind(supabaseOptions);
    supabaseOptions.Validate();

    builder.Services.AddHttpContextAccessor();

    // Runtime session element (cookie scheme). A session is established only when
    // authentication succeeds AND account resolution returns an active ADMIN/USER.
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.Cookie.Name = "dmo.session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        });

    builder.Services.AddHttpClient<SupabaseAuthenticationService>((provider, client) =>
    {
        var options = provider.GetRequiredService<IOptions<SupabaseOptions>>().Value;
        client.BaseAddress = new Uri(options.ProjectUrl!.TrimEnd('/') + "/");
    });

    builder.Services.AddScoped<IAuthenticationBoundary>(static provider =>
        provider.GetRequiredService<SupabaseAuthenticationService>());
    builder.Services.AddScoped<IAccountResolver, AccountResolver>();
    builder.Services.AddScoped<ISessionAuthentication, SessionAuthentication>();
    builder.Services.AddScoped<ICurrentAccountContext, CurrentAccountContext>();

    // ---- P1-T04 server-side Module gate ---------------------------------------------
    // Authorization policies are a thin ASP.NET projection of the canonical Module ids:
    // one policy per ModuleCatalog identity, each carrying exactly one
    // ModuleAuthorizationRequirement. The handler resolves effective access through the
    // Module access service (registry + persisted Template); it grants nothing from claims,
    // roles, Template names or navigation visibility.
    builder.Services.AddModuleAuthorization();

    // ---- P1-T03 single-ADMIN bootstrap (deployment-only command) -------------------
    // Exactly three inputs (env/user-secrets only): AdminBootstrap__Email,
    // AdminBootstrap__DisplayName, AdminBootstrap__AuthIdentityId (the exact provider
    // subject of the existing Supabase Auth ADMIN identity). No provider call is made.
    builder.Services.Configure<AdminBootstrapOptions>(
        builder.Configuration.GetSection(AdminBootstrapOptions.SectionName));
    builder.Services.AddScoped<AdminBootstrapCommand>();
}
catch (DatabaseConfigurationException ex)
{
    // Fail loudly and legibly. The host must never start against a missing or invalid
    // database configuration, and must never substitute a default connection.
    Console.Error.WriteLine($"Startup failed: {ex.Message}");
    return StartupCommands.FailureExitCode;
}
catch (SupabaseConfigurationException ex)
{
    // Fail loudly and legibly for the same reason: no default Supabase configuration is
    // ever assumed and no unauthenticated fallback mode exists.
    Console.Error.WriteLine($"Startup failed: {ex.Message}");
    return StartupCommands.FailureExitCode;
}

var app = builder.Build();

// `--migrate` (or `migrate`) is a technical entry point that applies pending migrations and
// exits. P1-T03 adds the first two product migrations (AccountAndTemplateFoundation and
// TemplateModuleComposition).
if (StartupCommands.IsMigrationCommand(args))
{
    return await StartupCommands.RunMigrateAsync(app.Services, app.Logger);
}

// `--bootstrap-admin` (or `bootstrap-admin`) is the deployment-only single-ADMIN bootstrap
// (DEV/TEST), idempotent and provider-free.
if (StartupCommands.IsBootstrapAdminCommand(args))
{
    return await StartupCommands.RunBootstrapAdminAsync(app.Services, app.Logger);
}

app.UseAuthentication();

app.MapTechnicalEndpoints();
app.MapAuthEndpoints();

return await StartupCommands.RunHostAsync(app);