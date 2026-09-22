namespace DMO.Infrastructure.Configuration;

/// <summary>
/// Loads the repository-root <c>.env</c> file into the current process for local
/// development and explicitly opted-in integration tests.
/// </summary>
/// <remarks>
/// Existing process variables always win, matching the normal .NET configuration
/// precedence. The loader is deliberately small and dependency-free; it never logs names or
/// values and it does not search outside the repository containing <c>DMO.slnx</c>.
/// </remarks>
public static class LocalEnvironmentFile
{
    private const string SolutionFileName = "DMO.slnx";
    private const string EnvironmentFileName = ".env";

    /// <summary>
    /// Finds the repository root from the current directory or application base directory and
    /// loads its <c>.env</c> file when present.
    /// </summary>
    /// <returns><c>true</c> when a file was found and parsed; otherwise <c>false</c>.</returns>
    public static bool LoadFromRepositoryRoot()
    {
        var repositoryRoot = FindRepositoryRoot(Directory.GetCurrentDirectory())
            ?? FindRepositoryRoot(AppContext.BaseDirectory);

        return repositoryRoot is not null
            && Load(Path.Combine(repositoryRoot, EnvironmentFileName));
    }

    /// <summary>Loads one explicit environment file without replacing existing variables.</summary>
    /// <param name="path">Absolute or relative path to the environment file.</param>
    /// <returns><c>true</c> when the file exists and was parsed; otherwise <c>false</c>.</returns>
    public static bool Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            return false;
        }

        var lineNumber = 0;
        foreach (var rawLine in File.ReadLines(path))
        {
            lineNumber++;
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.Ordinal))
            {
                line = line["export ".Length..].TrimStart();
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                throw new FormatException($"Invalid .env declaration at line {lineNumber}.");
            }

            var name = line[..separator].Trim();
            if (!IsValidVariableName(name))
            {
                throw new FormatException($"Invalid .env variable name at line {lineNumber}.");
            }

            var value = Unquote(line[(separator + 1)..].Trim());

            if (Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process) is null)
            {
                Environment.SetEnvironmentVariable(
                    name,
                    value,
                    EnvironmentVariableTarget.Process);
            }
        }

        return true;
    }

    private static string? FindRepositoryRoot(string startPath)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startPath));
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static bool IsValidVariableName(string name)
    {
        if (name.Length == 0 || !(name[0] == '_' || char.IsAsciiLetter(name[0])))
        {
            return false;
        }

        return name[1..].All(character => character == '_' || char.IsAsciiLetterOrDigit(character));
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"')
                || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }
}
