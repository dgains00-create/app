using System.Runtime.CompilerServices;
using DMO.Infrastructure.Configuration;

namespace DMO.IntegrationTests;

/// <summary>Loads local repository configuration before environment-gated tests execute.</summary>
internal static class LocalEnvironmentBootstrap
{
    [ModuleInitializer]
    internal static void Initialize() => LocalEnvironmentFile.LoadFromRepositoryRoot();
}
