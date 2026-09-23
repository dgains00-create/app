using System.Data;
using DMO.Application.ControloCreate;
using DMO.Application.Persistence;
using DMO.Application.Repositories;
using DMO.Domain.Controlo;
using DMO.Infrastructure.Persistence;
using DMO.IntegrationTests.ControloCreate;
using DMO.Web.Endpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

// Test-only raw SQL: every interpolated value is a fixed, test-owned token (row identifiers
// derived from a fresh Guid) against a disposable database. Analyzer EF1003 suppressed.
#pragma warning disable EF1003

namespace DMO.IntegrationTests.Persistence;

/// <summary>
/// P2-T05 env-gated integration test — <c>Controlo_Create → Definições</c> over the disposable
/// PostgreSQL database: repairer register, machine assignments, PDF-directory setting (with the real
/// server-side probe), email lists and email templates, with the version guards of §19.
/// </summary>
/// <remarks>
/// Authority: P2-T05 contract §9–§14 (settings areas), §18 (transactions/replace-all), §19
/// (concurrency) and §30 rows REP2/REP3/REP5, MAC1–MAC5, SET1, SET2, SET4–SET7, SET9, SET11
/// (AC-D2/AC-D3/AC-E1/AC-E2/AC-E3/AC-F1/AC-F2/AC-F3/AC-F4/AC-F5/AC-F7/AC-F9). The fixed machine
/// keys (B1..C3) and the single-row PDF setting make the shared DB determinism mandatory: every
/// test clears the settings tables first (dependency order).
/// </remarks>
[Collection(PersistenceDatabaseCollection.Name)]
public sealed class ControloSettingsRepositoryIntegrationTests
{
    /// <summary>Creates the settings service over the real repositories and a fixed probe verdict.</summary>
    private static ControloDefinicoesService Definicoes(
        DmoDbContext context,
        IPdfDirectoryProbe? probe = null) => new(
        new RepairerRepository(context),
        new MachineRepairerAssignmentRepository(context),
        new PdfDirectorySettingsRepository(context),
        new EmailListRepository(context),
        new EmailTemplateRepository(context),
        probe ?? new FixedPdfDirectoryProbe());

    /// <summary>The six settled machine codes, in the settled order.</summary>
    private static readonly string[] Machines = ["B1", "B2", "B3", "C1", "C2", "C3"];

