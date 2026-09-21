using DMO.Application.Migrations;
using DMO.Infrastructure;
using DMO.Infrastructure.Database;
using DMO.Web.Endpoints;
using DMO.Web.Startup;

var builder = WebApplication.CreateBuilder(args);

// Composition root. The host wires the runtime: configuration binding, shared
// infrastructure registration and the HTTP pipeline. It owns no industrial business rule.
try
{
    builder.Services.AddDmoInfrastructure(builder.Configuration);
}
catch (DatabaseConfigurationException ex)
{
    // Fail loudly and legibly. The host must never start against a missing or invalid
    // database configuration, and must never substitute a default connection.
    Console.Error.WriteLine($"Startup failed: {ex.Message}");
    return StartupCommands.FailureExitCode;
}

var app = builder.Build();

// `--migrate` (or `migrate`) is a technical entry point that applies pending migrations and
// exits. At P1-T01 no product migration exists, so it applies nothing and reports that.
if (StartupCommands.IsMigrationCommand(args))
{
    return await StartupCommands.RunMigrateAsync(app.Services, app.Logger);
}

app.MapTechnicalEndpoints();

return await StartupCommands.RunHostAsync(app);
