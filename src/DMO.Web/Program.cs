using DMO.Application.Accounts;
using DMO.Application.Authentication;
using DMO.Application.Migrations;
using DMO.Application.Session;
using DMO.Infrastructure;
using DMO.Infrastructure.Database;
using DMO.Web.Auth;
using DMO.Web.Endpoints;
using DMO.Web.Resolution;
using DMO.Web.Startup;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Composition root. The host wires the runtime: configuration binding, shared
// infrastructure registration and the HTTP pipeline. It owns no industrial business rule.
try
{
    builder.Services.AddDmoInfrastructure(builder.Configuration);

    // ---- P1-T02 authentication + account boundary -------------------------------
    // ADMIN authentication is real Supabase Auth against the DEV/TEST project using only
    // the project URL and the publishable key. No service_role, no secret key, no fake
    // provider, no in-memory account store.

    builder.Services.Configure<SupabaseOptions>(builder.Configuration.GetSection(SupabaseOptions.SectionName));

    // Fail fast at startup, exactly like the database configuration: the host must never
    // report itself as started when the Supabase configuration it will need is absent or
    // invalid, and it must never substitute a default.
    var supabaseOptions = new SupabaseOptions();
    builder.Configuration.GetSection(SupabaseOptions.SectionName).Bind(supabaseOptions);
    supabaseOptions.Validate();

    builder.Services.AddHttpContextAccessor();

    // Runtime session element (cookie scheme). In P1-T02 production no session is ever
    // established because account resolution always fails closed (UnavailableAccountLookup
    // until P1-T03); the scheme below is the real mechanism, exercised by tests with
    // test-only fakes.
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
    builder.Services.AddScoped<IAccountLookup, UnavailableAccountLookup>();
    builder.Services.AddScoped<IAccountResolver, AccountResolver>();
    builder.Services.AddScoped<ISessionAuthentication, SessionAuthentication>();
    builder.Services.AddScoped<ICurrentAccountContext, CurrentAccountContext>();
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
// exits. At P1-T02 no product migration exists, so it applies nothing and reports that.
if (StartupCommands.IsMigrationCommand(args))
{
    return await StartupCommands.RunMigrateAsync(app.Services, app.Logger);
}

app.UseAuthentication();

app.MapTechnicalEndpoints();
app.MapAuthEndpoints();

return await StartupCommands.RunHostAsync(app);