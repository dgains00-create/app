using DMO.Application.ControloCreate;
using DMO.Application.Persistence;
using DMO.Application.Repositories;
using DMO.Domain.Controlo;
using DMO.Domain.Tools;

namespace DMO.UnitTests.ControloCreate;

/// <summary>
/// P2-T05 unit proofs of the Definições validation: rows REP1, REP2, SET6, SET7, SET9, MAC1 and
/// SET1 of the test-to-acceptance matrix (<c>plans/contracts/P2-T05_CONTROLO_CREATE_CONTRACT.md</c>
/// §26.4), over the closed §10.4/§12.2/§13.2/§14.2 token set.
/// </summary>
/// <remarks>
/// The pure static validator runs before any write and returns the exact contracted codes. The one
/// DB-backed referee of SET6 — the same address refused twice in one list — lives on the
/// <c>UNIQUE (email_list_id, address)</c> constraint of the persistence layer, so it is proved here
/// through the real service path that maps the typed persistence failure onto
/// <c>ADDRESS_INVALID</c> (the validator itself only checks the minimal address shape; the contract
/// row SET6 is class DB for exactly this reason).
/// </remarks>
public sealed class ControloDefinicoesValidatorTests
{
    /// <summary>
    /// REP1 (AC-D1) — the repairer register needs the name only: blank and whitespace names are
    /// refused with exactly <c>NAME_REQUIRED</c>, and a name is accepted (trimmed) with no token.
    /// </summary>
    [Fact]
    public void REP1_CreateRepairerRequiresANameWithTheExactToken()
    {
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.NameRequired },
            ControloDefinicoesValidator.Validate(new CreateRepairerCommand(string.Empty)));
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.NameRequired },
            ControloDefinicoesValidator.Validate(new CreateRepairerCommand("   ")));

        Assert.Empty(ControloDefinicoesValidator.Validate(new CreateRepairerCommand(" José ")));
        Assert.Empty(ControloDefinicoesValidator.Validate(new CreateRepairerCommand("José")));
    }

    /// <summary>
    /// REP2 (AC-D2) — the rename command requires its name with the same <c>NAME_REQUIRED</c> token
    /// (and a missing repairer identity is the same refusal, resolved before any write); a valid
    /// name validates clean.
    /// </summary>
    [Fact]
    public void REP2_RenameRepairerRejectsABlankNameWithTheExactToken()
    {
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.NameRequired },
            ControloDefinicoesValidator.Validate(new RenameRepairerCommand(Guid.NewGuid(), 1, "  ")));
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.NameRequired },
            ControloDefinicoesValidator.Validate(new RenameRepairerCommand(Guid.Empty, 1, "José")));

        Assert.Empty(ControloDefinicoesValidator.Validate(
            new RenameRepairerCommand(Guid.NewGuid(), 1, "José")));
        Assert.Empty(ControloDefinicoesValidator.Validate(
            new RenameRepairerCommand(Guid.NewGuid(), 7, "José Maria")));
    }

    /// <summary>
    /// SET6 (AC-F4) — recipient addresses follow the minimal unbroken shape: exactly one <c>@</c>,
    /// non-blank local part and domain, no whitespace. Broken shapes are refused with exactly
    /// <c>ADDRESS_INVALID</c>, blanks with <c>ADDRESS_REQUIRED</c>; the same address is legal across
    /// two separate lists, and an address repeated inside ONE list is refused by the
    /// <c>(email_list_id, address)</c> uniqueness backstop mapped by the service onto the same
    /// <c>ADDRESS_INVALID</c> token.
    /// </summary>
    [Fact]
    public async Task SET6_AddressShapesUseTheMinimalUnbrokenRuleAndTheExactTokens()
    {
        // The shape predicate: the accepted form plus every broken form.
        Assert.True(ControloDefinicoesValidator.IsMinimalAddressShape("a@b.com"));
        Assert.False(ControloDefinicoesValidator.IsMinimalAddressShape("no-at-sign"));
        Assert.False(ControloDefinicoesValidator.IsMinimalAddressShape("a@b@c"));
        Assert.False(ControloDefinicoesValidator.IsMinimalAddressShape("a b@c"));
        Assert.False(ControloDefinicoesValidator.IsMinimalAddressShape("@b"));
        Assert.False(ControloDefinicoesValidator.IsMinimalAddressShape("a@"));
        Assert.False(ControloDefinicoesValidator.IsMinimalAddressShape(""));
        Assert.False(ControloDefinicoesValidator.IsMinimalAddressShape("  "));

        // The create-list validation surfaces the exact tokens.
        Assert.Empty(ControloDefinicoesValidator.Validate(
            new CreateEmailListCommand("Lista", ["a@b.com"])));

        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.AddressInvalid },
            ControloDefinicoesValidator.Validate(new CreateEmailListCommand("Lista", ["no-at-sign"])));
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.AddressInvalid },
            ControloDefinicoesValidator.Validate(new CreateEmailListCommand("Lista", ["a@b@c"])));
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.AddressInvalid },
            ControloDefinicoesValidator.Validate(new CreateEmailListCommand("Lista", ["a b@c"])));
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.AddressInvalid },
            ControloDefinicoesValidator.Validate(new CreateEmailListCommand("Lista", ["@b"])));
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.AddressInvalid },
            ControloDefinicoesValidator.Validate(new CreateEmailListCommand("Lista", ["a@"])));

        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.AddressRequired },
            ControloDefinicoesValidator.Validate(new CreateEmailListCommand("Lista", [""])));
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.AddressRequired },
            ControloDefinicoesValidator.Validate(new CreateEmailListCommand("Lista", ["   "])));

        // Cross-list repetition is allowed: the same address validates clean in two separate
        // commands, and the service persists both lists.
        Assert.Empty(ControloDefinicoesValidator.Validate(
            new CreateEmailListCommand("Lista A", ["a@b.com"])));
        Assert.Empty(ControloDefinicoesValidator.Validate(
            new CreateEmailListCommand("Lista B", ["a@b.com"])));

        var storingLists = new StoringEmailListRepository();
        var service = BuildSettingsService(storingLists);
        var first = Assert.IsType<SettingsResult.EmailListCreated>(
            await service.CreateEmailListAsync(new CreateEmailListCommand("Lista A", ["a@b.com"]), CancellationToken.None));
        var second = Assert.IsType<SettingsResult.EmailListCreated>(
            await service.CreateEmailListAsync(new CreateEmailListCommand("Lista B", ["a@b.com"]), CancellationToken.None));
        Assert.NotEqual(first.EmailListId, second.EmailListId);
        Assert.Equal(2, storingLists.CreatedCount);

        // One address twice in ONE list is refused by the persistence uniqueness backstop, mapped
        // by the service onto exactly ADDRESS_INVALID — never a 500 and never a silent second row.
        var duplicateService = BuildSettingsService(new DuplicateAddressEmailListRepository());
        var duplicateResult = Assert.IsType<SettingsResult.ValidationFailed>(
            await duplicateService.CreateEmailListAsync(new CreateEmailListCommand("Lista", ["a@b.com", "a@b.com"]), CancellationToken.None));
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.AddressInvalid },
            duplicateResult.Errors);
    }

    /// <summary>
    /// SET7 (AC-F5) — the email template carries name, subject, body and an optional document type:
    /// each blank fact is its own exact token, a fourth document type is
    /// <c>DOCUMENT_TYPE_UNKNOWN</c>, and <c>null</c> plus the three settled families validate clean.
    /// </summary>
    [Fact]
    public void SET7_EmailTemplateFactsAreRequiredAndDocumentTypeIsClosed()
    {
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.TemplateNameRequired },
            ControloDefinicoesValidator.Validate(new CreateEmailTemplateCommand("  ", "Assunto", "Corpo", null)));
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.SubjectRequired },
            ControloDefinicoesValidator.Validate(new CreateEmailTemplateCommand("Modelo", " ", "Corpo", null)));
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.BodyRequired },
            ControloDefinicoesValidator.Validate(new CreateEmailTemplateCommand("Modelo", "Assunto", "\t", null)));

        Assert.Equal(
            new[]
            {
                ControloDefinicoesValidationErrors.TemplateNameRequired,
                ControloDefinicoesValidationErrors.SubjectRequired,
                ControloDefinicoesValidationErrors.BodyRequired,
            },
            ControloDefinicoesValidator.Validate(new CreateEmailTemplateCommand("", "", "", null)));

        // The document type is one of peso/pegamentos/resumo or absent; anything else is unknown.
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.DocumentTypeUnknown },
            ControloDefinicoesValidator.Validate(new CreateEmailTemplateCommand("Modelo", "Assunto", "Corpo", "folha")));
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.DocumentTypeUnknown },
            ControloDefinicoesValidator.Validate(new CreateEmailTemplateCommand("Modelo", "Assunto", "Corpo", "x")));

        Assert.Empty(ControloDefinicoesValidator.Validate(
            new CreateEmailTemplateCommand("Modelo", "Assunto", "Corpo", null)));
        Assert.Empty(ControloDefinicoesValidator.Validate(
            new CreateEmailTemplateCommand("Modelo", "Assunto", "Corpo", "peso")));
        Assert.Empty(ControloDefinicoesValidator.Validate(
            new CreateEmailTemplateCommand("Modelo", "Assunto", "Corpo", "pegamentos")));
        Assert.Empty(ControloDefinicoesValidator.Validate(
            new CreateEmailTemplateCommand("Modelo", "Assunto", "Corpo", "resumo")));
    }

    /// <summary>
    /// SET9 (AC-F7) — the list and template deletes are explicit confirmations: missing confirmation
    /// → exactly <c>DELETE_NOT_CONFIRMED</c>; confirmed deletes validate clean.
    /// </summary>
    [Fact]
    public void SET9_ListAndTemplateDeletesRequireExplicitConfirmation()
    {
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.DeleteNotConfirmed },
            ControloDefinicoesValidator.Validate(new DeleteEmailListCommand(Guid.NewGuid(), 1, DeleteConfirmed: false)));
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.DeleteNotConfirmed },
            ControloDefinicoesValidator.Validate(new DeleteEmailTemplateCommand(Guid.NewGuid(), 1, DeleteConfirmed: false)));

        Assert.Empty(ControloDefinicoesValidator.Validate(
            new DeleteEmailListCommand(Guid.NewGuid(), 1, DeleteConfirmed: true)));
        Assert.Empty(ControloDefinicoesValidator.Validate(
            new DeleteEmailTemplateCommand(Guid.NewGuid(), 1, DeleteConfirmed: true)));
    }

    /// <summary>
    /// MAC1 (AC-E1) — a one-machine assignment accepts exactly the six settled codes: <c>B4</c>,
    /// <c>Linha B</c>, <c>C</c> and a blank are <c>MACHINE_UNKNOWN</c>; a settled machine with a
    /// real repairer validates clean; an empty repairer id is <c>REPAIRER_NOT_FOUND</c>; a null
    /// repairer id is the explicit clear and validates clean; the clear command enforces the same
    /// closed machine set.
    /// </summary>
    [Fact]
    public void MAC1_MachineAssignmentsAcceptOnlyTheSixSettledMachinesAndAResolvableRepairer()
    {
        foreach (var rejected in new[] { "B4", "Linha B", "C", "" })
        {
            Assert.Equal(
                new[] { ControloDefinicoesValidationErrors.MachineUnknown },
                ControloDefinicoesValidator.Validate(
                    new SetMachineAssignmentCommand(rejected, Guid.NewGuid(), 1)));
        }

        Assert.Empty(ControloDefinicoesValidator.Validate(
            new SetMachineAssignmentCommand("B1", Guid.NewGuid(), 1)));

        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.RepairerNotFound },
            ControloDefinicoesValidator.Validate(
                new SetMachineAssignmentCommand("B1", Guid.Empty, 1)));

        // A null repairer id is the explicit clear for that machine, not a failure.
        Assert.Empty(ControloDefinicoesValidator.Validate(
            new SetMachineAssignmentCommand("B1", RepairerId: null, ExpectedVersion: null)));

        // The clear command goes through the same closed machine set.
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.MachineUnknown },
            ControloDefinicoesValidator.Validate(new ClearMachineAssignmentCommand("B4", 1)));
        Assert.Empty(ControloDefinicoesValidator.Validate(new ClearMachineAssignmentCommand("C2", 1)));
    }

    /// <summary>
    /// SET1 (AC-F1) — the PDF base directory is an absolute server-host path: a blank value is
    /// <c>DIRECTORY_REQUIRED</c>, a relative value is <c>DIRECTORY_INVALID</c>, and a rooted value
    /// validates clean (only absolute paths reach the accessibility probe).
    /// </summary>
    [Fact]
    public void SET1_PdfDirectoryRequiresAnAbsoluteBaseDirectoryWithTheExactTokens()
    {
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.DirectoryRequired },
            ControloDefinicoesValidator.Validate(new SetPdfDirectoryCommand("", 1)));
        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.DirectoryRequired },
            ControloDefinicoesValidator.Validate(new SetPdfDirectoryCommand("   ", 1)));

        Assert.Equal(
            new[] { ControloDefinicoesValidationErrors.DirectoryInvalid },
            ControloDefinicoesValidator.Validate(new SetPdfDirectoryCommand("relativa/pasta", 1)));

        // A rooted value is accepted on any host (the check itself is the probe's job).
        var rooted = Path.Combine(Path.GetTempPath(), "dmo-pdf-settings-probe");
        Assert.True(Path.IsPathRooted(rooted));
        Assert.Empty(ControloDefinicoesValidator.Validate(new SetPdfDirectoryCommand(rooted, 1)));
    }

    // ---------------------------------------------------------------------------------------------
    // Fakes
    // ---------------------------------------------------------------------------------------------

    private static ControloDefinicoesService BuildSettingsService(IEmailListRepository emailLists) =>
        new(
            new EmptyRepairerRepository(),
            new EmptyAssignmentRepository(),
            new EmptyPdfDirectoryRepository(),
            emailLists,
            new EmptyEmailTemplateRepository(),
            new OkDirectoryProbe());

    /// <summary>A repairer repository that is never consulted by these proofs.</summary>
    private sealed class EmptyRepairerRepository : IRepairerRepository
    {
        public Task<Repairer?> GetByIdAsync(Guid repairerId, CancellationToken cancellationToken) =>
            Task.FromResult<Repairer?>(null);

        public Task<IReadOnlyList<Repairer>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Repairer>>([]);

        public Task<Repairer> CreatedAsync(Repairer repairer, CancellationToken cancellationToken) =>
            Task.FromResult(repairer);

        public Task<Repairer> RenamedAsync(Repairer repairer, CancellationToken cancellationToken) =>
            Task.FromResult(repairer);
    }

    /// <summary>An assignment repository that is never consulted by these proofs.</summary>
    private sealed class EmptyAssignmentRepository : IMachineRepairerAssignmentRepository
    {
        public Task<MachineRepairerAssignment?> GetByMachineAsync(string machine, CancellationToken cancellationToken) =>
            Task.FromResult<MachineRepairerAssignment?>(null);

        public Task<IReadOnlyList<MachineRepairerAssignment>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MachineRepairerAssignment>>([]);

        public Task<MachineRepairerAssignment> SetAsync(
            MachineRepairerAssignment assignment,
            CancellationToken cancellationToken) =>
            Task.FromResult(assignment);

        public Task ClearedAsync(string machine, int expectedVersion, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>A PDF-directory settings repository that is never consulted by these proofs.</summary>
    private sealed class EmptyPdfDirectoryRepository : IPdfDirectorySettingsRepository
    {
        public Task<PdfDirectorySettings?> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult<PdfDirectorySettings?>(null);

        public Task<PdfDirectorySettings> SetAsync(
            PdfDirectorySettings settings,
            CancellationToken cancellationToken) =>
            Task.FromResult(settings);
    }

    /// <summary>An email-template repository that is never consulted by these proofs.</summary>
    private sealed class EmptyEmailTemplateRepository : IEmailTemplateRepository
    {
        public Task<EmailTemplate?> GetByIdAsync(Guid emailTemplateId, CancellationToken cancellationToken) =>
            Task.FromResult<EmailTemplate?>(null);

        public Task<IReadOnlyList<EmailTemplate>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EmailTemplate>>([]);

        public Task<EmailTemplate> CreatedAsync(EmailTemplate template, CancellationToken cancellationToken) =>
            Task.FromResult(template);

        public Task<EmailTemplate> UpdatedAsync(EmailTemplate template, CancellationToken cancellationToken) =>
            Task.FromResult(template);

        public Task DeletedAsync(Guid emailTemplateId, int expectedVersion, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>An email-list repository that records how many lists were created.</summary>
    private sealed class StoringEmailListRepository : IEmailListRepository
    {
        private readonly List<EmailList> _lists = new();

        public int CreatedCount => _lists.Count;

        public Task<EmailList?> GetByIdAsync(Guid emailListId, CancellationToken cancellationToken) =>
            Task.FromResult(_lists.FirstOrDefault(list => list.EmailListId.Value == emailListId));

        public Task<IReadOnlyList<EmailList>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EmailList>>(_lists.ToList());

        public Task<EmailList> CreatedAsync(
            EmailList list,
            IReadOnlyList<EmailRecipient> recipients,
            CancellationToken cancellationToken)
        {
            var stored = list with { Recipients = recipients.ToList() };
            _lists.Add(stored);
            return Task.FromResult(stored);
        }

        public Task<EmailList> UpdatedAsync(
            EmailList list,
            IReadOnlyList<EmailRecipient> recipients,
            CancellationToken cancellationToken) =>
            Task.FromResult(list);

        public Task DeletedAsync(Guid emailListId, int expectedVersion, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>An email-list repository replaying the <c>UNIQUE (email_list_id, address)</c>
    /// persistence refusal of a duplicated address inside one list.</summary>
    private sealed class DuplicateAddressEmailListRepository : IEmailListRepository
    {
        public Task<EmailList?> GetByIdAsync(Guid emailListId, CancellationToken cancellationToken) =>
            Task.FromResult<EmailList?>(null);

        public Task<IReadOnlyList<EmailList>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EmailList>>([]);

        public Task<EmailList> CreatedAsync(
            EmailList list,
            IReadOnlyList<EmailRecipient> recipients,
            CancellationToken cancellationToken) =>
            throw new ControloPersistenceException(
                ControloPersistenceFailureReason.DuplicateAddress,
                "An address appears twice in one list.");

        public Task<EmailList> UpdatedAsync(
            EmailList list,
            IReadOnlyList<EmailRecipient> recipients,
            CancellationToken cancellationToken) =>
            throw new ControloPersistenceException(
                ControloPersistenceFailureReason.DuplicateAddress,
                "An address appears twice in one list.");

        public Task DeletedAsync(Guid emailListId, int expectedVersion, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>A directory probe that never claims a failure (never consulted by these proofs).</summary>
    private sealed class OkDirectoryProbe : IPdfDirectoryProbe
    {
        public PdfDirectoryCheckState Probe(string absoluteDirectoryPath) => PdfDirectoryCheckState.Ok;
    }
}