    /// <summary>
    /// REP2 (AC-D2): a repairer rename keeps the SAME <c>repairer_id</c>, increments the version, is
    /// visible in the list and a stale rename is refused with <c>StaleVersion</c>.
    /// </summary>
    [SkippableFact]
    public async Task REP2_ANameChangeKeepsTheSameIdAndAStaleVersionIsRefused()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);
        await ClearSettingsTablesAsync(context);

        var token = Guid.NewGuid().ToString("N");

        try
        {
            var services = Definicoes(context);
            var original = $"rep-{token}";
            var renamedTo = $"rep-{token}-renamed";

            var created = Assert.IsType<SettingsResult.RepairerCreated>(await services.CreateRepairerAsync(
                new CreateRepairerCommand(original), CancellationToken.None));
            Assert.Equal(1, created.Version);

            var renamed = Assert.IsType<SettingsResult.RepairerRenamed>(await services.RenameRepairerAsync(
                new RenameRepairerCommand(created.RepairerId, ExpectedVersion: 1, renamedTo),
                CancellationToken.None));
            Assert.Equal(created.RepairerId, renamed.RepairerId); // the SAME id retains the row
            Assert.Equal(2, renamed.Version);

            var list = Assert.IsType<SettingsResult.RepairersFound>(await services.ListRepairersAsync(
                CancellationToken.None)).Repairers;
            Assert.Contains(list, item =>
                item.RepairerId.Value == created.RepairerId
                && item.Name == renamedTo
                && item.Version == 2);

            // A stale rename (observed version 1, current version 2) is refused; nothing written.
            var stale = Assert.IsType<SettingsResult.Refused>(await services.RenameRepairerAsync(
                new RenameRepairerCommand(created.RepairerId, ExpectedVersion: 1, $"rep-{token}-stale"),
                CancellationToken.None));
            Assert.Equal(SettingsRefusalReason.StaleVersion, stale.Reason);
        }
        finally
        {
            await ClearSettingsTablesAsync(context);
        }
    }

    /// <summary>
    /// REP3 (AC-D3): no delete route exists for repairers — the repository contract carries no
    /// delete member and the endpoints declare no delete request carrier.
    /// </summary>
    [SkippableFact]
    public async Task REP3_NoDeleteRouteExistsForRepairers()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        var repositoryMethods = typeof(IRepairerRepository).GetMethods()
            .Select(method => method.Name)
            .ToList();
        Assert.DoesNotContain(repositoryMethods, name => name.Contains("Delete", StringComparison.Ordinal));

        var endpointTypes = typeof(ControloDefinicoesEndpoints).GetNestedTypes();
        Assert.DoesNotContain(endpointTypes, type =>
            type.Name.Contains("DeleteRepairer", StringComparison.Ordinal));

        await Task.CompletedTask;
    }

    /// <summary>
    /// REP5 (AC-D3): a repairer referenced by a machine assignment cannot be deleted — the RESTRICT
    /// foreign key rejects the raw delete with <c>23503</c>.
    /// </summary>
    [SkippableFact]
    public async Task REP5_ADeleteOfAReferencedRepairerIsRejectedByTheRestrictForeignKey()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);
        await ClearSettingsTablesAsync(context);

        try
        {
            var services = Definicoes(context);
            var repairerId = Assert.IsType<SettingsResult.RepairerCreated>(await services.CreateRepairerAsync(
                new CreateRepairerCommand($"rep-{Guid.NewGuid():N}"), CancellationToken.None)).RepairerId;

            Assert.IsType<SettingsResult.AssignmentSet>(await services.SetMachineAssignmentAsync(
                new SetMachineAssignmentCommand("B1", repairerId, null), CancellationToken.None));

            var rejected = await Assert.ThrowsAsync<PostgresException>(() =>
                context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM repairers WHERE repairer_id = @p",
                    new NpgsqlParameter("p", repairerId)));

            Assert.Equal("23503", rejected.SqlState);
            Assert.Equal("FK_machine_repairer_assignments_repairers_repairer_id", rejected.ConstraintName);

            Assert.Equal(
                1,
                await CountAsync(context, "repairers WHERE repairer_id = @p", new NpgsqlParameter("p", repairerId)));
        }
        finally
        {
            await ClearSettingsTablesAsync(context);
        }
    }

    /// <summary>
    /// MAC1 (AC-E1): every settled machine code accepts its first assignment at version 1; any other
    /// machine token is refused with <c>MACHINE_UNKNOWN</c>.
    /// </summary>
    [SkippableFact]
    public async Task MAC1_EverySettledMachineAcceptsItsFirstAssignmentAndUnknownMachinesAreRefused()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);
        await ClearSettingsTablesAsync(context);

        var token = Guid.NewGuid().ToString("N");

        try
        {
            var services = Definicoes(context);

            foreach (var machine in Machines)
            {
                var repairerId = Assert.IsType<SettingsResult.RepairerCreated>(await services.CreateRepairerAsync(
                    new CreateRepairerCommand($"rep-{machine}-{token}"), CancellationToken.None)).RepairerId;

                var set = Assert.IsType<SettingsResult.AssignmentSet>(await services.SetMachineAssignmentAsync(
                    new SetMachineAssignmentCommand(machine, repairerId, null), CancellationToken.None));
                Assert.Equal(machine, set.Machine);
                Assert.Equal(1, set.Version);
            }

            foreach (var invalid in new[] { "B4", "Linha B", "C", "B" })
            {
                var refused = Assert.IsType<SettingsResult.ValidationFailed>(await services.SetMachineAssignmentAsync(
                    new SetMachineAssignmentCommand(invalid, Guid.NewGuid(), null), CancellationToken.None));
                Assert.Contains(ControloDefinicoesValidationErrors.MachineUnknown, refused.Errors);
            }
        }
        finally
        {
            await ClearSettingsTablesAsync(context);
        }
    }

    /// <summary>
    /// MAC2 (AC-E2): changing ONE machine's assignment touches only that machine's row; the other
    /// five assignments remain exactly as they were.
    /// </summary>
    [SkippableFact]
    public async Task MAC2_ChangingOneMachineChangesOnlyThatMachinesAssignment()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);
        await ClearSettingsTablesAsync(context);

        var token = Guid.NewGuid().ToString("N");

        try
        {
            var services = Definicoes(context);

            var original = new Dictionary<string, Guid>();
            foreach (var machine in Machines)
            {
                var repairerId = Assert.IsType<SettingsResult.RepairerCreated>(await services.CreateRepairerAsync(
                    new CreateRepairerCommand($"rep-{machine}-{token}"), CancellationToken.None)).RepairerId;
                original[machine] = repairerId;

                Assert.IsType<SettingsResult.AssignmentSet>(await services.SetMachineAssignmentAsync(
                    new SetMachineAssignmentCommand(machine, repairerId, null), CancellationToken.None));
            }

            var list = Assert.IsType<SettingsResult.AssignmentsFound>(await services.ListMachineAssignmentsAsync(
                CancellationToken.None)).Assignments;
            Assert.Equal(6, list.Count);

            // B1 moves to a NEW repairer (observed version 1); the edit touches B1 only.
            var replacement = Assert.IsType<SettingsResult.RepairerCreated>(await services.CreateRepairerAsync(
                new CreateRepairerCommand($"rep-b1-new-{token}"), CancellationToken.None)).RepairerId;

            var changed = Assert.IsType<SettingsResult.AssignmentSet>(await services.SetMachineAssignmentAsync(
                new SetMachineAssignmentCommand("B1", replacement, ExpectedVersion: 1), CancellationToken.None));
            Assert.Equal("B1", changed.Machine);
            Assert.Equal(2, changed.Version);

            var after = Assert.IsType<SettingsResult.AssignmentsFound>(await services.ListMachineAssignmentsAsync(
                CancellationToken.None)).Assignments;

            Assert.Equal(6, after.Count);
            var byMachine = after.ToDictionary(assignment => assignment.Machine.Value);
            Assert.Equal(replacement, byMachine["B1"].RepairerId.Value);

            foreach (var machine in Machines.Skip(1))
            {
                Assert.Equal(original[machine], byMachine[machine].RepairerId.Value);
                Assert.Equal(1, byMachine[machine].Version);
            }
        }
        finally
        {
            await ClearSettingsTablesAsync(context);
        }
    }

    /// <summary>
    /// MAC3 (AC-E2): re-setting a machine with a different repairer updates that ONE row in place
    /// (version bumped, repairer changed); other machines stay untouched.
    /// </summary>
    [SkippableFact]
    public async Task MAC3_ResettingOneMachineUpdatesOnlyThatRowInPlace()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);
        await ClearSettingsTablesAsync(context);

        var token = Guid.NewGuid().ToString("N");

        try
        {
            var services = Definicoes(context);
            var assignments = new MachineRepairerAssignmentRepository(context);

            var repairerA = Assert.IsType<SettingsResult.RepairerCreated>(await services.CreateRepairerAsync(
                new CreateRepairerCommand($"rep-a-{token}"), CancellationToken.None)).RepairerId;
            var repairerB = Assert.IsType<SettingsResult.RepairerCreated>(await services.CreateRepairerAsync(
                new CreateRepairerCommand($"rep-b-{token}"), CancellationToken.None)).RepairerId;
            var repairerC = Assert.IsType<SettingsResult.RepairerCreated>(await services.CreateRepairerAsync(
                new CreateRepairerCommand($"rep-c-{token}"), CancellationToken.None)).RepairerId;

            Assert.IsType<SettingsResult.AssignmentSet>(await services.SetMachineAssignmentAsync(
                new SetMachineAssignmentCommand("B1", repairerA, null), CancellationToken.None));
            Assert.IsType<SettingsResult.AssignmentSet>(await services.SetMachineAssignmentAsync(
                new SetMachineAssignmentCommand("B2", repairerC, null), CancellationToken.None));

            var b1Before = await assignments.GetByMachineAsync("B1", CancellationToken.None);
            var b2Before = await assignments.GetByMachineAsync("B2", CancellationToken.None);
            Assert.Equal(1, b1Before!.Version);
            Assert.Equal(1, b2Before!.Version);

            // B1 → B: only the B1 row changes.
            var updated = Assert.IsType<SettingsResult.AssignmentSet>(await services.SetMachineAssignmentAsync(
                new SetMachineAssignmentCommand("B1", repairerB, ExpectedVersion: 1), CancellationToken.None));
            Assert.Equal(2, updated.Version);

            var b1After = await assignments.GetByMachineAsync("B1", CancellationToken.None);
            var b2After = await assignments.GetByMachineAsync("B2", CancellationToken.None);

            Assert.Equal(repairerB, b1After!.RepairerId.Value);
            Assert.Equal(2, b1After.Version);
            Assert.Equal(repairerC, b2After!.RepairerId.Value); // untouched
            Assert.Equal(1, b2After.Version); // untouched
        }
        finally
        {
            await ClearSettingsTablesAsync(context);
        }
    }

    /// <summary>
    /// MAC4 (AC-E2): clearing one machine removes ONLY its row; the other five assignments remain.
    /// </summary>
    [SkippableFact]
    public async Task MAC4_ClearingOneMachineRemovesOnlyThatRow()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);
        await ClearSettingsTablesAsync(context);

        var token = Guid.NewGuid().ToString("N");

        try
        {
            var services = Definicoes(context);

            foreach (var machine in Machines)
            {
                var repairerId = Assert.IsType<SettingsResult.RepairerCreated>(await services.CreateRepairerAsync(
                    new CreateRepairerCommand($"rep-{machine}-{token}"), CancellationToken.None)).RepairerId;

                Assert.IsType<SettingsResult.AssignmentSet>(await services.SetMachineAssignmentAsync(
                    new SetMachineAssignmentCommand(machine, repairerId, null), CancellationToken.None));
            }

            var cleared = Assert.IsType<SettingsResult.AssignmentCleared>(await services.ClearMachineAssignmentAsync(
                new ClearMachineAssignmentCommand("C2", ExpectedVersion: 1), CancellationToken.None));
            Assert.Equal("C2", cleared.Machine);

            var list = Assert.IsType<SettingsResult.AssignmentsFound>(await services.ListMachineAssignmentsAsync(
                CancellationToken.None)).Assignments;

            Assert.Equal(5, list.Count);
            Assert.DoesNotContain(list, assignment => assignment.Machine.Value == "C2");
            Assert.Equal(5, Machines.Count(machine => list.Any(assignment => assignment.Machine.Value == machine)));
        }
        finally
        {
            await ClearSettingsTablesAsync(context);
        }
    }

    /// <summary>
    /// MAC5 (AC-E3): the resolution read returns exactly the current assignment of one machine, and
    /// <c>null</c> once the assignment is cleared (absent row = none assigned).
    /// </summary>
    [SkippableFact]
    public async Task MAC5_TheResolutionReadReturnsTheCurrentAssignmentAndNoneAfterClearing()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);
        await ClearSettingsTablesAsync(context);

        var token = Guid.NewGuid().ToString("N");

        try
        {
            var services = Definicoes(context);
            var assignments = new MachineRepairerAssignmentRepository(context);

            var repairerId = Assert.IsType<SettingsResult.RepairerCreated>(await services.CreateRepairerAsync(
                new CreateRepairerCommand($"rep-{token}"), CancellationToken.None)).RepairerId;

            Assert.IsType<SettingsResult.AssignmentSet>(await services.SetMachineAssignmentAsync(
                new SetMachineAssignmentCommand("B1", repairerId, null), CancellationToken.None));

            var current = await assignments.GetByMachineAsync("B1", CancellationToken.None);
            Assert.NotNull(current);
            Assert.Equal(repairerId, current!.RepairerId.Value);
            Assert.Equal(1, current.Version);

            Assert.IsType<SettingsResult.AssignmentCleared>(await services.ClearMachineAssignmentAsync(
                new ClearMachineAssignmentCommand("B1", ExpectedVersion: 1), CancellationToken.None));

            Assert.Null(await assignments.GetByMachineAsync("B1", CancellationToken.None));
        }
        finally
        {
            await ClearSettingsTablesAsync(context);
        }
    }

    /// <summary>
    /// SET1 (AC-F1): the PDF-directory setting starts as the explicit not-configured state, is
    /// configured, changed and version-guarded; blank and relative values are refused before any
    /// write.
    /// </summary>
    [SkippableFact]
    public async Task SET1_ThePdfDirectorySettingIsVersionGuardedAndValidated()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);
        await ClearSettingsTablesAsync(context);

        var token = Guid.NewGuid().ToString("N");
        var path = Path.Combine(Path.GetTempPath(), $"dmo-p2t05-{token}");
        var changedPath = Path.Combine(Path.GetTempPath(), $"dmo-p2t05-{token}-changed");

        try
        {
            var services = Definicoes(context);

            // Not configured yet: an explicit state, never an error.
            var notConfigured = Assert.IsType<SettingsResult.PdfDirectoryFound>(await services.GetPdfDirectoryAsync(
                CancellationToken.None));
            Assert.Null(notConfigured.View);

            // First set (no observed version): version 1.
            var first = Assert.IsType<SettingsResult.PdfDirectorySaved>(await services.SetPdfDirectoryAsync(
                new SetPdfDirectoryCommand(path, ExpectedVersion: null), CancellationToken.None));
            Assert.Equal(1, first.Version);

            var found = Assert.IsType<SettingsResult.PdfDirectoryFound>(await services.GetPdfDirectoryAsync(
                CancellationToken.None));
            Assert.Equal(path, found.View!.BaseDirectory);
            Assert.Equal(1, found.View.Version);

            // Change with the observed version: exactly one increment.
            var second = Assert.IsType<SettingsResult.PdfDirectorySaved>(await services.SetPdfDirectoryAsync(
                new SetPdfDirectoryCommand(changedPath, ExpectedVersion: 1), CancellationToken.None));
            Assert.Equal(2, second.Version);

            // Stale version: refused, nothing written.
            var stale = Assert.IsType<SettingsResult.Refused>(await services.SetPdfDirectoryAsync(
                new SetPdfDirectoryCommand(changedPath, ExpectedVersion: 1), CancellationToken.None));
            Assert.Equal(SettingsRefusalReason.StaleVersion, stale.Reason);

            // Blank and relative values are contracted validation failures.
            var blank = Assert.IsType<SettingsResult.ValidationFailed>(await services.SetPdfDirectoryAsync(
                new SetPdfDirectoryCommand("   ", ExpectedVersion: null), CancellationToken.None));
            Assert.Contains(ControloDefinicoesValidationErrors.DirectoryRequired, blank.Errors);

            var relative = Assert.IsType<SettingsResult.ValidationFailed>(await services.SetPdfDirectoryAsync(
                new SetPdfDirectoryCommand("relative/path", ExpectedVersion: null), CancellationToken.None));
            Assert.Contains(ControloDefinicoesValidationErrors.DirectoryInvalid, relative.Errors);
        }
        finally
        {
            await ClearSettingsTablesAsync(context);
        }
    }

    /// <summary>
    /// SET2 (AC-F2): the real server-side probe (<see cref="ServerHostPdfDirectoryProbe"/>) over
    /// real temp directories reports <c>Ok</c>, <c>DirectoryNotFound</c> and <c>NotADirectory</c>,
    /// and the probe never leaves a file behind.
    /// </summary>
    [SkippableFact]
    public async Task SET2_TheServerSideProbeReportsOkNotFoundAndNotADirectoryOverRealPaths()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);
        await ClearSettingsTablesAsync(context);

        var token = Guid.NewGuid().ToString("N");
        var directory = Path.Combine(Path.GetTempPath(), $"dmo-p2t05-{token}");
        var missing = Path.Combine(directory, "missing-sub");
        var filePath = Path.Combine(directory, "not-a-directory.txt");

        try
        {
            Directory.CreateDirectory(directory);

            var services = Definicoes(context, new ServerHostPdfDirectoryProbe());

            // No row configured: the explicit not-configured state.
            var notConfigured = Assert.IsType<SettingsResult.PdfDirectoryCheck>(await services.CheckPdfDirectoryAsync(
                CancellationToken.None));
            Assert.Equal(PdfDirectoryCheckState.NotConfigured, notConfigured.Check.State);

            // An existing directory is read/write-reachable: Ok, and nothing is left behind.
            Assert.IsType<SettingsResult.PdfDirectorySaved>(await services.SetPdfDirectoryAsync(
                new SetPdfDirectoryCommand(directory, ExpectedVersion: null), CancellationToken.None));
            var ok = Assert.IsType<SettingsResult.PdfDirectoryCheck>(await services.CheckPdfDirectoryAsync(
                CancellationToken.None));
            Assert.Equal(PdfDirectoryCheckState.Ok, ok.Check.State);
            Assert.Empty(Directory.EnumerateFileSystemEntries(directory));

            // The file used for the NotADirectory case is created only AFTER the emptiness proof,
            // so the two evidences never interfere.
            File.WriteAllText(filePath, string.Empty);

            // A missing path is directory-not-found.
            Assert.IsType<SettingsResult.PdfDirectorySaved>(await services.SetPdfDirectoryAsync(
                new SetPdfDirectoryCommand(missing, ExpectedVersion: 1), CancellationToken.None));
            var notFound = Assert.IsType<SettingsResult.PdfDirectoryCheck>(await services.CheckPdfDirectoryAsync(
                CancellationToken.None));
            Assert.Equal(PdfDirectoryCheckState.DirectoryNotFound, notFound.Check.State);

            // An existing FILE path is not-a-directory.
            Assert.IsType<SettingsResult.PdfDirectorySaved>(await services.SetPdfDirectoryAsync(
                new SetPdfDirectoryCommand(filePath, ExpectedVersion: 2), CancellationToken.None));
            var notADirectory = Assert.IsType<SettingsResult.PdfDirectoryCheck>(await services.CheckPdfDirectoryAsync(
                CancellationToken.None));
            Assert.Equal(PdfDirectoryCheckState.NotADirectory, notADirectory.Check.State);
        }
        finally
        {
            await ClearSettingsTablesAsync(context);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            if (Directory.Exists(directory))
            {
                try
                {
                    Directory.Delete(directory, recursive: true);
                }
                catch (IOException)
                {
                    // The probe file may still be transiently held; the directory cleanup is best-effort.
                }
            }
        }
    }

    /// <summary>
    /// SET4/SET5 (AC-F3): an email-list update replaces the COMPLETE recipient set atomically — no
    /// partial set is ever observable — and increments the list version exactly once.
    /// </summary>
    [SkippableFact]
    public async Task SET4_5_AListUpdateReplacesTheWholeRecipientSetAndBumpsOnce()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);
        await ClearSettingsTablesAsync(context);

        var token = Guid.NewGuid().ToString("N");
        var name = $"list-{token}";

        try
        {
            var services = Definicoes(context);

            var created = Assert.IsType<SettingsResult.EmailListCreated>(await services.CreateEmailListAsync(
                new CreateEmailListCommand(name, ["a@x.pt", "b@x.pt"]), CancellationToken.None));
            Assert.Equal(1, created.Version);

            var first = Assert.IsType<SettingsResult.EmailListFound>(await services.GetEmailListAsync(
                created.EmailListId, CancellationToken.None)).List;
            Assert.Equal(new[] { "a@x.pt", "b@x.pt" }, first.Recipients.Select(recipient => recipient.Address).ToArray());

            // Update with the complete new set: replace-all within one transaction.
            var updated = Assert.IsType<SettingsResult.EmailListUpdated>(await services.UpdateEmailListAsync(
                new UpdateEmailListCommand(created.EmailListId, ExpectedVersion: 1, name, ["c@x.pt"]),
                CancellationToken.None));
            Assert.Equal(2, updated.Version);

            var second = Assert.IsType<SettingsResult.EmailListFound>(await services.GetEmailListAsync(
                created.EmailListId, CancellationToken.None)).List;
            Assert.Equal(new[] { "c@x.pt" }, second.Recipients.Select(recipient => recipient.Address).ToArray());
            Assert.Equal(2, second.Version); // the version was incremented exactly once
        }
        finally
        {
            await ClearSettingsTablesAsync(context);
        }
    }

    /// <summary>
    /// SET6-DB (AC-F4): the unique list name maps to <c>DuplicateName</c>; the same address may
    /// appear in two lists; a duplicated address inside ONE list is refused with
    /// <c>ADDRESS_INVALID</c> (service path) and the database unique key is the backstop.
    /// </summary>
    [SkippableFact]
    public async Task SET6_Db_DuplicateNamesAndDuplicateInListAddressesAreRefused()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);
        await ClearSettingsTablesAsync(context);

        var token = Guid.NewGuid().ToString("N");

        try
        {
            var services = Definicoes(context);
            var first = Assert.IsType<SettingsResult.EmailListCreated>(await services.CreateEmailListAsync(
                new CreateEmailListCommand($"list-{token}-1", ["a@x.pt", "b@x.pt"]), CancellationToken.None));

            // A duplicate name is the typed duplicate-name result.
            var duplicateName = Assert.IsType<SettingsResult.DuplicateName>(await services.CreateEmailListAsync(
                new CreateEmailListCommand($"list-{token}-1", ["z@x.pt"]), CancellationToken.None));
            Assert.Equal($"list-{token}-1", duplicateName.Name);

            // The same address in a DIFFERENT list is legitimate.
            Assert.IsType<SettingsResult.EmailListCreated>(await services.CreateEmailListAsync(
                new CreateEmailListCommand($"list-{token}-2", ["a@x.pt"]), CancellationToken.None));

            // A duplicated address inside ONE list is refused before any write (service path).
            var duplicateAddress = Assert.IsType<SettingsResult.ValidationFailed>(await services.CreateEmailListAsync(
                new CreateEmailListCommand($"list-{token}-3", ["c@x.pt", "c@x.pt"]), CancellationToken.None));
            Assert.Contains(ControloDefinicoesValidationErrors.AddressInvalid, duplicateAddress.Errors);

            Assert.Equal(
                2,
                await CountAsync(
                    context,
                    "email_lists WHERE name LIKE @p",
                    new NpgsqlParameter("p", $"%{token}%")));

            // The database backstop: the unique (email_list_id, address) key rejects the raw insert.
            var listId = Guid.Parse(await ScalarAsync(
                context,
                "SELECT email_list_id::text FROM email_lists WHERE name = @p",
                new NpgsqlParameter("p", $"list-{token}-1")));

            var rejected = await Assert.ThrowsAsync<PostgresException>(() =>
                context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO email_list_recipients (email_list_id, address) VALUES (@list, 'b@x.pt')",
                    new NpgsqlParameter("list", listId)));

            Assert.Equal("23505", rejected.SqlState);
            Assert.Equal("email_list_recipients_list_address_key", rejected.ConstraintName);
        }
        finally
        {
            await ClearSettingsTablesAsync(context);
        }
    }

    /// <summary>
    /// SET7-DB (AC-F5): email templates persist the contracted document-type vocabulary, store null
    /// for a generic template, refuse unknown types and keep the body verbatim.
    /// </summary>
    [SkippableFact]
    public async Task SET7_Db_EmailTemplatesPersistTheDocumentTypeAndKeepTheBodyVerbatim()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);
        await ClearSettingsTablesAsync(context);

        var token = Guid.NewGuid().ToString("N");
        var body = $"Linha um\nLinha dois com  espaços  internos\nLinha três-{token}";

        try
        {
            var services = Definicoes(context);

            var created = Assert.IsType<SettingsResult.EmailTemplateCreated>(await services.CreateEmailTemplateAsync(
                new CreateEmailTemplateCommand($"tpl-{token}", $"subj-{token}", body, "peso"),
                CancellationToken.None));
            Assert.Equal(1, created.Version);

            var typed = Assert.IsType<SettingsResult.EmailTemplateFound>(await services.GetEmailTemplateAsync(
                created.EmailTemplateId, CancellationToken.None)).Template;
            Assert.Equal(EmailTemplateDocumentType.Peso, typed.DocumentType);
            Assert.Equal(body, typed.Body); // stored verbatim
            Assert.Equal($"subj-{token}", typed.Subject);

            // A null document type is the generic template: stored null.
            var generic = Assert.IsType<SettingsResult.EmailTemplateCreated>(await services.CreateEmailTemplateAsync(
                new CreateEmailTemplateCommand($"tpl-{token}-generic", $"subj-{token}-g", "corpo", null),
                CancellationToken.None));
            var genericFound = Assert.IsType<SettingsResult.EmailTemplateFound>(await services.GetEmailTemplateAsync(
                generic.EmailTemplateId, CancellationToken.None)).Template;
            Assert.Null(genericFound.DocumentType);

            // An unknown document type is refused before any write.
            var refused = Assert.IsType<SettingsResult.ValidationFailed>(await services.CreateEmailTemplateAsync(
                new CreateEmailTemplateCommand($"tpl-{token}-bad", "s", "b", "x"), CancellationToken.None));
            Assert.Contains(ControloDefinicoesValidationErrors.DocumentTypeUnknown, refused.Errors);
        }
        finally
        {
            await ClearSettingsTablesAsync(context);
        }
    }

    /// <summary>
    /// SET9-DB (AC-F7): list/template deletes require explicit confirmation; after a confirmed delete
    /// the record reads NotFound; a raw delete of a list that still owns recipients is rejected by
    /// the RESTRICT foreign key.
    /// </summary>
    [SkippableFact]
    public async Task SET9_Db_DeletesRequireConfirmationAndDependentsBlockByRestrict()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);
        await ClearSettingsTablesAsync(context);

        var token = Guid.NewGuid().ToString("N");

        try
        {
            var services = Definicoes(context);

            // ---- Lists.
            var listId = Assert.IsType<SettingsResult.EmailListCreated>(await services.CreateEmailListAsync(
                new CreateEmailListCommand($"list-{token}", ["a@x.pt"]), CancellationToken.None)).EmailListId;

            var unconfirmed = Assert.IsType<SettingsResult.ValidationFailed>(await services.DeleteEmailListAsync(
                new DeleteEmailListCommand(listId, ExpectedVersion: 1, DeleteConfirmed: false),
                CancellationToken.None));
            Assert.Contains(ControloDefinicoesValidationErrors.DeleteNotConfirmed, unconfirmed.Errors);

            Assert.IsType<SettingsResult.EmailListDeleted>(await services.DeleteEmailListAsync(
                new DeleteEmailListCommand(listId, ExpectedVersion: 1, DeleteConfirmed: true),
                CancellationToken.None));
            Assert.IsType<SettingsResult.NotFound>(await services.GetEmailListAsync(listId, CancellationToken.None));

            // ---- Templates.
            var templateId = Assert.IsType<SettingsResult.EmailTemplateCreated>(await services.CreateEmailTemplateAsync(
                new CreateEmailTemplateCommand($"tpl-{token}", "s", "b", null), CancellationToken.None)).EmailTemplateId;

            var templateUnconfirmed = Assert.IsType<SettingsResult.ValidationFailed>(await services.DeleteEmailTemplateAsync(
                new DeleteEmailTemplateCommand(templateId, ExpectedVersion: 1, DeleteConfirmed: false),
                CancellationToken.None));
            Assert.Contains(ControloDefinicoesValidationErrors.DeleteNotConfirmed, templateUnconfirmed.Errors);

            Assert.IsType<SettingsResult.EmailTemplateDeleted>(await services.DeleteEmailTemplateAsync(
                new DeleteEmailTemplateCommand(templateId, ExpectedVersion: 1, DeleteConfirmed: true),
                CancellationToken.None));
            Assert.IsType<SettingsResult.NotFound>(await services.GetEmailTemplateAsync(templateId, CancellationToken.None));

            // ---- RESTRICT backstop: a list that still owns recipients cannot be deleted raw.
            var protectedList = Assert.IsType<SettingsResult.EmailListCreated>(await services.CreateEmailListAsync(
                new CreateEmailListCommand($"list-{token}-protected", ["a@x.pt"]), CancellationToken.None)).EmailListId;

            var rejected = await Assert.ThrowsAsync<PostgresException>(() =>
                context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM email_lists WHERE email_list_id = @p",
                    new NpgsqlParameter("p", protectedList)));

            Assert.Equal("23503", rejected.SqlState);
            Assert.Equal("FK_email_list_recipients_email_lists_email_list_id", rejected.ConstraintName);
        }
        finally
        {
            await ClearSettingsTablesAsync(context);
        }
    }

    /// <summary>
    /// SET11 (AC-F9): the PDF-directory setting is version-guarded both by the explicit compare and
    /// at save time — the SaveTimeRaceInterceptor pattern applies the same race reproduction to
    /// <see cref="PdfDirectorySettingsRepository.SetAsync"/>.
    /// </summary>
    [SkippableFact]
    public async Task SET11_ThePdfDirectorySettingIsVersionGuardedExplicitlyAndAtSaveTime()
    {
        PersistenceTestDatabase.SkipIfNotConfigured();

        await using var context = PersistenceTestDatabase.CreateContext();
        await PersistenceTestDatabase.ApplyMigrationsAsync(context);
        await ClearSettingsTablesAsync(context);

        var token = Guid.NewGuid().ToString("N");
        var firstPath = Path.Combine(Path.GetTempPath(), $"dmo-p2t05-{token}");
        var secondPath = Path.Combine(Path.GetTempPath(), $"dmo-p2t05-{token}-b");

        try
        {
            var services = Definicoes(context);

            // (a) The explicit compare: a stale observed version is refused with StaleVersion.
            Assert.IsType<SettingsResult.PdfDirectorySaved>(await services.SetPdfDirectoryAsync(
                new SetPdfDirectoryCommand(firstPath, ExpectedVersion: null), CancellationToken.None));
            Assert.IsType<SettingsResult.PdfDirectorySaved>(await services.SetPdfDirectoryAsync(
                new SetPdfDirectoryCommand(secondPath, ExpectedVersion: 1), CancellationToken.None));

            var stale = Assert.IsType<SettingsResult.Refused>(await services.SetPdfDirectoryAsync(
                new SetPdfDirectoryCommand(secondPath, ExpectedVersion: 1), CancellationToken.None));
            Assert.Equal(SettingsRefusalReason.StaleVersion, stale.Reason);

            // (b) The save-time race: a second connection commits version 2 inside the guarded save.
            await ClearTableAsync(context, "pdf_directory_settings");
            var repository = new PdfDirectorySettingsRepository(context);

            var seeded = await repository.SetAsync(
                new PdfDirectorySettings(Guid.NewGuid(), firstPath, Version: 1, DateTimeOffset.UtcNow),
                CancellationToken.None);
            Assert.Equal(1, seeded.Version);

            var racer = new PdfDirectoryVersionRaceInterceptor();
            await using var marked = CreateMarkedContext(racer);

            await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
                new PdfDirectorySettingsRepository(marked).SetAsync(
                    new PdfDirectorySettings(Guid.NewGuid(), secondPath, Version: 1, DateTimeOffset.UtcNow),
                    CancellationToken.None));

            Assert.Equal(1, racer.Bumps);

            // The persisted row keeps the original path and the racer's committed version 2.
            Assert.Equal(
                "true|2",
                await ScalarAsync(
                    context,
                    "SELECT (base_directory = @p)::text || '|' || version FROM pdf_directory_settings",
                    new NpgsqlParameter("p", firstPath)));
        }
        finally
        {
            await ClearSettingsTablesAsync(context);
        }
    }

    // --------------------------------------------------------------------------------------------
    // Composition and DB helpers
    // --------------------------------------------------------------------------------------------

    private static DmoDbContext CreateMarkedContext(PdfDirectoryVersionRaceInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<DmoDbContext>()
            .UseNpgsql(PersistenceTestDatabase.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;

        return new DmoDbContext(options);
    }

    private static Task ClearTableAsync(DmoDbContext context, string table) =>
        PersistenceTestDatabase.ClearTableAsync(context, table);

    /// <summary>
    /// Empties the settings tables in FK-safe dependency order; the fixed machine keys and the
    /// single-row PDF setting make this mandatory for deterministic runs.
    /// </summary>
    private static async Task ClearSettingsTablesAsync(DmoDbContext context)
    {
        await ClearTableAsync(context, "email_list_recipients");
        await ClearTableAsync(context, "email_lists");
        await ClearTableAsync(context, "email_templates");
        await ClearTableAsync(context, "machine_repairer_assignments");
        await ClearTableAsync(context, "pdf_directory_settings");
        await ClearTableAsync(context, "repairers");
    }

    private static async Task<int> CountAsync(
        DmoDbContext context,
        string fromAndWhere,
        params NpgsqlParameter[] parameters) =>
        int.Parse(Assert.Single(await QueryStringsAsync(
            context,
            $"SELECT count(*) FROM {fromAndWhere}",
            parameters)));

    private static async Task<string> ScalarAsync(
        DmoDbContext context,
        string sql,
        params NpgsqlParameter[] parameters) =>
        Assert.Single(await QueryStringsAsync(context, sql, parameters));

    private static async Task<IReadOnlyList<string>> QueryStringsAsync(
        DmoDbContext context,
        string sql,
        params NpgsqlParameter[] parameters)
    {
        var results = new List<string>();
        var connection = context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var values = new string[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                values[i] = reader.GetValue(i)?.ToString() ?? string.Empty;
            }

            results.Add(string.Join("|", values));
        }

        return results;
    }

    /// <summary>
    /// The test-only race seam for the single-row PDF-directory setting: on the repository save in
    /// flight, a second — real — database connection commits <c>SET version = version + 1</c> before
    /// the first write's guarded <c>UPDATE</c> evaluates its concurrency-token predicate.
    /// </summary>
    private sealed class PdfDirectoryVersionRaceInterceptor : SaveChangesInterceptor
    {
        private int _bumps;

        /// <summary>The number of version bumps committed by the second connection.</summary>
        public int Bumps => _bumps;

        /// <inheritdoc />
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _bumps, 1) == 0)
            {
                using var racer = PersistenceTestDatabase.CreateContext();
                racer.Database.ExecuteSqlRaw(
                    "UPDATE pdf_directory_settings SET version = version + 1");
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}