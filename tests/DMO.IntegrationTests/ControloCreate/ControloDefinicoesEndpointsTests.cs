using System.Net;
using System.Text.Json;
using DMO.Application.Access;
using DMO.Application.ControloCreate;
using DMO.IntegrationTests.Navigation;
using DMO.Web.Authorization;

namespace DMO.IntegrationTests.ControloCreate;

/// <summary>
/// P2-T05 transport-class (<c>I</c>) proofs of the Definições surface (routes 12–17): the REAL web
/// host with the REAL <c>ControloDefinicoesEndpoints</c>/Razor page and the REAL
/// <c>ControloDefinicoesService</c> over the controlled P2-T05 store.
/// </summary>
/// <remarks>
/// <para>
/// Authority: P2-T05 contract §21.3 (routes 12–17), §26.2 (the exact validation tokens), §26.4 rows
/// MAC7 (AC-E5) and AC-G2 (the settings authorization proof). Every caller in this class holds the
/// <c>controlo-create</c> grant; the approve-only refusal in MAC7 proves the Definições gating
/// server-side.</para>
/// <para>
/// The transport rows prove the exact round-trips: ids are retained across renames, versions bump
/// exactly once per committed write, stale versions are refused with 409 <c>stale-version</c>, the
/// six machine assignments stay independent, the PDF directory is explicit about its
/// not-configured state and uses the typed check vocabulary, and the email lists/templates
/// replace-all and delete with explicit confirmation.</para>
/// </remarks>
public sealed class ControloDefinicoesEndpointsTests
{
    private const string DefinicoesPath = "/controlo/create/definicoes";

    /// <summary>
    /// The accepted repairer register round-trip (route 13, supports the MAC7 transport): create →
    /// 201 with the real id, list → one item, rename → the SAME repairer_id with version 2, a stale
    /// rename → 409 <c>stale-version</c>, and a blank name → 400 <c>NAME_REQUIRED</c>.
    /// </summary>
    [Fact]
    public async Task RepairerRegister_TransportRoundTripRetainsTheIdAcrossTheRename()
    {
        var composition = new P2T05TestComposition();

        using var factory = P2T05TestHost.ForUser(P2T05TestHost.AllGranted(), composition);
        using var client = factory.CreateClient();

        Guid repairerId;
        using (var created = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Post,
                   $"{DefinicoesPath}/repairers",
                   P2T05TestHost.Json(new { name = "José" })))
        {
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);

