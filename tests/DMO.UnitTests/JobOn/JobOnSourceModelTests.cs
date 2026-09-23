using DMO.Infrastructure.Persistence.Entities;


namespace DMO.UnitTests.JobOn;

/// <summary>
/// P2-T04 source/model rows JOB23 and CTX10 (contract §20.2/§20.3, AC-33, AC-46).
/// </summary>
/// <remarks>
/// Both rows are static checks over the persistence model and the P2-T04 production sources:
/// <list type="bullet">
/// <item><b>JOB23</b> — no EF query exists outside the contracted repositories, and no module
/// queries another module's table;</item>
/// <item><b>CTX10</b> — the three context tables declare exactly the contracted columns; none of
/// the excluded facts has a column.</item>
/// </list>
/// </remarks>
public sealed class JobOnSourceModelTests
{
    /// <summary>JOB23 (AC-46) — EF queries live only in the contracted repositories.</summary>
    [Fact]
    public void JOB23_NoEfQueryExistsOutsideTheContractedRepositories_AndNoModuleQueriesAnotherModulesTable()
    {
        // The application boundary reference points: the Web layer holds no persistence query.
        var webQueryReferences = new[]
        {
            "src/DMO.Web/Endpoints/JobOnEndpoints.cs",
            "src/DMO.Web/Endpoints/FerramentasEndpoints.cs",
            "src/DMO.Web/Pages/JobOn/Index.cshtml.cs",
            "src/DMO.Web/Pages/JobOn/View.cshtml.cs",
            "src/DMO.Web/Pages/JobOn/Create.cshtml.cs",
            "src/DMO.Web/Pages/JobOn/Edit.cshtml.cs",
            "src/DMO.Web/Pages/JobOn/Duplicate.cshtml.cs",
            "src/DMO.Web/Pages/Ferramentas/Tool.cshtml.cs",
            "src/DMO.Web/Pages/JobOn/JobOnToolPickerAdapter.cs",
        };

        foreach (var path in webQueryReferences)
        {
            var source = Read(path);

            Assert.DoesNotContain("DmoDbContext", source, StringComparison.Ordinal);
            Assert.DoesNotContain(".Include(", source, StringComparison.Ordinal);
            Assert.DoesNotContain("FirstOrDefaultAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ToListAsync", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Set<ToolEntity>", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Set<JobOnEntity>", source, StringComparison.Ordinal);
        }

        // Every EF query in the P2-T04 production code sits in one of the two contracted
        // repositories or in the Job On module's own dependency probe (its own table).
        var repositorySources = new[]
        {
            Read("src/DMO.Infrastructure/Persistence/ToolRepository.cs"),
            Read("src/DMO.Infrastructure/Persistence/JobOnRepository.cs"),
            Read("src/DMO.Infrastructure/Persistence/JobOnLineageDependencyProbe.cs"),
        };

        foreach (var source in repositorySources)
        {
            Assert.Contains("DmoDbContext", source, StringComparison.Ordinal);
        }

        // No other P2-T04 production file under src/ performs an EF query: a file that uses any
        // DB-set/async-LINQ token must be one of the three contracted query holders. The
        // DesignTimeDmoDbContextFactory and the context itself reference the context type but
        // query nothing, so the scan keys on query tokens, not on the type name.
        var queryTokens = new[]
        {
            "_context.Set<", "context.Set<", "DbSet<", "Include(", "FirstOrDefaultAsync", "ToListAsync", "SingleOrDefaultAsync",
            "ExecuteDeleteAsync", "FromSql",
        };

        foreach (var path in AllP2T04SourcePaths())
        {
            var source = Read(path);

            if (source == repositorySources[0] || source == repositorySources[1] || source == repositorySources[2])
            {
                continue;
            }

            foreach (var token in queryTokens)
            {
                Assert.DoesNotContain(token, source, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>CTX10 (AC-33) — the context tables declare exactly the contracted columns.</summary>
    [Fact]
    public void CTX10_TheThreeContextTablesDeclareExactlyTheContractedColumns()
    {
        var cm = typeof(CmContextEntity).GetProperties().Select(property => property.Name).ToHashSet();
        var mf = typeof(MfContextEntity).GetProperties().Select(property => property.Name).ToHashSet();
        var bq = typeof(BqContextEntity).GetProperties().Select(property => property.Name).ToHashSet();

        // Exactly the contracted column set per context table.
        Assert.Equal(
            new[]
            {
                nameof(CmContextEntity.CmId), nameof(CmContextEntity.JobOnId), nameof(CmContextEntity.ToolId),
                nameof(CmContextEntity.ToolType), nameof(CmContextEntity.ToolReference),
                nameof(CmContextEntity.ToolLot), nameof(CmContextEntity.CreatedAt), nameof(CmContextEntity.UpdatedAt),
            },
            cm);
        Assert.Equal(
            new[]
            {
                nameof(MfContextEntity.MfId), nameof(MfContextEntity.JobOnId), nameof(MfContextEntity.ToolId),
                nameof(MfContextEntity.ToolType), nameof(MfContextEntity.ToolReference),
                nameof(MfContextEntity.ToolLot), nameof(MfContextEntity.CreatedAt), nameof(MfContextEntity.UpdatedAt),
            },
            mf);
        Assert.Equal(
            new[]
            {
                nameof(BqContextEntity.BqId), nameof(BqContextEntity.JobOnId), nameof(BqContextEntity.ToolId),
                nameof(BqContextEntity.ToolType), nameof(BqContextEntity.ToolReference),
                nameof(BqContextEntity.ToolLot), nameof(BqContextEntity.CreatedAt), nameof(BqContextEntity.UpdatedAt),
            },
            bq);

        // None of the excluded facts has a column on any context table.
        foreach (var excluded in new[]
                 {
                     "Quantity", "Processo", "OperationalNote", "Note", "Baffle", "Calote",
                     "Measurement", "Boquilha", "Machine", "Status", "State", "Version",
                 })
        {
            Assert.DoesNotContain(cm, member => string.Equals(member, excluded, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(mf, member => string.Equals(member, excluded, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(bq, member => string.Equals(member, excluded, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DMO.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private static IEnumerable<string> AllP2T04SourcePaths()
    {
        var applicationFiles = Directory.EnumerateFiles(
            Path.Combine(RepositoryRoot(), "src", "DMO.Application"),
            "*.cs",
            SearchOption.AllDirectories).Select(Relative);

        var infrastructureFiles = Directory.EnumerateFiles(
            Path.Combine(RepositoryRoot(), "src", "DMO.Infrastructure", "Persistence"),
            "*.cs",
            SearchOption.AllDirectories).Select(Relative);

        var domainFiles = Directory.EnumerateFiles(
            Path.Combine(RepositoryRoot(), "src", "DMO.Domain"),
            "*.cs",
            SearchOption.AllDirectories).Select(Relative);

        return applicationFiles
            .Concat(infrastructureFiles)
            .Concat(domainFiles)
            .Distinct()
            .Where(path => !path.Contains("DmoDbContext", StringComparison.OrdinalIgnoreCase))
            .Where(path => path.Contains("Tool", StringComparison.OrdinalIgnoreCase) ||
                           path.Contains("JobOn", StringComparison.OrdinalIgnoreCase) ||
                           path.Contains("Context", StringComparison.OrdinalIgnoreCase) ||
                           path.Contains("Machine", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static string Relative(string path) =>
        path.Replace(RepositoryRoot(), string.Empty, StringComparison.Ordinal).TrimStart('\\');
}