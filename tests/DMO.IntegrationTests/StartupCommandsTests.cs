using DMO.Web.Startup;

namespace DMO.IntegrationTests;

/// <summary>
/// Proposed P1-T01 test — the migration entry point is selected only by its own argument.
/// </summary>
/// <remarks>
/// PROPOSED — NOT EXECUTED. Awaiting Architect review before first execution.
/// See <c>docs/PROPOSED_TESTS_P1-T01.md</c> for the full test protocol record.
/// </remarks>
public sealed class StartupCommandsTests
{
    [Theory]
    [InlineData("migrate")]
    [InlineData("MIGRATE")]
    [InlineData("--migrate")]
    [InlineData("--MIGRATE")]
    public void IsMigrationCommand_WhenMigrationArgumentFirst_IsTrue(string argument)
    {
        Assert.True(StartupCommands.IsMigrationCommand([argument]));
    }

    [Fact]
    public void IsMigrationCommand_WhenNoArguments_IsFalse()
    {
        // Required non-effect: starting the host with no arguments must not run migrations.
        Assert.False(StartupCommands.IsMigrationCommand([]));
        Assert.False(StartupCommands.IsMigrationCommand(null));
    }

    [Fact]
    public void IsMigrationCommand_WhenOnlyLaterArgument_IsFalse()
    {
        // The run must not treat a following value as the command.
        Assert.False(StartupCommands.IsMigrationCommand(["--environment", "migrate"]));
    }

    [Fact]
    public void IsMigrationCommand_WhenHostArgumentsPresent_IsFalse()
    {
        // Required non-effect: ordinary host startup arguments must not trigger a migration run.
        Assert.False(StartupCommands.IsMigrationCommand(["--urls", "http://localhost:5280"]));
        Assert.False(StartupCommands.IsMigrationCommand(["--environment", "Production"]));
    }
}