            var payload = await ReadJsonAsync(created);
            repairerId = payload.GetProperty("repairerId").GetGuid();
            Assert.NotEqual(Guid.Empty, repairerId);
            Assert.Equal(1, payload.GetProperty("version").GetInt32());
        }

        using (var list = await P2T05TestHost.GetAsync(client, $"{DefinicoesPath}/repairers"))
        {
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);

            var payload = await ReadJsonAsync(list);
            var item = Assert.Single(payload.GetProperty("repairers").EnumerateArray().ToArray());
            Assert.Equal(repairerId, item.GetProperty("repairerId").GetGuid());
            Assert.Equal("José", item.GetProperty("name").GetString());
            Assert.Equal(1, item.GetProperty("version").GetInt32());
        }

        using (var renamed = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Put,
                   $"{DefinicoesPath}/repairers/{repairerId}",
                   P2T05TestHost.Json(new { expectedVersion = 1, name = "José Silva" })))
        {
            Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);

            var payload = await ReadJsonAsync(renamed);
            Assert.Equal(repairerId, payload.GetProperty("repairerId").GetGuid());
            Assert.Equal(2, payload.GetProperty("version").GetInt32());
        }

        // A stale rename is refused with the typed token and writes nothing.
        using (var stale = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Put,
                   $"{DefinicoesPath}/repairers/{repairerId}",
                   P2T05TestHost.Json(new { expectedVersion = 1, name = "José Outra Vez" })))
        {
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

            var payload = await ReadJsonAsync(stale);
            Assert.Equal("stale-version", payload.GetProperty("reason").GetString());
        }

        // A blank name is a validation failure, never a write.
        using (var blank = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Post,
                   $"{DefinicoesPath}/repairers",
                   P2T05TestHost.Json(new { name = "   " })))
        {
            Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);

            var payload = await ReadJsonAsync(blank);
            Assert.Equal("validation-failed", payload.GetProperty("reason").GetString());
            Assert.Contains(
                ControloDefinicoesValidationErrors.NameRequired,
                payload.GetProperty("errors").EnumerateArray().Select(error => error.GetString() ?? string.Empty));
        }
    }

    /// <summary>
    /// MAC7 (contract §26.4) — proves AC-E5: the six machine assignments are always listed
    /// (B1…C3), one machine is set/changed/cleared INDEPENDENTLY (the other five are never
    /// touched), an unknown machine is <c>MACHINE_UNKNOWN</c>, a clear returns version 0, and an
    /// approve-only caller is denied the settings write with 403 (AC-G2).
    /// </summary>
    [Fact]
    public async Task MAC7_MachineAssignmentsChangeOnlyTheTargetedMachineAndStaySettingsGated()
    {
        var composition = new P2T05TestComposition();

        using var factory = P2T05TestHost.ForUser(P2T05TestHost.AllGranted(), composition);
        using var client = factory.CreateClient();

        // A real register id is required by the assignment set.
        Guid repairerId;
        using (var created = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Post,
                   $"{DefinicoesPath}/repairers",
                   P2T05TestHost.Json(new { name = "José" })))
        {
            var payload = await ReadJsonAsync(created);
            repairerId = payload.GetProperty("repairerId").GetGuid();
        }

        // The initial snapshot is ALL SIX machines, all unassigned.
        using (var initial = await P2T05TestHost.GetAsync(client, $"{DefinicoesPath}/machine-assignments"))
        {
            Assert.Equal(HttpStatusCode.OK, initial.StatusCode);

            var assignments = (await ReadJsonAsync(initial)).GetProperty("assignments");
            Assert.Equal(6, assignments.GetArrayLength());
            Assert.Equal(
                new[] { "B1", "B2", "B3", "C1", "C2", "C3" },
                assignments.EnumerateArray().Select(item => item.GetProperty("machine").GetString()));
            Assert.All(
                assignments.EnumerateArray(),
                item => Assert.Equal(JsonValueKind.Null, item.GetProperty("repairerId").ValueKind));
        }

        // Set B1 only.
        using (var setB1 = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Put,
                   $"{DefinicoesPath}/machine-assignments/B1",
                   P2T05TestHost.Json(new { repairerId, expectedVersion = (int?)null })))
        {
            Assert.Equal(HttpStatusCode.OK, setB1.StatusCode);

            var payload = await ReadJsonAsync(setB1);
            Assert.Equal("B1", payload.GetProperty("machine").GetString());
            Assert.Equal(1, payload.GetProperty("version").GetInt32());
        }

        // Set C3 independently; the other four machines stay untouched.
        using (var setC3 = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Put,
                   $"{DefinicoesPath}/machine-assignments/C3",
                   P2T05TestHost.Json(new { repairerId, expectedVersion = (int?)null })))
        {
            Assert.Equal(HttpStatusCode.OK, setC3.StatusCode);

            var payload = await ReadJsonAsync(setC3);
            Assert.Equal("C3", payload.GetProperty("machine").GetString());
            Assert.Equal(1, payload.GetProperty("version").GetInt32());
        }

        using (var after = await P2T05TestHost.GetAsync(client, $"{DefinicoesPath}/machine-assignments"))
        {
            var assignments = (await ReadJsonAsync(after)).GetProperty("assignments");

            var byMachine = assignments.EnumerateArray()
                .ToDictionary(item => item.GetProperty("machine").GetString()!, item => item);

            Assert.Equal(repairerId, byMachine["B1"].GetProperty("repairerId").GetGuid());
            Assert.Equal(repairerId, byMachine["C3"].GetProperty("repairerId").GetGuid());
            Assert.All(
                new[] { "B2", "B3", "C1", "C2" },
                machine => Assert.Equal(
                    JsonValueKind.Null,
                    byMachine[machine].GetProperty("repairerId").ValueKind));
        }

        // An unknown machine is refused with the typed token.
        using (var unknown = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Put,
                   $"{DefinicoesPath}/machine-assignments/B4",
                   P2T05TestHost.Json(new { repairerId, expectedVersion = (int?)null })))
        {
            Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);

            var payload = await ReadJsonAsync(unknown);
            Assert.Equal("validation-failed", payload.GetProperty("reason").GetString());
            Assert.Contains(
                ControloDefinicoesValidationErrors.MachineUnknown,
                payload.GetProperty("errors").EnumerateArray().Select(error => error.GetString() ?? string.Empty));
        }

        // The explicit clear of B1 returns version 0 (cleared semantics) and touches only B1.
        using (var cleared = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Put,
                   $"{DefinicoesPath}/machine-assignments/B1",
                   P2T05TestHost.Json(new { repairerId = (Guid?)null, expectedVersion = 1 })))
        {
            Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);

            var payload = await ReadJsonAsync(cleared);
            Assert.Equal("B1", payload.GetProperty("machine").GetString());
            Assert.Equal(0, payload.GetProperty("version").GetInt32());
        }

        using (var afterClear = await P2T05TestHost.GetAsync(client, $"{DefinicoesPath}/machine-assignments"))
        {
            var assignments = (await ReadJsonAsync(afterClear)).GetProperty("assignments");

            var byMachine = assignments.EnumerateArray()
                .ToDictionary(item => item.GetProperty("machine").GetString()!, item => item);

            Assert.Equal(JsonValueKind.Null, byMachine["B1"].GetProperty("repairerId").ValueKind);
            Assert.Equal(repairerId, byMachine["C3"].GetProperty("repairerId").GetGuid());
        }

        // The settings authorization proof (AC-G2): an approve-only caller is denied the settings
        // write server-side, even though the module shares the "controlo" destination.
        var approve = TestNavigationComposition.Definition(
            ModuleCatalog.ControloApprove, "Controlo Approve", "controlo", "Controlo");

        using (var approveFactory = P2T05TestHost.ForUser([approve], composition))
        using (var approveClient = approveFactory.CreateClient())
        {
            using var denied = await P2T05TestHost.SendJsonAsync(
                approveClient,
                HttpMethod.Put,
                $"{DefinicoesPath}/machine-assignments/B1",
                P2T05TestHost.Json(new { repairerId, expectedVersion = (int?)null }));

            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        }

        // C3 is exactly the state a denied write must leave untouched.
        using (var final = await P2T05TestHost.GetAsync(client, $"{DefinicoesPath}/machine-assignments"))
        {
            var assignments = (await ReadJsonAsync(final)).GetProperty("assignments");
            var c3 = assignments.EnumerateArray().Single(item => item.GetProperty("machine").GetString() == "C3");
            Assert.Equal(repairerId, c3.GetProperty("repairerId").GetGuid());
            Assert.Equal(1, c3.GetProperty("version").GetInt32());
        }
    }

    /// <summary>
    /// PDF-directory transport (route 15, supports MAC7/settings transport): the not-configured
    /// state is explicit (<c>baseDirectory:null, version:null</c>), a rooted server-host path is
    /// saved (version 1), blank/non-rooted values are refused with
    /// <c>DIRECTORY_REQUIRED</c>/<c>DIRECTORY_INVALID</c>, and the server-side check reports the
    /// TYPED §12.2 vocabulary only (SET3-transport: states are never conflated).
    /// </summary>
    [Fact]
    public async Task PdfDirectory_TransportRoundTripWithTheTypedCheckVocabulary()
    {
        var composition = new P2T05TestComposition();

        using var factory = P2T05TestHost.ForUser(P2T05TestHost.AllGranted(), composition);
        using var client = factory.CreateClient();

        using (var notConfigured = await P2T05TestHost.GetAsync(client, $"{DefinicoesPath}/pdf-directory"))
        {
            Assert.Equal(HttpStatusCode.OK, notConfigured.StatusCode);

            var payload = await ReadJsonAsync(notConfigured);
            Assert.Equal(JsonValueKind.Null, payload.GetProperty("baseDirectory").ValueKind);
            Assert.Equal(JsonValueKind.Null, payload.GetProperty("version").ValueKind);
        }

        // The configured value is server-host absolute; the temp-based root is rooted everywhere.
        var baseDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "dmo-reports"));
        Assert.True(Path.IsPathRooted(baseDirectory));

        using (var saved = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Put,
                   $"{DefinicoesPath}/pdf-directory",
                   P2T05TestHost.Json(new { baseDirectory, expectedVersion = (int?)null })))
        {
            Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

            var payload = await ReadJsonAsync(saved);
            Assert.Equal(1, payload.GetProperty("version").GetInt32());
        }

        using (var readBack = await P2T05TestHost.GetAsync(client, $"{DefinicoesPath}/pdf-directory"))
        {
            var payload = await ReadJsonAsync(readBack);
            Assert.Equal(baseDirectory, payload.GetProperty("baseDirectory").GetString());
            Assert.Equal(1, payload.GetProperty("version").GetInt32());
        }

        // Blank and relative values are validation failures, never writes.
        using (var blank = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Put,
                   $"{DefinicoesPath}/pdf-directory",
                   P2T05TestHost.Json(new { baseDirectory = "   ", expectedVersion = 1 })))
        {
            Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);

            var payload = await ReadJsonAsync(blank);
            Assert.Equal("validation-failed", payload.GetProperty("reason").GetString());
            Assert.Contains(
                ControloDefinicoesValidationErrors.DirectoryRequired,
                payload.GetProperty("errors").EnumerateArray().Select(error => error.GetString() ?? string.Empty));
        }

        using (var relative = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Put,
                   $"{DefinicoesPath}/pdf-directory",
                   P2T05TestHost.Json(new { baseDirectory = "reports", expectedVersion = 1 })))
        {
            Assert.Equal(HttpStatusCode.BadRequest, relative.StatusCode);

            var payload = await ReadJsonAsync(relative);
            Assert.Equal("validation-failed", payload.GetProperty("reason").GetString());
            Assert.Contains(
                ControloDefinicoesValidationErrors.DirectoryInvalid,
                payload.GetProperty("errors").EnumerateArray().Select(error => error.GetString() ?? string.Empty));
        }

        // The typed check vocabulary: every verdict is its own token, never conflated (SET3).
        foreach (var (verdict, expectedToken) in new[]
                 {
                     (PdfDirectoryCheckState.NotADirectory, "not-a-directory"),
                     (PdfDirectoryCheckState.DirectoryNotFound, "directory-not-found"),
                     (PdfDirectoryCheckState.Ok, "ok"),
                 })
        {
            composition.DirectoryProbe.Verdict = verdict;

            using var check = await P2T05TestHost.SendJsonAsync(
                client, HttpMethod.Post, $"{DefinicoesPath}/pdf-directory/check", "{}");
            Assert.Equal(HttpStatusCode.OK, check.StatusCode);

            var payload = await ReadJsonAsync(check);
            Assert.Equal(expectedToken, payload.GetProperty("state").GetString());
        }
    }

    /// <summary>
    /// Email-list transport (route 16): create → 201 with the real id; a duplicate name → 409
    /// <c>duplicate-name</c>; the list surface reports the recipient COUNT; the ficha read returns
    /// the COMPLETE recipient set; an update replaces the whole set in one transaction; a delete
    /// requires the explicit confirmation and a second delete of the same id is a 404; an invalid
    /// address is <c>ADDRESS_INVALID</c>.
    /// </summary>
    [Fact]
    public async Task EmailLists_TransportRoundTripWithReplaceAllAndExplicitDelete()
    {
        var composition = new P2T05TestComposition();

        using var factory = P2T05TestHost.ForUser(P2T05TestHost.AllGranted(), composition);
        using var client = factory.CreateClient();

        Guid listId;
        using (var created = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Post,
                   $"{DefinicoesPath}/email-lists",
                   P2T05TestHost.Json(new
                   {
                       name = "Fábrica",
                       recipients = new[] { "a@x.pt", "b@x.pt" },
                   })))
        {
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);

            var payload = await ReadJsonAsync(created);
            listId = payload.GetProperty("emailListId").GetGuid();
            Assert.NotEqual(Guid.Empty, listId);
            Assert.Equal(1, payload.GetProperty("version").GetInt32());
        }

        using (var duplicate = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Post,
                   $"{DefinicoesPath}/email-lists",
                   P2T05TestHost.Json(new
                   {
                       name = "Fábrica",
                       recipients = new[] { "c@x.pt" },
                   })))
        {
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

            var payload = await ReadJsonAsync(duplicate);
            Assert.Equal("duplicate-name", payload.GetProperty("reason").GetString());
        }

        using (var list = await P2T05TestHost.GetAsync(client, $"{DefinicoesPath}/email-lists"))
        {
            var payload = await ReadJsonAsync(list);
            var item = Assert.Single(payload.GetProperty("lists").EnumerateArray().ToArray());
            Assert.Equal(listId, item.GetProperty("emailListId").GetGuid());
            Assert.Equal("Fábrica", item.GetProperty("name").GetString());
            Assert.Equal(2, item.GetProperty("recipientCount").GetInt32());
            Assert.Equal(1, item.GetProperty("version").GetInt32());
        }

        using (var ficha = await P2T05TestHost.GetAsync(client, $"{DefinicoesPath}/email-lists/{listId}"))
        {
            var payload = await ReadJsonAsync(ficha);
            Assert.Equal("Fábrica", payload.GetProperty("name").GetString());
            Assert.Equal(
                new[] { "a@x.pt", "b@x.pt" },
                payload.GetProperty("recipients").EnumerateArray().Select(recipient => recipient.GetString()));
        }

        // Replace-all: the update carries the COMPLETE new set; the old addresses are gone.
        using (var updated = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Put,
                   $"{DefinicoesPath}/email-lists/{listId}",
                   P2T05TestHost.Json(new
                   {
                       expectedVersion = 1,
                       name = "Fábrica",
                       recipients = new[] { "c@x.pt" },
                   })))
        {
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

            var payload = await ReadJsonAsync(updated);
            Assert.Equal(listId, payload.GetProperty("emailListId").GetGuid());
            Assert.Equal(2, payload.GetProperty("version").GetInt32());
        }

        using (var afterReplace = await P2T05TestHost.GetAsync(client, $"{DefinicoesPath}/email-lists/{listId}"))
        {
            var payload = await ReadJsonAsync(afterReplace);
            Assert.Equal(
                new[] { "c@x.pt" },
                payload.GetProperty("recipients").EnumerateArray().Select(recipient => recipient.GetString()));
        }

        // Confirmed delete → 204; the same id again → 404; unconfirmed delete → 400
        // DELETE_NOT_CONFIRMED and the list survives.
        using (var deleted = await P2T05TestHost.SendAuthenticatedAsync(
                   client,
                   new HttpRequestMessage(
                       HttpMethod.Delete,
                       $"{DefinicoesPath}/email-lists/{listId}?expectedVersion=2&deleteConfirmed=true")))
        {
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        }

        using (var again = await P2T05TestHost.SendAuthenticatedAsync(
                   client,
                   new HttpRequestMessage(
                       HttpMethod.Delete,
                       $"{DefinicoesPath}/email-lists/{listId}?expectedVersion=2&deleteConfirmed=true")))
        {
            Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
        }

        using (var invalidAddress = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Post,
                   $"{DefinicoesPath}/email-lists",
                   P2T05TestHost.Json(new
                   {
                       name = "Turno",
                       recipients = new[] { "not-an-address" },
                   })))
        {
            Assert.Equal(HttpStatusCode.BadRequest, invalidAddress.StatusCode);

            var payload = await ReadJsonAsync(invalidAddress);
            Assert.Equal("validation-failed", payload.GetProperty("reason").GetString());
            Assert.Contains(
                ControloDefinicoesValidationErrors.AddressInvalid,
                payload.GetProperty("errors").EnumerateArray().Select(error => error.GetString() ?? string.Empty));
        }

        // The freed name can be re-created, and the unconfirmed delete leaves it intact.
        Guid secondId;
        using (var recreated = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Post,
                   $"{DefinicoesPath}/email-lists",
                   P2T05TestHost.Json(new
                   {
                       name = "Fábrica",
                       recipients = new[] { "a@x.pt" },
                   })))
        {
            Assert.Equal(HttpStatusCode.Created, recreated.StatusCode);
            secondId = (await ReadJsonAsync(recreated)).GetProperty("emailListId").GetGuid();
        }

        using (var unconfirmed = await P2T05TestHost.SendAuthenticatedAsync(
                   client,
                   new HttpRequestMessage(
                       HttpMethod.Delete,
                       $"{DefinicoesPath}/email-lists/{secondId}?expectedVersion=1&deleteConfirmed=false")))
        {
            Assert.Equal(HttpStatusCode.BadRequest, unconfirmed.StatusCode);

            var payload = await ReadJsonAsync(unconfirmed);
            Assert.Equal("validation-failed", payload.GetProperty("reason").GetString());
            Assert.Contains(
                ControloDefinicoesValidationErrors.DeleteNotConfirmed,
                payload.GetProperty("errors").EnumerateArray().Select(error => error.GetString() ?? string.Empty));
        }

        using (var survives = await P2T05TestHost.GetAsync(client, $"{DefinicoesPath}/email-lists"))
        {
            var payload = await ReadJsonAsync(survives);
            Assert.Equal(1, payload.GetProperty("lists").GetArrayLength());
            Assert.Equal(secondId, payload.GetProperty("lists")[0].GetProperty("emailListId").GetGuid());
        }
    }

    /// <summary>
    /// Email-template transport (route 17): create with the <c>peso</c> document type → 201; an
    /// unknown document type → 400 <c>DOCUMENT_TYPE_UNKNOWN</c>; a null document type is the clean
    /// generic template → 201; the rename keeps the id and bumps to version 2; the confirmed delete
    /// → 204.
    /// </summary>
    [Fact]
    public async Task EmailTemplates_TransportRoundTripWithTheDocumentTypeVocabulary()
    {
        var composition = new P2T05TestComposition();

        using var factory = P2T05TestHost.ForUser(P2T05TestHost.AllGranted(), composition);
        using var client = factory.CreateClient();

        Guid templateId;
        using (var created = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Post,
                   $"{DefinicoesPath}/email-templates",
                   P2T05TestHost.Json(new
                   {
                       name = "Peso",
                       subject = "Relatório",
                       body = "Em anexo",
                       documentType = "peso",
                   })))
        {
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);

            var payload = await ReadJsonAsync(created);
            templateId = payload.GetProperty("emailTemplateId").GetGuid();
            Assert.NotEqual(Guid.Empty, templateId);
            Assert.Equal(1, payload.GetProperty("version").GetInt32());
        }

        using (var unknownType = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Post,
                   $"{DefinicoesPath}/email-templates",
                   P2T05TestHost.Json(new
                   {
                       name = "Inválido",
                       subject = "S",
                       body = "B",
                       documentType = "x",
                   })))
        {
            Assert.Equal(HttpStatusCode.BadRequest, unknownType.StatusCode);

            var payload = await ReadJsonAsync(unknownType);
            Assert.Equal("validation-failed", payload.GetProperty("reason").GetString());
            Assert.Contains(
                ControloDefinicoesValidationErrors.DocumentTypeUnknown,
                payload.GetProperty("errors").EnumerateArray().Select(error => error.GetString() ?? string.Empty));
        }

        using (var generic = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Post,
                   $"{DefinicoesPath}/email-templates",
                   P2T05TestHost.Json(new
                   {
                       name = "Genérico",
                       subject = "S",
                       body = "B",
                       documentType = (string?)null,
                   })))
        {
            Assert.Equal(HttpStatusCode.Created, generic.StatusCode);
        }

        using (var renamed = await P2T05TestHost.SendJsonAsync(
                   client,
                   HttpMethod.Put,
                   $"{DefinicoesPath}/email-templates/{templateId}",
                   P2T05TestHost.Json(new
                   {
                       expectedVersion = 1,
                       name = "Peso atualizado",
                       subject = "Relatório",
                       body = "Em anexo (revisto)",
                       documentType = "peso",
                   })))
        {
            Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);

            var payload = await ReadJsonAsync(renamed);
            Assert.Equal(templateId, payload.GetProperty("emailTemplateId").GetGuid());
            Assert.Equal(2, payload.GetProperty("version").GetInt32());
        }

        using (var deleted = await P2T05TestHost.SendAuthenticatedAsync(
                   client,
                   new HttpRequestMessage(
                       HttpMethod.Delete,
                       $"{DefinicoesPath}/email-templates/{templateId}?expectedVersion=2&deleteConfirmed=true")))
        {
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        }

        // The generic template is the only survivor.
        using (var list = await P2T05TestHost.GetAsync(client, $"{DefinicoesPath}/email-templates"))
        {
            var payload = await ReadJsonAsync(list);
            var items = payload.GetProperty("templates").EnumerateArray().ToArray();
            var item = Assert.Single(items);
            Assert.Equal("Genérico", item.GetProperty("name").GetString());
            Assert.Equal(JsonValueKind.Null, item.GetProperty("documentType").ValueKind);
        }
    }

    // ---- arrangement helpers -------------------------------------------------------------

    /// <summary>Reads a JSON response body into a detached element.</summary>
    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.Clone();
    }
}