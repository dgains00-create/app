using DMO.Infrastructure.Configuration;

namespace DMO.UnitTests.Configuration;

public sealed class LocalEnvironmentFileTests
{
    [Fact]
    public void Load_WhenFileIsAbsent_ReturnsFalse()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dmo-missing-{Guid.NewGuid():N}", ".env");

        Assert.False(LocalEnvironmentFile.Load(path));
    }

    [Fact]
    public void Load_ParsesSupportedDeclarations_WithoutReplacingProcessValues()
    {
        var token = Guid.NewGuid().ToString("N");
        var plainName = $"DMO_TEST_PLAIN_{token}";
        var quotedName = $"DMO_TEST_QUOTED_{token}";
        var exportedName = $"DMO_TEST_EXPORTED_{token}";
        var existingName = $"DMO_TEST_EXISTING_{token}";
        var directory = Directory.CreateTempSubdirectory("dmo-env-");
        var path = Path.Combine(directory.FullName, ".env");

        try
        {
            Environment.SetEnvironmentVariable(existingName, "process", EnvironmentVariableTarget.Process);
            File.WriteAllLines(path,
            [
                "# ignored",
                $"{plainName}=plain=value",
                $"{quotedName}=\"quoted value\"",
                $"export {exportedName}='exported value'",
                $"{existingName}=file",
            ]);

            Assert.True(LocalEnvironmentFile.Load(path));
            Assert.Equal("plain=value", Environment.GetEnvironmentVariable(plainName));
            Assert.Equal("quoted value", Environment.GetEnvironmentVariable(quotedName));
            Assert.Equal("exported value", Environment.GetEnvironmentVariable(exportedName));
            Assert.Equal("process", Environment.GetEnvironmentVariable(existingName));
        }
        finally
        {
            foreach (var name in new[] { plainName, quotedName, exportedName, existingName })
            {
                Environment.SetEnvironmentVariable(name, null, EnvironmentVariableTarget.Process);
            }

            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void Load_InvalidDeclaration_ReportsOnlyTheLineNumber()
    {
        var directory = Directory.CreateTempSubdirectory("dmo-env-");
        var path = Path.Combine(directory.FullName, ".env");

        try
        {
            File.WriteAllLines(path, ["VALID=value", "not-a-declaration"]);

            var exception = Assert.Throws<FormatException>(() => LocalEnvironmentFile.Load(path));

            Assert.Equal("Invalid .env declaration at line 2.", exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("VALID", null, EnvironmentVariableTarget.Process);
            directory.Delete(recursive: true);
        }
    }
}
