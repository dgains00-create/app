using DMO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

// Test-only raw SQL: fixed, test-owned tokens against a disposable database. Analyzer EF1003
// suppressed (the same convention as the settings integration tests' ClearSettingsTablesAsync).
#pragma warning disable EF1003

namespace DMO.IntegrationTests.Persistence;

/// <summary>
/// Shared restore helper for the env-gated glass-density DB tests: <c>glass_density_settings</c>
/// is SEEDED by migration 005 and must ALWAYS hold exactly the two canonical rows (NNPB 2.4027 /
/// PS 2.4231 at version 1). Every DB test that writes the table restores the canonical state in
/// <c>finally</c> (and at the start, defensively) so the shared disposable database never leaks a
/// mutated value into a later test — the same discipline as
/// <c>ControloSettingsRepositoryIntegrationTests.ClearSettingsTablesAsync</c>.
/// </summary>
public static class GlassDensityTestState
{
    /// <summary>Restores the table to exactly the two provenance-backed canonical rows.</summary>
    public static async Task RestoreAsync(DmoDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("DELETE FROM \"glass_density_settings\"");

        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"glass_density_settings\" (processo, density_g_cm3, version) VALUES " +
            "('NNPB', 2.4027, 1), ('PS', 2.4231, 1)");
    }
}