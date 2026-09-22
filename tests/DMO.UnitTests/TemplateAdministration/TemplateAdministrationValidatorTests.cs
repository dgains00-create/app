using DMO.Application.Access;
using DMO.Application.TemplateAdministration;
using DMO.UnitTests.Access.Fakes;

namespace DMO.UnitTests.TemplateAdministration;

/// <summary>
/// P1-T06 validator tests — the pure <see cref="TemplateAdministrationValidator"/> against
/// the controlled test registry (6 available Modules; <c>armazem</c> canonical but
/// unavailable): name bound, canonical-id-only composition, no duplicates, no new
/// unavailable selection, no unknown ids, persisted unavailable/invalid entries preserved or
/// explicitly removed (never silently repaired), and the landing rule (null OR represented by
/// a selected available non-contextual Module destination).
/// </summary>
public sealed class TemplateAdministrationValidatorTests
{
    private const string JobOnView = "job-on-view";
    private const string JobOnCreate = "job-on-create";
    private const string ControloCreate = "controlo-create";
    private const string ControloApprove = "controlo-approve";
    private const string Ferramentas = "ferramentas";
    private const string Armazem = "armazem"; // canonical, unavailable in the test registry

    // -------------------------------------------------------------- NAME

    [Fact]
    public void Create_BlankName_Error()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand("  ", [], LandingDestinationId: null),
            CreateRegistry());

        Assert.Contains(errors, error => error.Contains("Name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Create_TooLongName_Error()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                new string('x', TemplateAdministrationValidator.MaxNameLength + 1),
                [],
                LandingDestinationId: null),
            CreateRegistry());

        Assert.Contains(errors, error => error.Contains("Name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Create_BlankComposition_NullLanding_NoErrors()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Vazio", ModuleIds: [], LandingDestinationId: null),
            CreateRegistry());

        Assert.Empty(errors);
    }

    // -------------------------------------------------------------- COMPOSITION

    [Fact]
    public void Create_KnownAvailableModuleIds_NoErrors()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", [JobOnView, ControloCreate], LandingDestinationId: null),
            CreateRegistry());

        Assert.Empty(errors);
    }

    [Fact]
    public void Create_UnavailableModule_Error_NoNewSelection()
    {
        // armazem is canonical but not registered in this build: it can never be newly selected.
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", [Armazem], LandingDestinationId: null),
            CreateRegistry());

        Assert.Contains(errors, error => error.Contains(Armazem, StringComparison.Ordinal));
    }

    [Fact]
    public void Create_UnknownModuleId_Error()
    {
        // A display name or any non-canonical string is not an identity.
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", ["Job On View"], LandingDestinationId: null),
            CreateRegistry());

        Assert.Contains(errors, error => error.Contains("Job On View", StringComparison.Ordinal));
    }

    [Fact]
    public void Create_DuplicateModuleIds_Error()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", [JobOnView, JobOnView], LandingDestinationId: null),
            CreateRegistry());

        Assert.Contains(errors, error => error.Contains("more than once", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Create_DisplayNameIsNotIdentity_Errors()
    {
        // The validator never resolves display names/destinations as identities (ACCESS_MODEL §2).
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", ["Job On"], LandingDestinationId: null),
            CreateRegistry());

        Assert.DoesNotContain(errors, error => error.Contains("Job On View", StringComparison.Ordinal));
    }

    [Fact]
    public void Update_PersistedUnavailableModule_Preserved_NoErrors()
    {
        // A persisted unavailable entry (future surface, or state created while available)
        // may stay: the ficha surfaces it locked; this update preserves it (no silent repair).
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                Guid.NewGuid(), "Manutenção", [JobOnView, Armazem], LandingDestinationId: null, ExpectedVersion: 1),
            persistedModuleIds: new HashSet<string>([JobOnView, Armazem], StringComparer.Ordinal),
            registry: CreateRegistry());

        Assert.Empty(errors);
    }

    [Fact]
    public void Update_ReAddingUnavailableModule_Error()
    {
        // Re-adding a known-but-unavailable id that is NOT in the persisted composition is a
        // new selection → rejected.
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                Guid.NewGuid(), "Manutenção", [Armazem], LandingDestinationId: null, ExpectedVersion: 1),
            persistedModuleIds: new HashSet<string>([JobOnView], StringComparer.Ordinal),
            registry: CreateRegistry());

        Assert.Contains(errors, error => error.Contains(Armazem, StringComparison.Ordinal));
    }

    [Fact]
    public void Update_RemovingLastUnavailableModule_Allowed()
    {
        // Explicit removal is always allowed: the composition simply no longer contains it.
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                Guid.NewGuid(), "Manutenção", [JobOnView], LandingDestinationId: null, ExpectedVersion: 1),
            persistedModuleIds: new HashSet<string>([JobOnView, Armazem], StringComparer.Ordinal),
            registry: CreateRegistry());

        Assert.Empty(errors);
    }

    // -------------------------------------------------------------- LANDING

    [Fact]
    public void Create_NullLanding_Valid()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", [JobOnView], LandingDestinationId: null),
            CreateRegistry());

        Assert.Empty(errors);
    }

    [Fact]
    public void Create_LandingRepresentedBySelectedModule_Valid()
    {
        // job-on-view has destination "job-on": a represented landing.
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", [JobOnView], LandingDestinationId: "job-on"),
            CreateRegistry());

        Assert.Empty(errors);
    }

    [Fact]
    public void Create_SharedDestination_Valid_WithoutModuleMerging()
    {
        // job-on-view + job-on-create share destination "job-on" but stay two distinct Module
        // identities: the landing is valid and no identity merging occurs.
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", [JobOnView, JobOnCreate], LandingDestinationId: "job-on"),
            CreateRegistry());

        Assert.Empty(errors);
    }

    [Fact]
    public void Create_LandingNotRepresented_Error()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", [JobOnView], LandingDestinationId: "controlo"),
            CreateRegistry());

        Assert.Contains(errors, error => error.Contains("controlo", StringComparison.Ordinal));
    }

    [Fact]
    public void Create_LandingOnlyFromContextualModule_Error()
    {
        // ferramentas is contextual-only (no destination): it cannot represent any landing.
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", [Ferramentas], LandingDestinationId: "job-on"),
            CreateRegistry());

        Assert.Contains(errors, error => error.Contains("job-on", StringComparison.Ordinal));
    }

    [Fact]
    public void Create_LandingNotBackedByAvailableModule_Error()
    {
        // The landing expects "job-on", but the only selected Module is armazem (unavailable):
        // unavailable Modules never back a landing (they grant nothing).
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.CreateTemplateCommand(
                "Manutenção", [Armazem], LandingDestinationId: "job-on"),
            CreateRegistry());

        Assert.Contains(errors, error => error.Contains("job-on", StringComparison.Ordinal));
    }

    [Fact]
    public void Update_RemovingModuleThatBacksLanding_Error()
    {
        // The edit drops the only Module with destination "job-on" but keeps the landing:
        // rejected (no silent clearing, no auto-fix) — the Admin must change/remove the landing.
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                Guid.NewGuid(), "Manutenção", [ControloCreate], LandingDestinationId: "job-on", ExpectedVersion: 1),
            persistedModuleIds: new HashSet<string>([JobOnView, ControloCreate], StringComparer.Ordinal),
            registry: CreateRegistry());

        Assert.Contains(errors, error => error.Contains("job-on", StringComparison.Ordinal));
    }

    [Fact]
    public void Update_CorrectingLandingWhileRemovingModule_Valid()
    {
        // The Admin explicitly corrects the landing to the represented destination.
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                Guid.NewGuid(), "Manutenção", [ControloCreate], LandingDestinationId: "controlo", ExpectedVersion: 1),
            persistedModuleIds: new HashSet<string>([JobOnView, ControloCreate], StringComparer.Ordinal),
            registry: CreateRegistry());

        Assert.Empty(errors);
    }

    // --------------------------------------------- POSITIVE VERSION RULE (Architect correction B)

    /// <summary>
    /// Architect correction B (implementation review, CORRECTION REQUIRED §3): the accepted plan
    /// §17 requires <c>ExpectedVersion &gt; 0</c>; malformed carriers must fail closed as input
    /// validation, never fall through into repository comparison and surface as stale conflicts.
    /// </summary>
    /// <remarks>
    /// Test: Update_ExpectedVersionZero_Error.<br/>
    /// Purpose: prove a zero carrier is rejected by validation alone — before any repository
    /// read/write.<br/>
    /// Master behavior being verified: accepted plan §17 — <c>ExpectedVersion válido (&gt;0)</c>.<br/>
    /// Preconditions: command with <c>ExpectedVersion = 0</c> and otherwise valid facts.<br/>
    /// Action: <c>TemplateAdministrationValidator.Validate(update, persistedIds, registry)</c>.<br/>
    /// Assertions: at least one error naming the version; no exception.<br/>
    /// Required non-effects: pure validation — none possible (no repository involved).<br/>
    /// What this proves: the validators hold the rule, so the service rejects before any write.<br/>
    /// What this does NOT prove: that the service performs no write (service-level tests cover it).
    /// </remarks>
    [Fact]
    public void Update_ExpectedVersionZero_Error()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                Guid.NewGuid(), "Manutenção", [JobOnView], LandingDestinationId: null, ExpectedVersion: 0),
            persistedModuleIds: new HashSet<string>([JobOnView], StringComparer.Ordinal),
            registry: CreateRegistry());

        Assert.Contains(errors, error => error.Contains("ExpectedVersion", StringComparison.Ordinal));
    }

    /// <remarks>
    /// Test: Update_ExpectedVersionNegative_Error.<br/>
    /// Purpose: prove a negative carrier is rejected by validation alone.<br/>
    /// Master behavior being verified: accepted plan §17 — <c>ExpectedVersion válido (&gt;0)</c>.<br/>
    /// Preconditions: command with <c>ExpectedVersion = -1</c> and otherwise valid facts.<br/>
    /// Action: <c>Validate(update, persistedIds, registry)</c>.<br/>
    /// Assertions: at least one error naming the version.<br/>
    /// Required non-effects: pure validation — none possible.<br/>
    /// What this proves: negative carriers never become "stale version" outcomes.<br/>
    /// What this does NOT prove: service no-write behavior (service-level tests cover it).
    /// </remarks>
    [Fact]
    public void Update_ExpectedVersionNegative_Error()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                Guid.NewGuid(), "Manutenção", [JobOnView], LandingDestinationId: null, ExpectedVersion: -1),
            persistedModuleIds: new HashSet<string>([JobOnView], StringComparer.Ordinal),
            registry: CreateRegistry());

        Assert.Contains(errors, error => error.Contains("ExpectedVersion", StringComparison.Ordinal));
    }

    /// <remarks>
    /// Test: Update_ExpectedVersionPositive_StillValid.<br/>
    /// Purpose: prove the new rule does not reject the accepted positive carrier.<br/>
    /// Master behavior being verified: assumed positive versions are valid when facts are valid.<br/>
    /// Preconditions: update command with <c>ExpectedVersion = 1</c>.<br/>
    /// Action: <c>Validate(...)</c>.<br/>
    /// Assertions: empty errors.<br/>
    /// Required non-effects: none.<br/>
    /// What this proves: the rule is exactly <c>&gt; 0</c>, nothing broader.<br/>
    /// What this does NOT prove: repository behavior.
    /// </remarks>
    [Fact]
    public void Update_ExpectedVersionPositive_NoErrors()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.UpdateTemplateCommand(
                Guid.NewGuid(), "Manutenção", [JobOnView], LandingDestinationId: null, ExpectedVersion: 1),
            persistedModuleIds: new HashSet<string>([JobOnView], StringComparer.Ordinal),
            registry: CreateRegistry());

        Assert.Empty(errors);
    }

    /// <remarks>
    /// Test: Delete_ExpectedVersionZero_Error.<br/>
    /// Purpose: prove a zero delete carrier is rejected before any delete/null-out.<br/>
    /// Master behavior being verified: accepted plan §17 — delete carries <c>ExpectedVersion &gt; 0</c>.<br/>
    /// Preconditions: delete command with <c>ExpectedVersion = 0</c>.<br/>
    /// Action: <c>Validate(delete)</c>.<br/>
    /// Assertions: at least one error naming the version.<br/>
    /// Required non-effects: pure validation.<br/>
    /// What this proves: the validator closes the malformed delete at the boundary.<br/>
    /// What this does NOT prove: no-destructive-side-effect at service level (covered elsewhere).
    /// </remarks>
    [Fact]
    public void Delete_ExpectedVersionZero_Error()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.DeleteTemplateCommand(Guid.NewGuid(), ExpectedVersion: 0));

        Assert.Contains(errors, error => error.Contains("ExpectedVersion", StringComparison.Ordinal));
    }

    /// <remarks>
    /// Test: Delete_ExpectedVersionNegative_Error.<br/>
    /// Purpose: prove a negative delete carrier is rejected before any delete/null-out.<br/>
    /// Master behavior being verified: accepted plan §17.<br/>
    /// Preconditions: delete command with <c>ExpectedVersion = -1</c>.<br/>
    /// Action: <c>Validate(delete)</c>.<br/>
    /// Assertions: at least one error naming the version.<br/>
    /// Required non-effects: pure validation.<br/>
    /// What this proves: negative delete carriers are input failures, not conflicts.<br/>
    /// What this does NOT prove: service-level non-effects (covered elsewhere).
    /// </remarks>
    [Fact]
    public void Delete_ExpectedVersionNegative_Error()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.DeleteTemplateCommand(Guid.NewGuid(), ExpectedVersion: -1));

        Assert.Contains(errors, error => error.Contains("ExpectedVersion", StringComparison.Ordinal));
    }

    /// <remarks>
    /// Test: Delete_ExpectedVersionPositive_NoErrors.<br/>
    /// Purpose: prove accepted positive delete carriers remain valid.<br/>
    /// Master behavior being verified: accepted plan §17.<br/>
    /// Preconditions: delete command with <c>ExpectedVersion = 1</c>.<br/>
    /// Action: <c>Validate(delete)</c>.<br/>
    /// Assertions: empty errors.<br/>
    /// Required non-effects: none.<br/>
    /// What this proves: the delete rule is exactly <c>&gt; 0</c>.<br/>
    /// What this does NOT prove: repository behavior.
    /// </remarks>
    [Fact]
    public void Delete_ExpectedVersionPositive_NoErrors()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.DeleteTemplateCommand(Guid.NewGuid(), ExpectedVersion: 1));

        Assert.Empty(errors);
    }

    /// <remarks>
    /// Test: Membership_UserExpectedVersionZero_Error.<br/>
    /// Purpose: prove a zero membership carrier is rejected before any membership write.<br/>
    /// Master behavior being verified: accepted plan §17 — association operations carry a valid
    /// user optimistic-concurrency version.<br/>
    /// Preconditions: membership command with <c>UserExpectedVersion = 0</c>.<br/>
    /// Action: <c>Validate(membership)</c>.<br/>
    /// Assertions: at least one error naming the version.<br/>
    /// Required non-effects: pure validation.<br/>
    /// What this proves: malformed membership versions are input failures, not conflicts.<br/>
    /// What this does NOT prove: service-level no-write behavior (covered elsewhere).
    /// </remarks>
    [Fact]
    public void Membership_UserExpectedVersionZero_Error()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.SetTemplateUserCommand(
                Guid.NewGuid(), Guid.NewGuid(), TargetTemplateId: null, UserExpectedVersion: 0));

        Assert.Contains(errors, error => error.Contains("UserExpectedVersion", StringComparison.Ordinal));
    }

    /// <remarks>
    /// Test: Membership_UserExpectedVersionNegative_Error.<br/>
    /// Purpose: prove a negative membership carrier is rejected before any membership write.<br/>
    /// Master behavior being verified: accepted plan §17.<br/>
    /// Preconditions: membership command with <c>UserExpectedVersion = -1</c>.<br/>
    /// Action: <c>Validate(membership)</c>.<br/>
    /// Assertions: at least one error naming the version.<br/>
    /// Required non-effects: pure validation.<br/>
    /// What this proves: negative membership carriers never become stale-version conflicts.<br/>
    /// What this does NOT prove: service-level behavior (covered elsewhere).
    /// </remarks>
    [Fact]
    public void Membership_UserExpectedVersionNegative_Error()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.SetTemplateUserCommand(
                Guid.NewGuid(), Guid.NewGuid(), TargetTemplateId: null, UserExpectedVersion: -1));

        Assert.Contains(errors, error => error.Contains("UserExpectedVersion", StringComparison.Ordinal));
    }

    /// <remarks>
    /// Test: Membership_UserExpectedVersionPositive_NoErrors.<br/>
    /// Purpose: prove accepted positive membership carriers remain valid.<br/>
    /// Master behavior being verified: accepted plan §17.<br/>
    /// Preconditions: membership command with <c>UserExpectedVersion = 1</c>.<br/>
    /// Action: <c>Validate(membership)</c>.<br/>
    /// Assertions: empty errors.<br/>
    /// Required non-effects: none.<br/>
    /// What this proves: the membership version rule is exactly <c>&gt; 0</c>.<br/>
    /// What this does NOT prove: repository behavior.
    /// </remarks>
    [Fact]
    public void Membership_UserExpectedVersionPositive_NoErrors()
    {
        var errors = TemplateAdministrationValidator.Validate(
            new TemplateAdministrationCommands.SetTemplateUserCommand(
                Guid.NewGuid(), Guid.NewGuid(), TargetTemplateId: null, UserExpectedVersion: 1));

        Assert.Empty(errors);
    }

    // -------------------------------------------------------------- HELPERS

    private static IModuleRegistry CreateRegistry() => TestModuleDefinitions.TestRegistry();
}